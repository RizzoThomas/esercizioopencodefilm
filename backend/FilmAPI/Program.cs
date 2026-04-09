using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using FilmAPI.Data;
using FilmAPI.Endpoints;
using FilmAPI.Services;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "film-api-db";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "root";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "root";
var dbUseAutoDetect = (Environment.GetEnvironmentVariable("DB_USE_AUTODETECT") ?? "true")
    .Equals("true", StringComparison.OrdinalIgnoreCase);
var dbServerVersion = Environment.GetEnvironmentVariable("DB_SERVER_VERSION") ?? "10.11.0-mariadb";

var useSqliteFallback = (Environment.GetEnvironmentVariable("USE_SQLITE_FALLBACK") ?? "true").Equals("true", StringComparison.OrdinalIgnoreCase);

if (!useSqliteFallback)
{
    var connectionString = $"Server={dbHost};Port={dbPort};Database={dbName};User Id={dbUser};Password={dbPassword};";
    var serverVersion = dbUseAutoDetect
        ? ServerVersion.AutoDetect(connectionString)
        : ServerVersion.Parse(dbServerVersion);

    builder.Services.AddDbContext<FilmDbContext>(
        dbContextOptions => dbContextOptions
        .UseMySql(connectionString, serverVersion)
        .LogTo(Console.WriteLine, LogLevel.Information)
        .EnableSensitiveDataLogging()
        .EnableDetailedErrors()
    );
}
else
{
    // Fallback di sviluppo: usare SQLite se MySQL non è disponibile
    var sqliteConn = Environment.GetEnvironmentVariable("SQLITE_CONNECTION") ?? "Data Source=dev_films.db";
    builder.Services.AddDbContext<FilmDbContext>(
        dbContextOptions => dbContextOptions
        .UseSqlite(sqliteConn)
        .LogTo(Console.WriteLine, LogLevel.Information)
        .EnableSensitiveDataLogging()
        .EnableDetailedErrors()
    );
}

// JWT Configuration
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? Environment.GetEnvironmentVariable("JWT_KEY")
    ?? "YourSuperSecretDevelopmentKeyThatIsAtLeast32Chars!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? "FilmAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? "CineBase.Web";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Services
builder.Services.AddScoped<IRegistaService, RegistaService>();
builder.Services.AddScoped<IFilmService, FilmService>();
builder.Services.AddScoped<ICinemaService, CinemaService>();
builder.Services.AddScoped<IProiezioneService, ProiezioneService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserProiezioneService, UserProiezioneService>();
builder.Services.AddScoped<IPrenotazioneService, PrenotazioneService>();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowCineBaseFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5001", "http://127.0.0.1:5001")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "FilmAPI";
    config.Title = "FilmAPI v1";
    config.Version = "v1";
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
    db.Database.EnsureCreated();
    SchemaPatcher.EnsureBookingSchema(db);
    DbSeeder.SeedIfEmpty(db);
}

app.UseCors("AllowCineBaseFrontend");

// Add Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "FilmAPI v1";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
        config.DocExpansion = "list";
    });
}

// Map endpoints
app.MapAuthEndpoints();
app.MapCategorieEndpoints();
app.MapUserProiezioniEndpoints();
app.MapPrenotazioniEndpoints();
app.MapRegistiEndpoints();
app.MapFilmsEndpoints();
app.MapCinemasEndpoints();
app.MapProiezioniEndpoints();

app.Run();

public partial class Program;
