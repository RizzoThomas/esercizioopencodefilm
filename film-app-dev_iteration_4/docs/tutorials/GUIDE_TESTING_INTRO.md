# Guida Introduttiva al Testing in FilmAPI

**Autore:** Claude AI Assistant
**Data:** 16 Marzo 2026
**Ultimo aggiornamento:** 17 Aprile 2026
**Progetto di Riferimento:** FilmAPI
**Linguaggio:** C# / .NET 9.0

---

## Indice

1. [Come Partono i Test: Entry Point e Discovery](#1-come-partono-i-test-entry-point-e-discovery)
2. [Panoramica della Strategia di Testing](#2-panoramica-della-strategia-di-testing)
3. [Esecuzione dei Test](#3-esecuzione-dei-test)
4. [Architettura dei Test](#4-architettura-dei-test)
5. [Stack di Database Doppio: Produzione e Testing](#5-stack-di-database-doppio-produzione-e-testing)
6. [Introduzione dei Servizi e Dependency Injection](#6-introduzione-dei-servizi-e-dependency-injection)
7. [Comandi Utili](#7-comandi-utili)

---

## 1. Come Partono i Test: Entry Point e Discovery

### 1.1 Non Esiste un `Main` — Chi Esegue i Test?

Una delle domande più comuni per chi si avvicina al testing in .NET è: **"dove è il metodo `Main` che fa partire i test?"**. La risposta è: **non esiste**. Non c'è nessun file di ingresso esplicito.

Il motore di esecuzione è il comando `dotnet test`, che funziona così:

```
dotnet test
    │
    ▼
Microsoft.NET.Test.Sdk (pacchetto NuGet)
    │
    ▼
Scopre l'assembly compilato (.dll)
    │
    ▼
xUnit Test Runner (xunit.runner.visualstudio)
    │
    ▼
Scansiona tutte le classi con reflection
    │
    ▼
Trova metodi marcati con [Fact] o [Theory]
    │
    ▼
Esegue ogni test in parallelo (di default)
    │
    ▼
Aggrega risultati → Output console
```

Ogni metodo decorato con `[Fact]` è un **entry point indipendente**. Il test runner li scopre automaticamente tramite reflection sull'assembly compilato.

### 1.2 Esempio Pratico: Cosa Succede Quando Esegui `dotnet test`

```bash
$ dotnet test tests/backend/FilmAPI.Tests.csproj
```

1. **Build**: Compila `FilmAPI.csproj` e `FilmAPI.Tests.csproj`
2. **Discovery**: xUnit scansiona `FilmAPI.Tests.dll` e trova tutte le classi con `[Fact]`
3. **Execution**: Per ogni classe di test:
   - Crea un'istanza della classe (chiama il costruttore)
   - Se implementa `IClassFixture<CustomWebApplicationFactory>`, crea la factory e la inietta
   - Esegue ogni metodo `[Fact]` (in parallelo se possibile)
   - Chiama `DisposeAsync()` se presente
4. **Report**: Stampa il riepilogo (`Passed: 143, Failed: 0`)

### 1.3 Anatomia di una Classe di Test

```csharp
// IClassFixture → xUnit crea UNA factory condivisa per tutta la classe
// è la inietta nel costruttore. Tutti i test nella classe condividono
// la stessa istanza dell'applicazione in-memory.
public class SalaIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    // Il costruttore riceve la factory da xUnit
    public SalaIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // [Fact] = "Questo metodo è un test, eseguilo"
    [Fact]
    public async Task S5_CreateSala_ReturnsCreated()
    {
        // 1. Reset del DB + seed dati iniziali
        await _factory.ResetDatabaseAsync(db => SeedCinemaAsync(db));

        // 2. Crea client HTTP con ruolo Admin
        var client = _factory.CreateAdminClient();

        // 3. Chiama l'endpoint REALE dell'app in-memory
        var response = await client.PostAsJsonAsync("/cinemas/1/sale", dto);

        // 4. Verifica il risultato
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
```

### 1.4 Struttura del Progetto di Test

```
tests/backend/
├── FilmAPI.Tests.csproj            ← Progetto di test (riferimento a FilmAPI)
├── Unit/                           ← Test unitari (chiamata diretta ai service)
│   ├── RegistaServiceTests.cs      ← 12 test per RegistaService
│   ├── FilmServiceTests.cs         ← Test per FilmService
│   ├── CinemaServiceTests.cs       ← Test per CinemaService
│   └── ProiezioneServiceTests.cs   ← Test per ProiezioneService
└── Integration/                    ← Test di integrazione (chiamate HTTP)
    ├── CustomWebApplicationFactory.cs   ← IL CUORE: app in-memory + SQLite + auth fake
    ├── ApiIntegrationTests.cs           ← Test endpoint legacy
    ├── ProgrammazioneIntegrationTests.cs ← Test catalogo pubblico (20 test)
    ├── SalaIntegrationTests.cs          ← Test CRUD sale (20 test)
    └── ... altri file di test
```

---

## 2. Panoramica della Strategia di Testing

### 2.1 Test Unitari

I test unitari verificano il comportamento di singole unità di codice, tipicamente metodi all'interno delle classi service, in isolamento dalle dipendenze esterne. Questi test utilizzano un database InMemory per simulare le operazioni di persistenza senza modificare i dati di produzione.

I test unitari offrono numerosi vantaggi: eseguono in tempi estremamente rapidi (millisecondi), non richiedono infrastruttura esterna, garantiscono isolamento completo tra un test e l'altro, e permettono di verificare la logica di business in modo focalizzato.

### 2.2 Test di Integrazione

I test di integrazione verificano che gli endpoint HTTP dell'API funzionino correttamente quando chiamati da un client esterno. A differenza dei test unitari, coinvolgono l'intera catena di elaborazione: routing, validazione, service layer, database e serializzazione della risposta.

Questa tipologia di test utilizza `CustomWebApplicationFactory` che estende `WebApplicationFactory` di ASP.NET Core per creare un'istanza in-memory dell'applicazione completa, con **SQLite in-memory** come database e un **auth handler fake** per simulare i ruoli utente.

### 2.3 Tabella Comparativa

| Caratteristica | Test Unitari | Test di Integrazione |
|----------------|--------------|----------------------|
| Scope | Singolo metodo/classe | Intera richiesta HTTP |
| Database | InMemory (EF Core) | SQLite in-memory |
| HTTP | Mai usato | Sempre usato (HttpClient) |
| Auth | Non applicabile | Simulata con TestAuthHandler |
| Tempo di esecuzione | < 10ms | 100-500ms |
| Dipendenze esterne | Nessuna | Nessuna (tutto in-memory) |
| Numero nel progetto | ~40 | ~100+ |

---

## 3. Esecuzione dei Test

### 3.1 Comandi Fondamentali

```bash
# Esegue tutti i test presenti nel progetto
dotnet test tests/backend/FilmAPI.Tests.csproj

# Esegue i test unitari
dotnet test --filter "FullyQualifiedName~Unit"

# Esegue i test di integrazione
dotnet test --filter "FullyQualifiedName~Integration"

# Esegue un test specifico per nome
dotnet test --filter "FullyQualifiedName~SalaIntegrationTests.S5"

# Esegue i test saltando la ricompilazione (se già compilati)
dotnet test --no-build

# Esegue i test con una classe specifica
dotnet test --filter "FullyQualifiedName~SalaIntegrationTests"
```

### 3.2 Build dei Progetti di Test

```bash
# Compila il progetto principale FilmAPI
dotnet build backend/FilmAPI/FilmAPI.csproj

# Compila il progetto di test
dotnet build tests/backend/FilmAPI.Tests.csproj
```

### 3.3 Configurazione del Progetto di Test

Il file `FilmAPI.Tests.csproj` contiene le dipendenze necessarie:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.11" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.11" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.11" />
    <PackageReference Include="FluentAssertions" Version="8.8.0" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\backend\FilmAPI\FilmAPI.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
    <Using Include="FluentAssertions" />
  </ItemGroup>
</Project>
```

**Pacchetti chiave:**
- `Microsoft.NET.Test.Sdk`: Il motore che permette a `dotnet test` di scoprire ed eseguire i test
- `xunit`: Framework di testing con attributi `[Fact]` e `[Theory]`
- `xunit.runner.visualstudio`: Il "ponte" tra `dotnet test` e xUnit
- `Microsoft.AspNetCore.Mvc.Testing`: Fornisce `WebApplicationFactory` per test HTTP in-memory

---

## 4. Architettura dei Test

### 4.1 Test Unitari: Chiamata Diretta ai Servizi

Nei test unitari, il codice sotto test viene invocato direttamente senza passare attraverso il layer HTTP:

```csharp
[Fact]
public async Task U_R1_GetAllAsync_WhenNoRegistiExist_ReturnsEmptyList()
{
    // Chiamata diretta al metodo del service
    var result = await _service.GetAllAsync();
    result.Should().BeEmpty();
}
```

### 4.2 Test di Integrazione: Chiamata agli Endpoint HTTP

Nei test di integrazione, il test effettua chiamate HTTP reali. Il flusso attraversa l'intera applicazione:

```
Test → HttpClient → Endpoint HTTP → Service → SQLite DB → Response HTTP → Test
```

```csharp
[Fact]
public async Task S5_CreateSala_ReturnsCreated()
{
    // Arrange: reset DB + client admin
    await _factory.ResetDatabaseAsync(db => SeedCinemaAsync(db));
    var client = _factory.CreateAdminClient();

    // Act: chiamata HTTP POST
    var response = await client.PostAsJsonAsync("/cinemas/1/sale", dto);

    // Assert: verifica status code
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
}
```

---

## 5. Stack di Database Doppio: Produzione e Testing

### 5.1 Il Problema della Gestione del Database nei Test

Il progetto FilmAPI adotta un approccio separato: **MySQL/MariaDB per la produzione**, **database in-memory per i test**. Il backend non contiene alcun riferimento al database usato nei test — la selezione avviene interamente nel codice di test.

### 5.2 Come Funziona la `CustomWebApplicationFactory`

La `CustomWebApplicationFactory` è il cuore dei test di integrazione. Fa tre cose fondamentali:

```csharp
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // 1. Connessione SQLite in-memory (vive solo finché la connessione è aperta)
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 2. Rimuove il DbContext di produzione (MySQL)
            services.RemoveAll<IDbContextOptionsConfiguration<FilmDbContext>>();
            services.RemoveAll<DbContextOptions<FilmDbContext>>();
            services.RemoveAll<FilmDbContext>();

            // 3. Aggiunge SQLite in-memory
            services.AddDbContext<FilmDbContext>(options =>
                options.UseSqlite(_connection));

            // 4. Sostituisce JWT auth con TestAuthHandler (legge ruolo da header)
            services.RemoveAll<IConfigureOptions<AuthenticationOptions>>();
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            // 5. Crea lo schema DB
            var db = services.BuildServiceProvider()
                             .GetRequiredService<FilmDbContext>();
            db.Database.EnsureCreated();
        });
    }

    // Apre la connessione SQLite (necessario per in-memory)
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

    // Factory methods per client preconfigurati con ruoli
    public HttpClient CreateAdminClient(int userId = 1) { ... }
    public HttpClient CreatePowerUserClient(int userId = 1) { ... }
    public HttpClient CreateUserClient(int userId = 1) { ... }
    public HttpClient CreateAnonymousClient() { ... }
}
```

### 5.3 Il `TestAuthHandler` — Come Funziona l'Auth nei Test

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

### 5.4 Differenze Tra SQLite InMemory e Database Reale

SQLite in-memory è molto più fedele a MySQL rispetto a EF Core InMemory:
- Enforce vincoli di chiave esterna
- Supporta transazioni complete
- Query SQL reali (non simulate)

Tuttavia, esistono differenze minori nei tipi di dato e nelle funzioni specifiche del database.

---

## 6. Introduzione dei Servizi e Dependency Injection

### 6.1 Il Layer Servizi

Ogni entità del dominio ha un service dedicato che incapsula la logica di business:

```
Services/
├── IRegistaService.cs / RegistaService.cs
├── IFilmService.cs / FilmService.cs
├── ICinemaService.cs / CinemaService.cs
├── ISalaService.cs / SalaService.cs          ← Iterazione 4
├── IProgrammazioneService.cs / ProgrammazioneService.cs  ← Iterazione 4
└── ...
```

### 6.2 Registrazione in Program.cs

```csharp
builder.Services.AddScoped<IRegistaService, RegistaService>();
builder.Services.AddScoped<ISalaService, SalaService>();
builder.Services.AddScoped<IProgrammazioneService, ProgrammazioneService>();
// ... altri servizi
```

### 6.3 Vantaggi per i Test

- **Unit test**: si crea un `DbContext` InMemory e si inietta nel service
- **Integration test**: la `CustomWebApplicationFactory` sostituisce il DB e l'autenticazione

---

## 7. Comandi Utili

### 7.1 Test

```bash
# Tutti i test
dotnet test tests/backend/FilmAPI.Tests.csproj

# Solo unit test
dotnet test --filter "FullyQualifiedName~Unit"

# Solo integration test
dotnet test --filter "FullyQualifiedName~Integration"

# Test specifici per classe
dotnet test --filter "FullyQualifiedName~SalaIntegrationTests"

# Test specifico per nome
dotnet test --filter "FullyQualifiedName~S5_CreateSala_ReturnsCreated"

# Senza ricompilare
dotnet test --no-build

# Con coverage
dotnet test --collect:"XPlat Code Coverage"
```

### 7.2 Build

```bash
dotnet build backend/FilmAPI/FilmAPI.csproj
dotnet build tests/backend/FilmAPI.Tests.csproj
```

---

## Riepilogo

Il progetto FilmAPI adotta un approccio strutturato al testing con **143 test automatici** (unitari + integrazione). Il motore di esecuzione è `dotnet test` che scopre automaticamente i test tramite reflection — non esiste un `Main` esplicito. I test di integrazione utilizzano `CustomWebApplicationFactory` per avviare l'app in-memory con SQLite e autenticazione simulata, permettendo di testare l'intero stack HTTP senza dipendenze esterne.

---

**Documento creato il:** 16 Marzo 2026
**Ultimo aggiornamento:** 17 Aprile 2026
**Versione:** 2.0
**Progetto:** FilmAPI - Guida Introduttiva al Testing
