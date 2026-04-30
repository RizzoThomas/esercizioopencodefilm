using FluentAssertions;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FilmAPI.Tests.Unit;

public class RegistaServiceTests : IAsyncLifetime
{
    private readonly IServiceProvider _serviceProvider;
    private readonly FilmDbContext _context;
    private readonly IRegistaService _service;

    public RegistaServiceTests()
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
        _service = _serviceProvider.GetRequiredService<IRegistaService>();
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

    [Fact]
    public async Task U_R1_GetAllAsync_WhenNoRegistiExist_ReturnsEmptyList()
    {
        var result = await _service.GetAllAsync();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task U_R2_GetAllAsync_WhenRegistiExist_ReturnsAllRegisti()
    {
        await _service.CreateAsync(new RegistaCreateDTO { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "UK" });
        await _service.CreateAsync(new RegistaCreateDTO { Nome = "Quentin", Cognome = "Tarantino", Nazionalita = "US" });

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task U_R3_GetByIdAsync_WhenRegistaExists_ReturnsRegista()
    {
        var created = await _service.CreateAsync(new RegistaCreateDTO { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "UK" });

        var result = await _service.GetByIdAsync(created.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.Nome.Should().Be("Christopher");
    }

    [Fact]
    public async Task U_R4_GetByIdAsync_WhenRegistaNotExists_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task U_R5_CreateAsync_WithValidData_CreatesRegista()
    {
        var dto = new RegistaCreateDTO { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "UK" };

        var result = await _service.CreateAsync(dto);

        result.Id.Should().BeGreaterThan(0);
        result.Nome.Should().Be("Christopher");
        result.Cognome.Should().Be("Nolan");
        result.Nazionalita.Should().Be("UK");
    }

    [Fact]
    public async Task U_R6_CreateAsync_WithInvalidData_ThrowsArgumentException()
    {
        var dto = new RegistaCreateDTO { Nome = "", Cognome = "Nolan", Nazionalita = "UK" };

        var act = async () => await _service.CreateAsync(dto);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task U_R7_UpdateAsync_WhenRegistaExists_UpdatesRegista()
    {
        var created = await _service.CreateAsync(new RegistaCreateDTO { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "UK" });
        var dto = new RegistaUpdateDTO { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "American" };

        var result = await _service.UpdateAsync(created.Id, dto);

        result.Should().NotBeNull();
        result!.Nazionalita.Should().Be("American");
    }

    [Fact]
    public async Task U_R8_UpdateAsync_WhenRegistaNotExists_ReturnsNull()
    {
        var dto = new RegistaUpdateDTO { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "UK" };

        var result = await _service.UpdateAsync(999, dto);

        result.Should().BeNull();
    }

    [Fact]
    public async Task U_R9_DeleteAsync_WhenRegistaExists_DeletesRegista()
    {
        var created = await _service.CreateAsync(new RegistaCreateDTO { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "UK" });

        var result = await _service.DeleteAsync(created.Id);

        result.Should().BeTrue();
        var deleted = await _service.GetByIdAsync(created.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task U_R10_DeleteAsync_WhenRegistaNotExists_ReturnsFalse()
    {
        var result = await _service.DeleteAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task U_R11_GetFilmsByRegistaIdAsync_WhenRegistaHasFilms_ReturnsFilms()
    {
        var regista = await _service.CreateAsync(new RegistaCreateDTO { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "UK" });
        
        var filmDto = new FilmCreateDTO
        {
            Titolo = "Inception",
            DataProduzione = new DateTime(2010, 7, 16),
            RegistaId = regista.Id,
            Durata = 148
        };
        
        var filmService = _serviceProvider.GetRequiredService<IFilmService>();
        await filmService.CreateAsync(filmDto);

        var result = await _service.GetFilmsByRegistaIdAsync(regista.Id);

        result.Should().HaveCount(1);
        result[0].Titolo.Should().Be("Inception");
    }

    [Fact]
    public async Task U_R12_GetFilmsByRegistaIdAsync_WhenRegistaHasNoFilms_ReturnsEmptyList()
    {
        var regista = await _service.CreateAsync(new RegistaCreateDTO { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "UK" });

        var result = await _service.GetFilmsByRegistaIdAsync(regista.Id);

        result.Should().BeEmpty();
    }
}
