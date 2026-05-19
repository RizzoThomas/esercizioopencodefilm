using FluentAssertions;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FilmAPI.Tests.Unit;

/// <summary>Suite di test per FilmServiceTests.</summary>
public class FilmServiceTests : IAsyncLifetime
{
    private readonly IServiceProvider _serviceProvider;
    private readonly FilmDbContext _context;
    private readonly IFilmService _service;

    public FilmServiceTests()
    {
        var services = new ServiceCollection();
        
        var options = new DbContextOptionsBuilder<FilmDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new FilmDbContext(options);
        services.AddScoped(_ => _context);
        services.AddScoped<IRegistaService, RegistaService>();
        services.AddScoped<IFilmService, FilmService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _service = _serviceProvider.GetRequiredService<IFilmService>();
    }

    public async Task InitializeAsync()
    {
        var registaService = _serviceProvider.GetRequiredService<IRegistaService>();
        await registaService.CreateAsync(new RegistaCreateDTO { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "UK" });
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    /// <summary>Verifica lo scenario di U_F1_GetAllAsync_WhenNoFilmsExist_ReturnsEmptyList: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task U_F1_GetAllAsync_WhenNoFilmsExist_ReturnsEmptyList()
    {
        var result = await _service.GetAllAsync();
        result.Should().BeEmpty();
    }

    /// <summary>Verifica lo scenario di U_F2_GetAllAsync_WhenFilmsExist_ReturnsAllFilms: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task U_F2_GetAllAsync_WhenFilmsExist_ReturnsAllFilms()
    {
        await _service.CreateAsync(new FilmCreateDTO { Titolo = "Inception", DataProduzione = new DateTime(2010, 7, 16), RegistaId = 1, Durata = 148 });
        await _service.CreateAsync(new FilmCreateDTO { Titolo = "Interstellar", DataProduzione = new DateTime(2014, 11, 7), RegistaId = 1, Durata = 169 });

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(2);
    }

    /// <summary>Verifica lo scenario di U_F3_GetByIdAsync_WhenFilmExists_ReturnsFilm: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task U_F3_GetByIdAsync_WhenFilmExists_ReturnsFilm()
    {
        var created = await _service.CreateAsync(new FilmCreateDTO { Titolo = "Inception", DataProduzione = new DateTime(2010, 7, 16), RegistaId = 1, Durata = 148 });

        var result = await _service.GetByIdAsync(created.Id);

        result.Should().NotBeNull();
        result!.Titolo.Should().Be("Inception");
    }

    /// <summary>Verifica lo scenario di U_F4_GetByIdAsync_WhenFilmNotExists_ReturnsNull: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task U_F4_GetByIdAsync_WhenFilmNotExists_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(999);

        result.Should().BeNull();
    }

    /// <summary>Verifica lo scenario di U_F5_CreateAsync_WithValidData_CreatesFilm: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task U_F5_CreateAsync_WithValidData_CreatesFilm()
    {
        var dto = new FilmCreateDTO { Titolo = "Inception", DataProduzione = new DateTime(2010, 7, 16), RegistaId = 1, Durata = 148 };

        var result = await _service.CreateAsync(dto);

        result.Id.Should().BeGreaterThan(0);
        result.Titolo.Should().Be("Inception");
    }

    /// <summary>Verifica lo scenario di U_F6_CreateAsync_WhenRegistaNotExists_ThrowsArgumentException: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task U_F6_CreateAsync_WhenRegistaNotExists_ThrowsArgumentException()
    {
        var dto = new FilmCreateDTO { Titolo = "Inception", DataProduzione = new DateTime(2010, 7, 16), RegistaId = 999, Durata = 148 };

        var act = async () => await _service.CreateAsync(dto);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>Verifica lo scenario di U_F7_UpdateAsync_WithValidData_UpdatesFilm: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task U_F7_UpdateAsync_WithValidData_UpdatesFilm()
    {
        var created = await _service.CreateAsync(new FilmCreateDTO { Titolo = "Inception", DataProduzione = new DateTime(2010, 7, 16), RegistaId = 1, Durata = 148 });
        var dto = new FilmUpdateDTO { Titolo = "Inception 2", DataProduzione = new DateTime(2010, 7, 16), RegistaId = 1, Durata = 150 };

        var result = await _service.UpdateAsync(created.Id, dto);

        result.Should().NotBeNull();
        result!.Titolo.Should().Be("Inception 2");
    }

    /// <summary>Verifica lo scenario di U_F8_UpdateAsync_WhenRegistaNotExists_ThrowsArgumentException: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task U_F8_UpdateAsync_WhenRegistaNotExists_ThrowsArgumentException()
    {
        var created = await _service.CreateAsync(new FilmCreateDTO { Titolo = "Inception", DataProduzione = new DateTime(2010, 7, 16), RegistaId = 1, Durata = 148 });
        var dto = new FilmUpdateDTO { Titolo = "Inception", DataProduzione = new DateTime(2010, 7, 16), RegistaId = 999, Durata = 148 };

        var act = async () => await _service.UpdateAsync(created.Id, dto);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>Verifica lo scenario di U_F10_DeleteAsync_WhenFilmExists_DeletesFilm: predispone i dati e le condizioni previste dal caso di test e controlla che l'esito atteso venga restituito.</summary>
    [Fact]
    public async Task U_F10_DeleteAsync_WhenFilmExists_DeletesFilm()
    {
        var created = await _service.CreateAsync(new FilmCreateDTO { Titolo = "Inception", DataProduzione = new DateTime(2010, 7, 16), RegistaId = 1, Durata = 148 });

        var result = await _service.DeleteAsync(created.Id);

        result.Should().BeTrue();
    }
}
