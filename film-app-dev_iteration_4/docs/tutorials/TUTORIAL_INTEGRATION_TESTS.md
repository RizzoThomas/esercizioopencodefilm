# Guida ai Test di Integrazione - ASP.NET Core Minimal APIs

**Autore:** Claude AI Assistant
**Data:** 10 Marzo 2026
**Ultimo aggiornamento:** 17 Aprile 2026
**Progetto di Riferimento:** FilmAPI
**Framework:** xUnit 2.9.2 + Microsoft.AspNetCore.Mvc.Testing 9.0.11
**Linguaggio:** C# / .NET 9.0

---

## Indice
1. [Come Partono i Test: Entry Point e Discovery](#1-come-partono-i-test-entry-point-e-discovery)
2. [Introduzione ai Test di Integrazione](#2-introduzione-ai-test-di-integrazione)
3. [Test di Integrazione vs Test Unitari](#3-test-di-integrazione-vs-test-unitari)
4. [Setup dell'Ambiente di Integration Testing](#4-setup-dellambiente-di-integration-testing)
5. [CustomWebApplicationFactory - Il Cuore degli Integration Test](#5-customwebapplicationfactory--il-cuore-degli-integration-test)
6. [TestAuthHandler — Come Funziona l'Auth nei Test](#6-testauthhandler--come-funziona-lauth-nei-test)
7. [HttpClient per Testare API](#7-httpclient-per-testare-api)
8. [Database SQLite InMemory per Integration Test](#8-database-sqlite-inmemory-per-integration-test)
9. [Esempi Pratici dal Progetto FilmAPI](#9-esempi-pratici-dal-progetto-FilmAPI)
10. [Best Practices](#10-best-practices)

---

## 1. Come Partono i Test: Entry Point e Discovery

### 1.1 Non C'e un `Main`

Come per i test unitari, **non esiste un metodo `Main`** che fa partire i test di integrazione. Il motore è sempre `dotnet test` con xUnit:

```
dotnet test
    │
    ▼
Microsoft.NET.Test.Sdk
    │
    ▼
xUnit Runner → scopre classi con [Fact]
    │
    ▼
Per IClassFixture<CustomWebApplicationFactory>:
  1. Crea CustomWebApplicationFactory (una sola volta)
  2. Chiama InitializeAsync() → apre connessione SQLite
  3. Per ogni [Fact]:
     a. ResetDatabaseAsync() → cancella e ricrea DB
     b. Esegue il test con HttpClient
     c. Verifica risultati
  4. Chiama DisposeAsync() → chiude connessione
```

### 1.2 Anatomia di un Test di Integrazione

```csharp
// IClassFixture → xUnit crea UNA factory condivisa per tutta la classe
public class SalaIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    // La factory viene iniettata automaticamente da xUnit
    public SalaIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task S5_CreateSala_ReturnsCreated()
    {
        // 1. Reset DB + seed cinema
        await _factory.ResetDatabaseAsync(db => SeedCinemaAsync(db));

        // 2. Crea client con ruolo Admin (header X-Test-Role: Admin)
        var client = _factory.CreateAdminClient();

        // 3. Chiama l'endpoint REALE dell'app in-memory
        var response = await client.PostAsJsonAsync("/cinemas/1/sale", dto);

        // 4. Verifica
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
```

### 1.3 I Factory Methods di CustomWebApplicationFactory

La factory offre metodi per creare client con ruoli diversi:

```csharp
var adminClient    = _factory.CreateAdminClient();     // Role=Admin
var powerUserClient = _factory.CreatePowerUserClient(); // Role=PowerUser
var userClient     = _factory.CreateUserClient();       // Role=User
var anonymous      = _factory.CreateAnonymousClient();  // Nessun header → 401
```

---

## 2. Introduzione ai Test di Integrazione

### 2.1 Cosa sono i Test di Integrazione?

I **test di integrazione** verificano che **più componenti** funzionino correttamente **insieme**. A differenza dei test unitari che testano un singolo metodo in isolamento, i test di integrazione testano le **interazioni** tra:

- Un endpoint HTTP è il service layer
- Il service layer è il database
- Più services che lavorano insieme
- L'intera richiesta HTTP → Service → Database → Response

```
┌──────────────────────────────────────────────────────────────────────┐
│                    TEST DI INTEGRAZIONE                             │
│                                                                      │
│  ┌────────────────┐        ┌──────────────┐        ┌─────────────┐ │
│  │   HTTP Client  │ ────>  │ API Endpoint │ ────>  │    Service  │ │
│  │                │        │              │        │             │ │
│  └────────────────┘        └──────────────┘        └──────┬──────┘ │
│                                                            │         │
│                                                            ▼         │
│                                                    ┌─────────────┐ │
│                                                    │   Database   │ │
│                                                    │  (InMemory)  │ │
│                                                    └─────────────┘ │
│                                                              ▲     │
└──────────────────────────────────────────────────────────────┼─────┘
                                                               │
                                                         ══════╩══════
                                                          TEST
                                                          CODE
```

### 2.2 Perché scrivere Test di Integrazione?

| Vantaggio | Descrizione |
|-----------|-------------|
| **Verifica integrità dell'API** | Testa che gli endpoint HTTP funzionino correttamente |
| **Test serializzazione JSON** | Verifica che request/response JSON siano corretti |
| **Valida codici HTTP** | Assicura che vengano restituiti i codici HTTP corretti (200, 404, 400, ecc.) |
| **Test flussi completi** | Verifica che un'intera richiesta HTTP funzioni end-to-end |
| **Preventivi** | Trova problemi di integrazione prima che vadano in produzione |

### 2.3 Cosa NON sono i Test di Integrazione

```
┌────────────────────────────────────────────────────────────────────┐
│                    TIPOLOGIA DI TEST                              │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │  UNIT TEST                                                 │    │
│  │  • Testa singole classi/metodi                            │    │
│  │  • Usa mock per isolare il codice                         │    │
│  │  • Molto veloci (< 10ms)                                  │    │
│  │  • Non usa database reali                                 │    │
│  └──────────────────────────────────────────────────────────┘    │
│                          ↑                                       │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │  INTEGRATION TEST (Quello che facciamo noi)              │    │
│  │  • Testa interazioni tra componenti                      │    │
│  │  • Usa componenti reali (non mock)                       │    │
│  │  • Più lenti (~100-500ms)                                │    │
│  │  • Usa database in memoria                               │    │
│  └──────────────────────────────────────────────────────────┘    │
│                          ↑                                       │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │  END-TO-END TEST (E2E)                                   │    │
│  │  • Testa l'intera applicazione                           │    │
│  │  • Usa database reali, servizi esterni                   │    │
│  │  • Molto lenti (> 1 secondo)                             │    │
│  │  • Spesso usa browser reali (Selenium)                   │    │
│  └──────────────────────────────────────────────────────────┘    │
└────────────────────────────────────────────────────────────────────┘
```

---

## 3. Test di Integrazione vs Test Unitari

### 3.1 Confronto Diretto

| Aspetto | Unit Test | Integration Test |
|---------|-----------|-------------------|
| **Cosa testa** | Singolo metodo/classe | Interazioni tra componenti |
| **Dipendenze** | Mock (simulati) | Reali (in-memory) |
| **Database** | InMemory per singolo test | InMemory per tutti i test |
| **HTTP** | Mai usato | Sempre usato |
| **Velocità** | Molto veloce (~10ms) | Più lento (~100-500ms) |
| **Affidabilità** | Molto affidabile | Meno affidabile (più punti di fallimento) |
| **Manutenzione** | Facile | Più complessa |

### 3.2 Quando usare quali?

```csharp
// ═════════════════════════════════════════════════════════════════
// UNIT TEST: Testa la logica del servizio
// ═════════════════════════════════════════════════════════════════
[Fact]
public async Task CreateAsync_WithValidData_CreatesRegista()
{
    var service = new RegistaService(mockContext, mockLogger);
    var result = await service.CreateAsync(dto);
    result.Should().NotBeNull();
}

// ═════════════════════════════════════════════════════════════════
// INTEGRATION TEST: Testa l'intera richiesta HTTP
// ═════════════════════════════════════════════════════════════════
[Fact]
public async Task CreateRegista_WithValidData_ReturnsCreated()
{
    var response = await client.PostAsJsonAsync("/registi", dto);
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var result = await response.Content.ReadFromJsonAsync<RegistaDTO>();
    result.Should().NotBeNull();
}
```

---

## 4. Setup dell'Ambiente di Integration Testing

### 4.1 Dipendenze NuGet Necessarie

```xml
<ItemGroup>
  <!-- Framework di testing -->
  <PackageReference Include="xunit" Version="2.9.2" />

  <!-- Test integrati ASP.NET Core -->
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.11" />

  <!-- Database in memoria -->
  <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.11" />

  <!-- Assertion leggibili -->
  <PackageReference Include="FluentAssertions" Version="8.8.0" />
</ItemGroup>
```

### 4.2 Struttura del Progetto

```
tests/backend/
├── FilmAPI.Tests.csproj
├── Unit/
│   ├── RegistaServiceTests.cs
│   └── ...
└── Integration/
    ├── CustomWebApplicationFactory.cs   ← IL CUORE
    ├── ApiIntegrationTests.cs
    ├── ProgrammazioneIntegrationTests.cs
    ├── SalaIntegrationTests.cs
    └── ...
```

### 4.3 Accesso alla classe Program

Perché i test di integrazione possano creare un'istanza dell'applicazione, la classe `Program` deve essere accessibile al progetto di test:

```csharp
// Program.cs - Alla fine del file
public partial class Program { }
```

```csharp
// FilmAPI.Tests.csproj - Riferimento al progetto principale
<ItemGroup>
  <ProjectReference Include="..\FilmAPI\FilmAPI.csproj" />
  <InternalsVisibleTo Include="FilmAPI.Tests" />
</ItemGroup>
```

---

## 5. CustomWebApplicationFactory - Il Cuore degli Integration Test

### 5.1 Cos'è CustomWebApplicationFactory?

`CustomWebApplicationFactory` estende `WebApplicationFactory<Program>` di ASP.NET Core e fa tre cose fondamentali:

1. **Avvia l'app in-memory** — senza porte di rete reali
2. **Sostituisce il DB** — da MySQL a SQLite in-memory
3. **Sostituisce l'auth JWT** — con `TestAuthHandler` che legge il ruolo da header HTTP

```csharp
public sealed class CustomWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Connessione SQLite in-memory
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Rimuove il DbContext di produzione (MySQL)
            services.RemoveAll<IDbContextOptionsConfiguration<FilmDbContext>>();
            services.RemoveAll<DbContextOptions<FilmDbContext>>();
            services.RemoveAll<FilmDbContext>();

            // Aggiunge SQLite in-memory
            services.AddDbContext<FilmDbContext>(options =>
                options.UseSqlite(_connection));

            // Sostituisce JWT auth con TestAuthHandler
            services.RemoveAll<IConfigureOptions<AuthenticationOptions>>();
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            // Crea lo schema DB
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
            db.Database.EnsureCreated();
        });
    }

    // Apre la connessione (necessario per SQLite in-memory)
    public async Task InitializeAsync() => await _connection.OpenAsync();

    // Reset completo del DB + opzionale seed
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

    // Factory methods per client preconfigurati
    public HttpClient CreateAdminClient(int userId = 1) { ... }
    public HttpClient CreatePowerUserClient(int userId = 1) { ... }
    public HttpClient CreateUserClient(int userId = 1) { ... }
    public HttpClient CreateAuthenticatedClient(string role, int userId = 1, ...) { ... }
    public HttpClient CreateAnonymousClient() { ... }
}
```

### 5.2 IClassFixture - Condividere la Factory

`IClassFixture<T>` è un'interfaccia xUnit che permette di condividere un'istanza di una classe tra tutti i test di una classe di test.

```csharp
//                    ══════════════════════════════════════
//                    IClassFixture in Azione
//                    ══════════════════════════════════════
//
//  ┌────────────────────────────────────────────────────┐
//  │  RegistaEndpointsTests                          │
//  │                                                  │
//  │  • Una sola WebApplicationFactory creata        │
//  │  • Tutti i test nella classe la condividono      │
//  │  • I test possono essere eseguiti in parallelo  │
//  │                                                  │
//  │  [Fact] Test1() ──┐                             │
//  │  [Fact] Test2() ──┼──> Condividono _factory     │
//  │  [Fact] Test3() ──┘                             │
//  └────────────────────────────────────────────────────┘
```

### 5.3 Configurare Services Specifici per i Test

Spesso vogliamo sostituire il database reale con un database in memoria:

```csharp
private HttpClient CreateClient()
{
    return _factory.WithWebHostBuilder(builder =>
    {
        builder.ConfigureServices(services =>
        {
            // 1. Trova la configurazione del DbContext esistente
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<FilmAPI.Data.FilmDbContext>));

            // 2. Rimuovi la configurazione originale
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // 3. Aggiungi il database in memoria
            services.AddDbContext<FilmAPI.Data.FilmDbContext>(options =>
            {
                // GUID unico = database isolato per ogni test
                options.UseInMemoryDatabase(Guid.NewGuid().ToString());
            });
        });
    }).CreateClient();
}
```

**Visualizzazione del Processo:**

```
┌─────────────────────────────────────────────────────────────────┐
│              WebApplicationFactory                              │
│                                                                  │
│  ┌────────────────┐     ┌─────────────────┐                   │
│  │ Configurazione │ ──> │  Rimuovi DB      │                   │
│  │    Originale   │     │     Reale        │                   │
│  └────────────────┘     └─────────┬───────┘                   │
│                                   │                             │
│                                   ▼                             │
│  ┌─────────────────────────────────────────────────────┐      │
│  │         Aggiungi InMemory Database                  │      │
│  │  • Ogni test ha il suo database isolato            │      │
│  │  • Database pulito per ogni test                   │      │
│  └─────────────────────────────────────────────────────┘      │
│                           │                                  │
│                           ▼                                  │
│  ┌─────────────────────────────────────────────────────┐      │
│  │              Crea HttpClient                         │      │
│  │  • Configurato per comunicare con TestServer       │      │
│  │  • Non usa porte reali                              │      │
│  └─────────────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────────────┘
```

---

## 6. TestAuthHandler — Come Funziona l'Auth nei Test

Invece di JWT reali, i test usano un handler di autenticazione fake che legge il ruolo da header HTTP:

```csharp
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Se non c'è header X-Test-Role → non autenticato (401)
        if (!Request.Headers.ContainsKey("X-Test-Role"))
            return Task.FromResult(AuthenticateResult.NoResult());

        // Legge ruolo, userId, email dagli header
        var role = Request.Headers["X-Test-Role"].ToString();
        var userId = Request.Headers["X-Test-UserId"].FirstOrDefault() ?? "1";
        var email = Request.Headers["X-Test-Email"].FirstOrDefault() ?? "test@test.com";

        // Crea un ClaimsPrincipal con il ruolo richiesto
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim("sub", userId),
            new Claim("nome", "Test")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

Questo permette di testare RBAC facilmente:

```csharp
// Client admin → passa [RequireAuthorization("AdminOnly")]
var adminClient = _factory.CreateAdminClient();

// Client user → riceve 403 su [RequireAuthorization("AdminOnly")]
var userClient = _factory.CreateUserClient();

// Client anonimo → riceve 401 su [RequireAuthorization("Authenticated")]
var anonymousClient = _factory.CreateAnonymousClient();
```

---

## 7. HttpClient per Testare API

### 7.1 Il Testing con HttpClient

Negli integration test, usiamo `HttpClient` per fare richieste HTTP verso la nostra API:

```csharp
[Fact]
public async Task GetRegisti_ReturnsEmptyList_WhenNoRegistiExist()
{
    // ARRANGE: Ottieni un HTTP client configurato
    using var client = CreateClient();

    // ACT: Fai una richiesta HTTP GET
    var response = await client.GetAsync("/registi");

    // ASSERT: Verifica risposta HTTP
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var registi = await response.Content.ReadFromJsonAsync<IEnumerable<RegistaDTO>>();
    registi.Should().BeEmpty();
}
```

### 7.2 Metodi HTTP Comuni

| Metodo | Extension Method | Descrizione |
|--------|------------------|-------------|
| `GET` | `client.GetAsync(url)` | Ottieni risorse |
| `POST` | `client.PostAsJsonAsync(url, data)` | Crea nuova risorsa |
| `PUT` | `client.PutAsJsonAsync(url, data)` | Aggiorna risorsa |
| `DELETE` | `client.DeleteAsync(url)` | Elimina risorsa |

### 7.3 Verificare Codici HTTP

```csharp
using System.Net; // Per HttpStatusCode

// 200 OK
response.StatusCode.Should().Be(HttpStatusCode.OK);

// 201 Created
response.StatusCode.Should().Be(HttpStatusCode.Created);

// 204 No Content
response.StatusCode.Should().Be(HttpStatusCode.NoContent);

// 400 Bad Request
response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

// 404 Not Found
response.StatusCode.Should().Be(HttpStatusCode.NotFound);
```

### 7.4 Leggere il Response Body

```csharp
// Leggere come JSON tipizzato
var result = await response.Content.ReadFromJsonAsync<RegistaDTO>();
result.Should().NotBeNull();
result!.Cognome.Should().Be("Rossi");

// Leggere come collezione
var registi = await response.Content.ReadFromJsonAsync<IEnumerable<RegistaDTO>>();
registi.Should().HaveCountGreaterThan(0);
```

---

## 8. Database SQLite InMemory per Integration Test

### 8.1 Perché Usare SQLite InMemory?

| Aspetto | Database Reale | InMemory Database |
|---------|----------------|-------------------|
| **Velocità** | Lento (I/O disco o rete) | Veloce (tutto in RAM) |
| **Setup** | Complesso (installazione, configurazione) | Semplice (nessuna installazione) |
| **Isolamento** | Difficile (condiviso tra test) | Semplice (ogni test ha il suo) |
| **Affidabilità** | Più fedele alla produzione | Meno fedele (alcune feature non supportate) |
| **Uso ideale** | Test E2E o smoke tests | Test di integrazione |

### 8.2 Il Pattern ResetDatabaseAsync con Seed

Ogni test resetta il DB e opzionalmente seed dati iniziali:

```csharp
[Fact]
public async Task S2_GetSaleByCinema_ReturnsSaleForCinema()
{
    // Reset DB + seed cinema e sala
    await _factory.ResetDatabaseAsync(db => SeedCinemaAndSaleAsync(db));
    
    var client = _factory.CreateAnonymousClient();
    var response = await client.GetAsync("/cinemas/1/sale");
    
    response.EnsureSuccessStatusCode();
    var payload = await response.Content.ReadFromJsonAsync<List<SalaDTO>>();
    Assert.Single(payload);
}

// Funzione di seed — riceve il DbContext e popola i dati
private static async Task SeedCinemaAndSaleAsync(FilmDbContext db)
{
    var cinema = new Cinema { Nome = "Cinema Test", Citta = "Roma", Indirizzo = "Via Test 1" };
    db.Cinemas.Add(cinema);
    await db.SaveChangesAsync();

    var sala = new Sala
    {
        CinemaId = cinema.Id,
        NumeroProgressivo = 1,
        TipoSala = TipoSala.DueD,
        Nome = "Sala 1",
        Supplemento = 0,
        IsAttiva = true
    };
    db.Sale.Add(sala);
    await db.SaveChangesAsync();
}
```

### 8.3 Differenze Tra SQLite InMemory e Database Reale

```csharp
// GUID univoco = database isolato per ogni test
options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());
```

**Perché usare GUID?**
- ✅ Garantisce isolamento completo tra test
- ✅ Non serve cleanup manuale
- ✅ Test possono girare in parallelo
- ✅ Nessun effetto collaterale tra test

```csharp
// Esempio di isolamento
[Test1] CreateDatabase("guid-1") // Usa database "guid-1"
[Test2] CreateDatabase("guid-2") // Usa database "guid-2"
[Test3] CreateDatabase("guid-3") // Usa database "guid-3"
// Ogni test ha il suo database pulito!
```

---

## 9. Esempi Pratici dal Progetto FilmAPI

### 9.1 Struttura Completa di una Classe di Test

```csharp
using System.Net;
using System.Net.Http.Json;
using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Tests.Integration;

namespace FilmAPI.Tests.Integration;

public class SalaIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    // Factory condivisa tra tutti i test
    private readonly CustomWebApplicationFactory _factory;

    // Constructor: xUnit inietta la factory automaticamente
    public SalaIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ═════════════════════════════════════════════════════════════════
    // TEST: GET /sale/{salaId} — Sala esistente
    // ═════════════════════════════════════════════════════════════════
    [Fact]
    public async Task S3_GetSalaById_ReturnsSala()
    {
        // ARRANGE: reset DB + seed
        await _factory.ResetDatabaseAsync(db => SeedCinemaAndSaleAsync(db));
        var client = _factory.CreateAnonymousClient();

        // ACT: chiamata HTTP GET
        var response = await client.GetAsync("/sale/1");

        // ASSERT: verifica status code e body
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<SalaDTO>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload.NumeroProgressivo);
    }

    // ═════════════════════════════════════════════════════════════════
    // TEST: POST /cinemas/{cinemaId}/sale — Crea sala (Admin)
    // ═════════════════════════════════════════════════════════════════
    [Fact]
    public async Task S5_CreateSala_ReturnsCreated()
    {
        // ARRANGE
        await _factory.ResetDatabaseAsync(db => SeedCinemaAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new SalaCreateDTO
        {
            CinemaId = 1,
            NumeroProgressivo = 1,
            TipoSala = TipoSala.DueD,
            Supplemento = 0
        };

        // ACT: POST con JSON body
        var response = await client.PostAsJsonAsync("/cinemas/1/sale", dto);

        // ASSERT: verifica 201 Created
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<SalaDTO>();
        Assert.NotNull(payload);
        Assert.Equal("Sala 1", payload.Nome); // Nome auto-generato
    }

    // ═════════════════════════════════════════════════════════════════
    // TEST: POST /cinemas/{cinemaId}/sale — Duplicato (409 Conflict)
    // ═════════════════════════════════════════════════════════════════
    [Fact]
    public async Task S6_CreateSala_ConflictOnDuplicateNumero()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaAndSaleAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new SalaCreateDTO
        {
            CinemaId = 1,
            NumeroProgressivo = 1, // già esistente
            TipoSala = TipoSala.TreD,
            Supplemento = 2
        };

        var response = await client.PostAsJsonAsync("/cinemas/1/sale", dto);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ═════════════════════════════════════════════════════════════════
    // TEST: DELETE /sale/{salaId} — Bloccato da show futuri (409)
    // ═════════════════════════════════════════════════════════════════
    [Fact]
    public async Task S13_DeleteSala_BlockedByFutureShows()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaSalaAndFutureShowAsync(db));
        var client = _factory.CreateAdminClient();

        var response = await client.DeleteAsync("/sale/1");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ═════════════════════════════════════════════════════════════════
    // TEST: PUT /sale/{salaId}/posti — Salva piantina
    // ═════════════════════════════════════════════════════════════════
    [Fact]
    public async Task S16_SavePosti_CreatesLayout()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaAndSaleAsync(db));
        var client = _factory.CreateAdminClient();

        var dto = new SalaLayoutSaveDTO
        {
            Posti = new List<SalaPostoDTO>
            {
                new() { Settore = "PLATEA", Fila = 1, Numero = 1 },
                new() { Settore = "PLATEA", Fila = 1, Numero = 2 },
                new() { Settore = "PLATEA", Fila = 2, Numero = 1 },
            }
        };

        var response = await client.PutAsJsonAsync("/sale/1/posti", dto);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<List<SalaPostoDTO>>();
        Assert.NotNull(payload);
        Assert.Equal(3, payload.Count);
    }

    // ═════════════════════════════════════════════════════════════════
    // TEST: RBAC — User non può creare sala (403 Forbidden)
    // ═════════════════════════════════════════════════════════════════
    [Fact]
    public async Task S8_CreateSala_ForbiddenForUser()
    {
        await _factory.ResetDatabaseAsync(db => SeedCinemaAsync(db));
        var client = _factory.CreateUserClient(); // Role=User, non Admin

        var dto = new SalaCreateDTO
        {
            CinemaId = 1,
            NumeroProgressivo = 1,
            TipoSala = TipoSala.DueD,
            Supplemento = 0
        };

        var response = await client.PostAsJsonAsync("/cinemas/1/sale", dto);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Helper di seed
    private static async Task SeedCinemaAsync(FilmDbContext db)
    {
        var cinema = new Cinema { Nome = "Cinema Test", Citta = "Roma", Indirizzo = "Via Test 1" };
        db.Cinemas.Add(cinema);
        await db.SaveChangesAsync();
    }

    private static async Task SeedCinemaAndSaleAsync(FilmDbContext db)
    {
        await SeedCinemaAsync(db);
        var sala = new Sala
        {
            CinemaId = 1,
            NumeroProgressivo = 1,
            TipoSala = TipoSala.DueD,
            Nome = "Sala 1",
            Supplemento = 0,
            IsAttiva = true
        };
        db.Sale.Add(sala);
        await db.SaveChangesAsync();
    }

    private static async Task SeedCinemaSalaAndFutureShowAsync(FilmDbContext db)
    {
        await SeedCinemaAndSaleAsync(db);
        // ... seed regista, film, show futuro
    }
}
```

### 9.2 Flusso Completo di un Test di Integrazione

```
┌─────────────────────────────────────────────────────────────────┐
│             FLUSSO COMPLETO DI UN INTEGRATION TEST              │
└─────────────────────────────────────────────────────────────────┘

    1. ARRANGE - Preparazione
    ┌────────────────────────────────────────────────────────────┐
    │ using var client = CreateClient();                         │
    │ │                                                          │
    │ └─> Crea HttpClient con database in memoria isolato       │
    └────────────────────────────────────────────────────────────┘
                           │
                           ▼
    2. PREPARAZIONE DATI (se necessario)
    ┌────────────────────────────────────────────────────────────┐
    │ var createResponse = await client.PostAsJsonAsync(...);     │
    │ var created = await createResponse.Content...              │
    └────────────────────────────────────────────────────────────┘
                           │
                           ▼
    3. ACT - Esecuzione
    ┌────────────────────────────────────────────────────────────┐
    │ var response = await client.GetAsync("/registi/1");        │
    │ └─> Fai richiesta HTTP verso l'API                         │
    └────────────────────────────────────────────────────────────┘
                           │
                           ▼
    4. ASSERT - Verifica Status Code
    ┌────────────────────────────────────────────────────────────┐
    │ response.StatusCode.Should().Be(HttpStatusCode.OK);        │
    │ └─> Verifica che il codice HTTP sia quello atteso        │
    └────────────────────────────────────────────────────────────┘
                           │
                           ▼
    5. ASSERT - Verifica Response Body
    ┌────────────────────────────────────────────────────────────┐
    │ var result = await response.Content.ReadFromJsonAsync<>();│
    │ result.Should().NotBeNull();                               │
    │ └─> Verifica che i dati restituiti siano corretti        │
    └────────────────────────────────────────────────────────────┘
```

---

## 10. Best Practices

### 10.1 Organizzazione dei Test

```csharp
public class RegistaEndpointsTests
{
    // Gruppo 1: GET endpoints
    [Fact]
    public async Task GetAll_ReturnsList() { }

    [Fact]
    public async Task GetById_WhenExists_ReturnsRegista() { }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404() { }

    // Gruppo 2: POST endpoint
    [Fact]
    public async Task Create_WithValidData_Returns201() { }

    [Fact]
    public async Task Create_WithInvalidData_Returns400() { }

    // Gruppo 3: PUT endpoint
    [Fact]
    public async Task Update_WhenExists_Returns200() { }

    [Fact]
    public async Task Update_WhenNotFound_Returns404() { }

    // Gruppo 4: DELETE endpoint
    [Fact]
    public async Task Delete_WhenExists_Returns204() { }

    [Fact]
    public async Task Delete_WhenNotFound_Returns404() { }

    // Gruppo 5: Nested endpoints
    [Fact]
    public async Task GetFilms_WhenRegistaHasNoFilms_ReturnsEmpty() { }
}
```

### 10.2 Checklist per Ogni Endpoint

Per ogni endpoint API, dovresti testare:

```
☐ GET con risorsa esistente → 200 OK con dati corretti
☐ GET con risorsa inesistente → 404 Not Found
☐ GET con lista vuota → 200 OK con array vuoto
☐ POST con dati validi → 201 Created con Location header
☐ POST con dati invalidi → 400 Bad Request
☐ PUT con risorsa esistente → 200 OK con dati aggiornati
☐ PUT con risorsa inesistente → 404 Not Found
☐ DELETE con risorsa esistente → 204 No Content
☐ DELETE con risorsa inesistente → 404 Not Found
```

### 10.3 Errori Comuni da Evitare

```csharp
// ❌ ERRORE: Non verifica il body della risposta
[Fact]
public async Task Create_ReturnsCreated()
{
    var response = await client.PostAsJsonAsync("/registi", dto);
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    // Manca: verifica che il response sia corretto!
}

// ✅ CORRETTO: Verifica tutto
[Fact]
public async Task Create_ReturnsCreated()
{
    var response = await client.PostAsJsonAsync("/registi", dto);
    response.StatusCode.Should().Be(HttpStatusCode.Created);

    var result = await response.Content.ReadFromJsonAsync<RegistaDTO>();
    result.Should().NotBeNull();
    result!.Cognome.Should().Be("Rossi");
}

// ❌ ERRORE: Hardcoded ID
[Fact]
public async Task GetById_ReturnsRegista()
{
    var response = await client.GetAsync("/registi/1");
    // Se l'ID 1 non esiste, il test fallisce!
}

// ✅ CORRETTO: Crea prima il dato
[Fact]
public async Task GetById_ReturnsRegista()
{
    // Prima crea il regista
    var created = await CreateRegista(client);
    var response = await client.GetAsync($"/registi/{created.Id}");
}
```

### 10.4 Eseguire gli Integration Test

```bash
# Esegui solo test di integrazione
dotnet test --filter "FullyQualifiedName~Integration"

# Esegui con output dettagliato
dotnet test --filter "FullyQualifiedName~Integration" --logger "console;verbosity=detailed"

# Esegui una classe specifica
dotnet test --filter "FullyQualifiedName~SalaIntegrationTests"

# Esegui un test specifico
dotnet test --filter "FullyQualifiedName~S5_CreateSala_ReturnsCreated"
```

---

## Riepilogo

### Concetti Chiave

| Concetto | Descrizione |
|----------|-------------|
| **Integration Test** | Testa interazioni tra componenti dell'applicazione |
| **WebApplicationFactory** | Crea un'istanza dell'app per il testing |
| **IClassFixture** | Condivide la factory tra tutti i test della classe |
| **HttpClient** | Fa richieste HTTP verso l'API |
| **InMemory Database** | Database simulato per velocizzare i test |
| **Arrange-Act-Assert** | Pattern per organizzare i test |

### Prossimi Passi

1. **Scrivi il tuo primo integration test** → Copia un test esistente e modificane l'endpoint
2. **Testa tutti gli endpoint** → Segui la checklist per ogni endpoint
3. **Esegui i test regolarmente** → Ogni volta che modifichi un endpoint
4. **Mantieni i test aggiornati** → Quando cambi API, aggiorna anche i test

### Risorse

- [Microsoft Docs: Integration Tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [WebApplicationFactory Documentation](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests#webapplicationfactory)

---

**Documento creato il:** 10 Marzo 2026
**Versione:** 1.0
**Progetto:** FilmAPI - Tutorial Test di Integrazione
