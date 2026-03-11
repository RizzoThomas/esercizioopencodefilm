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

app.Run();

// expose Program type for WebApplicationFactory in integration tests
namespace CognomeNomeAPI
{
    public partial class Program { }
}
