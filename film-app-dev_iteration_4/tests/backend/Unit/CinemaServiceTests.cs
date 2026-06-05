using FluentAssertions;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FilmAPI.Tests.Unit;

/// <summary>Suite di test per CinemaServiceTests.</summary>
public class CinemaServiceTests : IAsyncLifetime
{
    private readonly IServiceProvider _serviceProvider;
    private readonly FilmDbContext _context;
    private readonly ICinemaService _service;

    public CinemaServiceTests()
    {
        var services = new ServiceCollection();
        
        var options = new DbContextOptionsBuilder<FilmDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new FilmDbContext(options);
        services.AddScoped(_ => _context);
        services.AddScoped<ICinemaService, CinemaService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _service = _serviceProvider.GetRequiredService<ICinemaService>();
    }

    public async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    /// <summary>Verifica lo scenario di U_C1_GetAllAsync_WhenNoCinemasExist_ReturnsEmptyList: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task U_C1_GetAllAsync_WhenNoCinemasExist_ReturnsEmptyList()
    {
        var result = await _service.GetAllAsync();
        result.Should().BeEmpty();
    }

    /// <summary>Verifica lo scenario di U_C2_CreateAsync_WithValidData_CreatesCinema: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task U_C2_CreateAsync_WithValidData_CreatesCinema()
    {
        var dto = new CinemaCreateDTO { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Milano" };

        var result = await _service.CreateAsync(dto);

        result.Id.Should().BeGreaterThan(0);
        result.Nome.Should().Be("Cinema Odeon");
    }

    /// <summary>Verifica lo scenario di U_C3_GetByIdAsync_WhenCinemaExists_ReturnsCinema: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task U_C3_GetByIdAsync_WhenCinemaExists_ReturnsCinema()
    {
        var created = await _service.CreateAsync(new CinemaCreateDTO { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Milano" });

        var result = await _service.GetByIdAsync(created.Id);

        result.Should().NotBeNull();
        result!.Nome.Should().Be("Cinema Odeon");
    }

    /// <summary>Verifica lo scenario di U_C4_UpdateAsync_WithValidData_UpdatesCinema: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task U_C4_UpdateAsync_WithValidData_UpdatesCinema()
    {
        var created = await _service.CreateAsync(new CinemaCreateDTO { Nome = "Cinema Vecchio", Indirizzo = "Via A", Citta = "Roma" });
        var dto = new CinemaUpdateDTO { Nome = "Cinema Nuovo", Indirizzo = "Via B", Citta = "Roma" };

        var result = await _service.UpdateAsync(created.Id, dto);

        result.Should().NotBeNull();
        result!.Nome.Should().Be("Cinema Nuovo");
    }

    /// <summary>Verifica lo scenario di U_C5_DeleteAsync_WhenCinemaExists_DeletesCinema: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task U_C5_DeleteAsync_WhenCinemaExists_DeletesCinema()
    {
        var created = await _service.CreateAsync(new CinemaCreateDTO { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Milano" });

        var result = await _service.DeleteAsync(created.Id);

        result.Should().BeTrue();
    }
}
