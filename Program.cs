using CognomeNomeAPI.Data;
using CognomeNomeAPI.DTO;
using CognomeNomeAPI.Extensions;
using CognomeNomeAPI.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("FilmAPIConnection");
var serverVersion = ServerVersion.AutoDetect(connectionString);

builder.Services.AddDbContext<FilmDbContext>(
    dbContextOptions => dbContextOptions
        .UseMySql(connectionString, serverVersion)
        .LogTo(Console.WriteLine, LogLevel.Information)
        .EnableSensitiveDataLogging()
        .EnableDetailedErrors()
);

// Register services
builder.Services.AddSingleton<CognomeNomeAPI.Services.IAIAdapter, CognomeNomeAPI.Services.MockAIAdapter>();
builder.Services.AddSingleton<CognomeNomeAPI.Services.Scoring.IScoringService, CognomeNomeAPI.Services.Scoring.MockScoringService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.DocumentTitle = "FilmAPI di Cognome v1";
        c.RoutePrefix = "swagger";
    });
}

// Serve a simple static UI from wwwroot (open /index.html)
app.UseDefaultFiles();
app.UseStaticFiles();

// Apply EF migrations at startup (development convenience)
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
    db.Database.Migrate();
    logger.LogInformation("Database migrated successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database.");
        // In production you'd normally not swallow this error
    }
}

// Tasks endpoints (PRD minimal)
app.MapPost("/tasks", async (TaskItem dto, FilmDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Title)) return Results.BadRequest(new { error = "Title required" });
    var t = new TaskItem
    {
        Title = dto.Title,
        Description = dto.Description,
        AssigneeId = dto.AssigneeId,
        CreatorId = dto.CreatorId,
        TeamId = dto.TeamId,
        PriorityScore = dto.PriorityScore,
        Status = dto.Status,
        DueDate = dto.DueDate,
        ParentTaskId = dto.ParentTaskId
    };
    db.Tasks.Add(t);
    await db.SaveChangesAsync();
    return Results.Created($"/tasks/{t.Id}", t);
});

// Natural language task creation (mock parser)
app.MapPost("/tasks/nl", async (NaturalLanguageTaskDTO dto, FilmDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Text)) return Results.BadRequest(new { error = "Text required" });
    var ai = app.Services.GetRequiredService<CognomeNomeAPI.Services.IAIAdapter>();
    var parsed = await ai.ParseTaskAsync(dto.Text);

    var t = new TaskItem { Title = parsed.Title, Description = parsed.Description, CreatorId = dto.CreatorId, TeamId = dto.TeamId, DueDate = parsed.DueDate };
    db.Tasks.Add(t);
    await db.SaveChangesAsync();
    return Results.Created($"/tasks/{t.Id}", t);
});

app.MapPatch("/tasks/{id}", async (int id, TaskItem dto, FilmDbContext db) =>
{
    var t = await db.Tasks.FindAsync(id);
    if (t is null) return Results.NotFound();
    t.Title = dto.Title ?? t.Title;
    t.Description = dto.Description ?? t.Description;
    t.AssigneeId = dto.AssigneeId ?? t.AssigneeId;
    t.Status = dto.Status ?? t.Status;
    t.PriorityScore = dto.PriorityScore;
    t.DueDate = dto.DueDate ?? t.DueDate;
    t.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(t);
});

// Compute and persist priority score for a task
app.MapPost("/tasks/{id}/score", async (int id, FilmDbContext db, CognomeNomeAPI.Services.Scoring.IScoringService scoring) =>
{
    var t = await db.Tasks.FindAsync(id);
    if (t is null) return Results.NotFound();

    // simple dependency depth provider that walks ParentTaskId
    int DepthProvider(int? parentId)
    {
        var depth = 0;
        var cur = parentId;
        while (cur.HasValue)
        {
            var p = db.Tasks.Find(cur.Value);
            if (p == null) break;
            depth++;
            cur = p.ParentTaskId;
            if (depth > 50) break; // guard
        }
        return depth;
    }

    var (score, factorsJson) = scoring.ComputePriority(t, DepthProvider);
    t.PriorityScore = score;
    t.UpdatedAt = DateTime.UtcNow;

    var log = new PriorityLog { TaskId = t.Id, Score = score, FactorsJson = factorsJson };
    db.PriorityLogs.Add(log);
    await db.SaveChangesAsync();

    return Results.Ok(new { TaskId = t.Id, PriorityScore = t.PriorityScore, PriorityLogId = log.Id });
});

app.MapGet("/tasks", async (int? team_id, string? sort, FilmDbContext db) =>
{
    var q = db.Tasks.AsQueryable();
    if (team_id.HasValue) q = q.Where(t => t.TeamId == team_id.Value);
    if (sort == "priority") q = q.OrderByDescending(t => t.PriorityScore);
    return Results.Ok(await q.ToListAsync());
});

app.MapGet("/tasks/{id}", async (int id, FilmDbContext db) =>
{
    var t = await db.Tasks.FindAsync(id);
    return t is null ? Results.NotFound() : Results.Ok(t);
});


app.MapGet("/registi", async (FilmDbContext db) =>
{
    var list = await db.Registi.ToListAsync();
    return Results.Ok(list.Select(r => r.ToDTO()));
});

app.MapGet("/registi/{id}", async (int id, FilmDbContext db) =>
{
    var reg = await db.Registi.FindAsync(id);
    return reg is null ? Results.NotFound() : Results.Ok(reg.ToDTO());
});

