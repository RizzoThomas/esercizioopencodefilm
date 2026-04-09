using FluentAssertions;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FilmAPI.Tests.Unit;

public class ProiezioneServiceTests : IAsyncLifetime
{
    private readonly IServiceProvider _serviceProvider;
    private readonly FilmDbContext _context;
    private readonly IProiezioneService _service;

    public ProiezioneServiceTests()
    {
        var services = new ServiceCollection();
        
        var options = new DbContextOptionsBuilder<FilmDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new FilmDbContext(options);
        services.AddScoped(_ => _context);
        services.AddScoped<IRegistaService, RegistaService>();
        services.AddScoped<IFilmService, FilmService>();
        services.AddScoped<ICinemaService, CinemaService>();
        services.AddScoped<IProiezioneService, ProiezioneService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _service = _serviceProvider.GetRequiredService<IProiezioneService>();
    }

    public async Task InitializeAsync()
    {
        var registaService = _serviceProvider.GetRequiredService<IRegistaService>();
        await registaService.CreateAsync(new RegistaCreateDTO { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "UK" });
        
        var filmService = _serviceProvider.GetRequiredService<IFilmService>();
        await filmService.CreateAsync(new FilmCreateDTO { Titolo = "Inception", DataProduzione = new DateTime(2010, 7, 16), RegistaId = 1, Durata = 148 });
        
        var cinemaService = _serviceProvider.GetRequiredService<ICinemaService>();
        await cinemaService.CreateAsync(new CinemaCreateDTO { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Milano" });
        
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task U_P1_GetAllAsync_WhenNoProiezioniExist_ReturnsEmptyList()
    {
        var result = await _service.GetAllAsync();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task U_P2_CreateAsync_WithValidData_CreatesProiezione()
    {
        var dto = new ProiezioneCreateDTO
        {
            CinemaId = 1,
            FilmId = 1,
            Data = new DateTime(2024, 12, 25),
            Ora = "20:00"
        };

        var result = await _service.CreateAsync(dto);

        result.Id.Should().BeGreaterThan(0);
        result.CinemaId.Should().Be(1);
    }

    [Fact]
    public async Task U_P3_CreateAsync_WhenCinemaNotExists_ThrowsArgumentException()
    {
        var dto = new ProiezioneCreateDTO
        {
            CinemaId = 999,
            FilmId = 1,
            Data = new DateTime(2024, 12, 25),
            Ora = "20:00"
        };

        var act = async () => await _service.CreateAsync(dto);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task U_P4_CreateAsync_WhenFilmNotExists_ThrowsArgumentException()
    {
        var dto = new ProiezioneCreateDTO
        {
            CinemaId = 1,
            FilmId = 999,
            Data = new DateTime(2024, 12, 25),
            Ora = "20:00"
        };

        var act = async () => await _service.CreateAsync(dto);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task U_P5_CreateAsync_WhenDuplicateUnique_ThrowsInvalidOperationException()
    {
        var dto = new ProiezioneCreateDTO
        {
            CinemaId = 1,
            FilmId = 1,
            Data = new DateTime(2024, 12, 25),
            Ora = "20:00"
        };

        await _service.CreateAsync(dto);

        var act = async () => await _service.CreateAsync(dto);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task U_P6_UpdateAsync_WithValidData_UpdatesProiezione()
    {
        var created = await _service.CreateAsync(new ProiezioneCreateDTO
        {
            CinemaId = 1,
            FilmId = 1,
            Data = new DateTime(2024, 12, 25),
            Ora = "20:00"
        });

        var dto = new ProiezioneUpdateDTO
        {
            CinemaId = 1,
            FilmId = 1,
            Data = new DateTime(2024, 12, 26),
            Ora = "21:00"
        };

        var result = await _service.UpdateAsync(created.Id, dto);

        result.Should().NotBeNull();
        result!.Data.Date.Should().Be(new DateTime(2024, 12, 26));
    }

    [Fact]
    public async Task U_P7_DeleteAsync_WhenProiezioneExists_DeletesProiezione()
    {
        var created = await _service.CreateAsync(new ProiezioneCreateDTO
        {
            CinemaId = 1,
            FilmId = 1,
            Data = new DateTime(2024, 12, 25),
            Ora = "20:00"
        });

        var result = await _service.DeleteAsync(created.Id);

        result.Should().BeTrue();
    }
}
