using FilmAPI.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FilmAPI.Tests.Integration;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("DB_USE_AUTODETECT", "false");
        Environment.SetEnvironmentVariable("DB_SERVER_VERSION", "10.11.0-mariadb");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Add test configuration for JWT settings
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-super-secret-key-min-32-characters-long!!",
                ["Jwt:Issuer"] = "FilmAPI",
                ["Jwt:Audience"] = "CineBase.Web",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<FilmDbContext>>();
            services.RemoveAll<DbContextOptions<FilmDbContext>>();
            services.RemoveAll<FilmDbContext>();

            services.AddDbContext<FilmDbContext>(options =>
                options.UseSqlite(_connection));

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
            db.Database.EnsureCreated();
        });
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
    }

    public new async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    public async Task ResetDatabaseAsync(Func<FilmDbContext, Task>? seed = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        if (seed is not null)
        {
            await seed(db);
            await db.SaveChangesAsync();
        }
    }
}