app.MapPost("/registi", async (RegistaDTO dto, FilmDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Cognome))
        return Results.BadRequest(new { error = "Nome and Cognome are required." });

    var entity = dto.ToEntity();
    db.Registi.Add(entity);
    await db.SaveChangesAsync();
    return Results.Created($"/registi/{entity.Id}", entity.ToDTO());
});

app.MapPut("/registi/{id}", async (int id, RegistaDTO dto, FilmDbContext db) =>
{
    var reg = await db.Registi.FindAsync(id);
    if (reg is null) return Results.NotFound();
    reg.Nome = dto.Nome;
    reg.Cognome = dto.Cognome;
    reg.Nazionalita = dto.Nazionalita;
    await db.SaveChangesAsync();
    return Results.Ok(reg.ToDTO());
});

app.MapDelete("/registi/{id}", async (int id, FilmDbContext db) =>
{
    var reg = await db.Registi.FindAsync(id);
    if (reg is null) return Results.NotFound();
    db.Registi.Remove(reg);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapGet("/registi/{id}/films", async (int id, FilmDbContext db) =>
{
    var exists = await db.Registi.AnyAsync(r => r.Id == id);
    if (!exists) return Results.NotFound();
    var films = await db.Films.Where(f => f.RegistaId == id).ToListAsync();
    return Results.Ok(films.Select(f => f.ToDTO()));
});

app.MapPost("/registi/{id}/films", async (int id, FilmDTO dto, FilmDbContext db) =>
{
    var reg = await db.Registi.FindAsync(id);
    if (reg is null) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(dto.Titolo) || dto.Durata <= 0)
        return Results.BadRequest(new { error = "Titolo is required and Durata must be > 0." });

    var film = dto.ToEntity();
    film.RegistaId = id;
    db.Films.Add(film);
    await db.SaveChangesAsync();
    return Results.Created($"/films/{film.Id}", film.ToDTO());
});

app.MapGet("/films", async (FilmDbContext db) =>
{
    var films = await db.Films.ToListAsync();
    return Results.Ok(films.Select(f => f.ToDTO()));
});

app.MapGet("/films/{id}", async (int id, FilmDbContext db) =>
{
    var film = await db.Films.FindAsync(id);
    return film is null ? Results.NotFound() : Results.Ok(film.ToDTO());
});

app.MapPut("/films/{id}", async (int id, FilmDTO dto, FilmDbContext db) =>
{
    var film = await db.Films.FindAsync(id);
    if (film is null) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(dto.Titolo) || dto.Durata <= 0)
        return Results.BadRequest(new { error = "Titolo is required and Durata must be > 0." });

    film.Titolo = dto.Titolo;
    film.DataProduzione = dto.DataProduzione;
    film.Durata = dto.Durata;
    film.RegistaId = dto.RegistaId;
    await db.SaveChangesAsync();
    return Results.Ok(film.ToDTO());
});

app.MapDelete("/films/{id}", async (int id, FilmDbContext db) =>
{
    var film = await db.Films.FindAsync(id);
    if (film is null) return Results.NotFound();
    db.Films.Remove(film);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapGet("/cinemas", async (FilmDbContext db) =>
{
    var cinemas = await db.Cinemas.ToListAsync();
    return Results.Ok(cinemas.Select(c => c.ToDTO()));
});

app.MapPost("/cinemas", async (CinemaDTO dto, FilmDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Indirizzo))
        return Results.BadRequest(new { error = "Nome and Indirizzo are required." });

    var entity = dto.ToEntity();
    db.Cinemas.Add(entity);
    await db.SaveChangesAsync();
    return Results.Created($"/cinemas/{entity.Id}", entity.ToDTO());
});

app.MapPost("/proiezioni", async (DatiProiezioneDTO dto, FilmDbContext db) =>
{
    if (dto.Data == default || dto.Ora == default)
        return Results.BadRequest(new { error = "Data and Ora are required." });

    var filmExists = await db.Films.AnyAsync(f => f.Id == dto.FilmId);
    var cinemaExists = await db.Cinemas.AnyAsync(c => c.Id == dto.CinemaId);
    if (!filmExists || !cinemaExists) return Results.BadRequest(new { error = "FilmId or CinemaId do not exist" });

    var p = new Proiezione { FilmId = dto.FilmId, CinemaId = dto.CinemaId, Data = dto.Data, Ora = dto.Ora };
    db.Proiezioni.Add(p);
    await db.SaveChangesAsync();
    return Results.Created($"/proiezioni/{p.Id}", p);
});

// GET proiezioni with joined film and cinema data
app.MapGet("/proiezioni", async (FilmDbContext db) =>
{
    var list = await db.Proiezioni
        .Join(db.Films, p => p.FilmId, f => f.Id, (p, f) => new { p, f })
        .Join(db.Cinemas, pf => pf.p.CinemaId, c => c.Id, (pf, c) => new { pf.p, pf.f, c })
        .Select(x => new DTO.ProiezioneViewDTO
        {
            Id = x.p.Id,
            FilmId = x.f.Id,
            FilmTitolo = x.f.Titolo,
            CinemaId = x.c.Id,
            CinemaNome = x.c.Nome,
            Data = x.p.Data,
            Ora = x.p.Ora
        }).ToListAsync();

    return Results.Ok(list);
});

app.Run();

// expose Program type for WebApplicationFactory in integration tests
namespace CognomeNomeAPI
{
    public partial class Program { }
}
