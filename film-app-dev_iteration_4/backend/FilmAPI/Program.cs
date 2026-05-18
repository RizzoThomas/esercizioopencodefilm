// ============================================================================
// PROGRAM.CS — PUNTO DI INGRESSO DELL'APPLICAZIONE
// ============================================================================
// Questo file è il cuore dell'applicazione ASP.NET Core.
// Qui avviene:
//   1. Caricamento configurazione (file .env)
//   2. Registrazione di tutti i servizi (Dependency Injection)
//   3. Configurazione autenticazione JWT e autorizzazione RBAC
//   4. Configurazione CORS, Swagger, Middleware
//   5. Mappatura di tutti gli endpoint REST
//   6. Avvio dell'applicazione
// ============================================================================

using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using FilmAPI.Data;
using FilmAPI.Endpoints;
using FilmAPI.Services;

// ─── CONFIGURAZIONE INIZIALE ────────────────────────────────────────────────
// Imposta la licenza Community per QuestPDF (libreria per generazione PDF)
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// ─── CARICAMENTO FILE .ENV ──────────────────────────────────────────────────
// Cerca il file .env in diverse posizioni possibili dell'albero del progetto
// (dalla cartella backend/, dalla radice, o dalla current directory)
var envCandidates = new[]
{
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".env")),
    Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "backend", ".env")),
    Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".env"))
};

var backendEnvPath = envCandidates.FirstOrDefault(File.Exists);
if (!string.IsNullOrWhiteSpace(backendEnvPath))
{
    Env.Load(backendEnvPath);                       // Carica il .env trovato
}
else
{
    Env.Load();                                     // Fallback: cerca .env nella directory corrente
}

// ─── BUILDER: INIZIO CONFIGURAZIONE ─────────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);

// === SERVIZI DI CONFIGURAZIONE RUNTIME ===
// FrontendRuntimeConfig è un record che espone la Stripe publishable key
// al frontend tramite GET /config/frontend
builder.Services.AddSingleton(new FrontendRuntimeConfig(
    Environment.GetEnvironmentVariable("STRIPE_PUBLISHABLE_API_KEY")
    ?? Environment.GetEnvironmentVariable("STRIPE_PUBLISHABLE_KEY")
    ?? string.Empty));

// === CONFIGURAZIONE DATABASE MySQL ===
// Legge i parametri di connessione da variabili d'ambiente
// (caricate dal file .env) con valori di default per sviluppo locale
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "film-api-db";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "root";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "root";
var dbUseAutoDetect = (Environment.GetEnvironmentVariable("DB_USE_AUTODETECT") ?? "true")
    .Equals("true", StringComparison.OrdinalIgnoreCase);
var dbServerVersion = Environment.GetEnvironmentVariable("DB_SERVER_VERSION") ?? "10.11.0-mariadb";

// Costruisce la connection string per MySQL
var connectionString = $"Server={dbHost};Port={dbPort};Database={dbName};User Id={dbUser};Password={dbPassword};";
var serverVersion = dbUseAutoDetect
    ? ServerVersion.AutoDetect(connectionString)    // Rileva automaticamente versione MySQL
    : ServerVersion.Parse(dbServerVersion);          // Usa versione specificata

// Registra il DbContext nel container DI
// FilmDbContext è la classe che mappa le entità C# alle tabelle MySQL
builder.Services.AddDbContext<FilmDbContext>(
    dbContextOptions => dbContextOptions
        .UseMySql(connectionString, serverVersion)
        .LogTo(Console.WriteLine, LogLevel.Information)
        .EnableSensitiveDataLogging()
        .EnableDetailedErrors()
);

// === REGISTRAZIONE SERVIZI (Dependency Injection) ===
// AddScoped = una nuova istanza per ogni richiesta HTTP
// AddSingleton = una sola istanza per tutta l'applicazione
// AddHostedService = servizio in background (esecuzione periodica)
// AddHttpClient = client HTTP per chiamate a servizi esterni (TMDB)
builder.Services.AddScoped<IAccountTokenService, AccountTokenService>();
builder.Services.AddScoped<IAccountEmailService, AccountEmailService>();
builder.Services.AddScoped<IUserSecurityAuditService, UserSecurityAuditService>();
builder.Services.AddScoped<IRegistaService, RegistaService>();
builder.Services.AddScoped<IFilmService, FilmService>();
builder.Services.AddScoped<ICinemaService, CinemaService>();
builder.Services.AddScoped<IProiezioneService, ProiezioneService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProfiloService, ProfiloService>();
builder.Services.AddScoped<IPrenotazioneService, PrenotazioneService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddScoped<IProgrammazioneService, ProgrammazioneService>();
builder.Services.AddScoped<ISalaService, SalaService>();
builder.Services.AddScoped<IShowService, ShowService>();
builder.Services.AddScoped<ISeatHoldService, SeatHoldService>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
builder.Services.AddScoped<ICreditoService, CreditoService>();
builder.Services.AddScoped<IBigliettoService, BigliettoService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IValidazioneBigliettoService, ValidazioneBigliettoService>();
builder.Services.AddScoped<IStripePaymentGateway, StripePaymentGateway>();
builder.Services.AddScoped<IPagamentoService, PagamentoService>();
builder.Services.AddHttpClient<ITmdbService, TmdbService>();
builder.Services.AddHostedService<RefreshTokenCleanupService>();     // Pulisce i refresh token scaduti
builder.Services.AddHostedService<ExpiredHoldCleanupService>();      // Pulisce gli hold posti scaduti

// === SWAGGER / OPENAPI ===
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

// === CORS (Cross-Origin Resource Sharing) ===
// Permette al frontend (porta 5001) di chiamare il backend (porta 5000)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowCineBaseFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5001", "http://127.0.0.1:5001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithExposedHeaders("Authorization");
    });
});

// === AUTORIZZAZIONE RBAC (Role-Based Access Control) ===
// Definisce 3 policy basate sul claim "role" del JWT:
// - AdminOnly: solo utenti con ruolo Admin
// - PowerUserOrAdmin: PowerUser e Admin
// - Authenticated: qualsiasi utente loggato
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => (c.Type == "role" || c.Type == System.Security.Claims.ClaimTypes.Role) && c.Value == "Admin")));
    options.AddPolicy("PowerUserOrAdmin", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => (c.Type == "role" || c.Type == System.Security.Claims.ClaimTypes.Role) && (c.Value == "PowerUser" || c.Value == "Admin"))));
    options.AddPolicy("Authenticated", policy =>
        policy.RequireAuthenticatedUser());
});

builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "FilmAPI";
    config.Title = "FilmAPI v1";
    config.Version = "v1";
});

// === CONFIGURAZIONE JWT (JSON Web Token) ===
// Legge la chiave segreta e i parametri del token dal .env
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "SuperSecretKeyForCineBaseJWTAuth2026!";
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "CineBaseAPI";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "CineBaseWeb";

builder.Services.AddAuthentication(options =>
{
    // Imposta JWT Bearer come schema di autenticazione predefinito
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Parametri di validazione del token:
    // - Verifica che il token sia firmato con la chiave segreta
    // - Verifica che issuer e audience corrispondano
    // - Verifica che il token non sia scaduto (ValidateLifetime)
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        RoleClaimType = "role"
    };
    // Evento: quando il token viene validato, copia il claim "role"
    // nel formato standard ClaimTypes.Role per l'autorizzazione
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var identity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
            if (identity != null)
            {
                var roleClaim = identity.FindFirst("role");
                if (roleClaim != null)
                {
                    identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, roleClaim.Value));
                }
            }
            return System.Threading.Tasks.Task.CompletedTask;
        }
    };
});

// ─── SOCIAL LOGIN (OAUTH) ───────────────────────────────────────────────────
// Configura i provider OAuth esterni se le credenziali sono presenti nel .env
// Supporta: Google, Facebook, Microsoft
var frontendBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "http://localhost:5001";

// Google OAuth
var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
if (!string.IsNullOrEmpty(googleClientId))
    builder.Services.AddAuthentication().AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret ?? "";
        options.SaveTokens = true;
        options.Events.OnTicketReceived = async context =>
        {
            await SocialAuthEndpoints.ProcessOAuthTicket(
                context.HttpContext, context.Principal!, context.Properties!, "Google", frontendBaseUrl);
            context.HandleResponse();
        };
        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect($"{frontendBaseUrl}/login.html?error=access_denied");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });

// Facebook OAuth
var fbAppId = Environment.GetEnvironmentVariable("FACEBOOK_APP_ID");
var fbAppSecret = Environment.GetEnvironmentVariable("FACEBOOK_APP_SECRET");
if (!string.IsNullOrEmpty(fbAppId))
    builder.Services.AddAuthentication().AddFacebook(options =>
    {
        options.AppId = fbAppId;
        options.AppSecret = fbAppSecret ?? "";
        options.SaveTokens = true;
        options.Events.OnTicketReceived = async context =>
        {
            await SocialAuthEndpoints.ProcessOAuthTicket(
                context.HttpContext, context.Principal!, context.Properties!, "Facebook", frontendBaseUrl);
            context.HandleResponse();
        };
        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect($"{frontendBaseUrl}/login.html?error=access_denied");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });

// Microsoft OAuth (con supporto tenant specifico per Azure AD)
var msClientId = Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_ID");
var msClientSecret = Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_SECRET");
var msTenantId = Environment.GetEnvironmentVariable("MICROSOFT_TENANT_ID") ?? "organizations";
Console.WriteLine($"[STARTUP] Microsoft Auth: ClientId={msClientId?[..Math.Min(8, msClientId?.Length ?? 0)]}..., TenantId={msTenantId}, SecretPresent={!string.IsNullOrEmpty(msClientSecret)}");
Console.WriteLine($"[STARTUP] Microsoft Auth: AuthEndpoint=https://login.microsoftonline.com/{msTenantId}/oauth2/v2.0/authorize");
Console.WriteLine($"[STARTUP] Microsoft Auth: TokenEndpoint=https://login.microsoftonline.com/{msTenantId}/oauth2/v2.0/token");
if (!string.IsNullOrEmpty(msClientId))
    builder.Services.AddAuthentication().AddMicrosoftAccount(options =>
    {
        options.ClientId = msClientId;
        options.ClientSecret = msClientSecret ?? "";
        options.SaveTokens = true;
        options.CallbackPath = "/signin-microsoft";
        // Usa tenant specifico per la scuola (single-tenant app in Azure AD)
        options.AuthorizationEndpoint = $"https://login.microsoftonline.com/{msTenantId}/oauth2/v2.0/authorize";
        options.TokenEndpoint = $"https://login.microsoftonline.com/{msTenantId}/oauth2/v2.0/token";
        options.Events.OnTicketReceived = async context =>
        {
            Console.WriteLine($"[AUTH] Microsoft OnTicketReceived OK - processing user");
            await SocialAuthEndpoints.ProcessOAuthTicket(
                context.HttpContext, context.Principal!, context.Properties!, "Microsoft", frontendBaseUrl);
            context.HandleResponse();
        };
        options.Events.OnRemoteFailure = context =>
        {
            Console.WriteLine($"[AUTH] Microsoft OnRemoteFailure: {context.Failure?.Message}");
            Console.WriteLine($"[AUTH] Microsoft OnRemoteFailure: {context.Failure?.InnerException?.Message}");
            Console.WriteLine($"[AUTH] Microsoft OnRemoteFailure stack: {context.Failure?.StackTrace}");
            context.Response.Redirect($"{frontendBaseUrl}/login.html?error=access_denied");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });
else
    Console.WriteLine("[STARTUP] Microsoft Auth: DISABLED (no MICROSOFT_CLIENT_ID)");

// === COSTRUZIONE APPLICAZIONE ===
var app = builder.Build();

// ─── MIDDLEWARE PIPELINE (ORDINE IMPORTANTE!) ───────────────────────────────
// L'ordine dei middleware è fondamentale:
// 1. CORS        → permette richieste cross-origin
// 2. RateLimiter → limita il numero di richieste (antiflood)
// 3. Authentication → legge il JWT e identifica l'utente
// 4. Authorization  → controlla i permessi (RBAC)
// 5. StaticFiles    → serve i file statici (frontend)
app.UseCors("AllowCineBaseFrontend");
app.UseMiddleware<FilmAPI.Middleware.RateLimiterMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

// === SWAGGER (solo in sviluppo) ===
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

// ─── MAPPATURA ENDPOINT ─────────────────────────────────────────────────────
// Ogni gruppo di endpoint viene mappato a un percorso specifico
// Esempio: app.MapAuthEndpoints() mappa /auth/register, /auth/login, etc.
app.MapRegistiEndpoints();
app.MapFilmsEndpoints();
app.MapCinemasEndpoints();
app.MapProiezioniEndpoints();
app.MapMediaEndpoints();
app.MapCategorieEndpoints();
app.MapAuthEndpoints();
app.MapProfiloEndpoints();
app.MapPrenotazioniEndpoints();
app.MapAdminUtentiEndpoints();
app.MapProgrammazioneEndpoints();
app.MapSaleEndpoints();
app.MapShowsEndpoints();
app.MapCheckoutEndpoints();
app.MapOfferteEndpoints();
app.MapAbbonamentiEndpoints();
app.MapCreditoEndpoints();
app.MapPagamentoEndpoints();
app.MapValidazioneBigliettiEndpoints();
app.MapSegnalazioniEndpoints();
app.MapDiagnosticEndpoints();
app.MapTmdbEndpoints();
app.MapSocialAuthEndpoints();
app.MapChatEndpoints();
app.MapAdminAnalyticsEndpoints();
app.MapNewsletterEndpoints();
app.MapWatchlistEndpoints();
app.MapRecommendationsEndpoints();
app.MapNotificheEndpoints();

// Endpoint pubblico: espone la publishable key di Stripe al frontend
// (necessario per inizializzare Stripe.js senza hardcodare la chiave)
app.MapGet("/config/frontend", (FrontendRuntimeConfig config) => Results.Ok(new
{
    stripePublishableKey = config.StripePublishableKey
})).AllowAnonymous();

// ─── MIGRATION E SEED ALL'AVVIO ─────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
    
    // Applica migration automaticamente all'avvio (sviluppo)
    try { context.Database.Migrate(); } catch (Exception ex) { Console.WriteLine($"[STARTUP] Migration skipped: {ex.Message}"); }
    
    // Esegue il seed dei dati di sviluppo
    try
    {
        var seeder = new DataSeeder(context);
        await seeder.SeedAsync();
        Console.WriteLine("[STARTUP] Seeding completato.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[STARTUP] Seeding skipped (DB non raggiungibile?): {ex.Message}");
    }
}

// ─── AVVIO APPLICAZIONE ────────────────────────────────────────────────────
app.Run();

/// <summary>
/// Classe parziale usata dai test di integrazione.
/// </summary>
public partial class Program;

/// <summary>
/// Configurazione runtime esposta al frontend.
/// </summary>
/// <param name="StripePublishableKey">Chiave pubblicabile Stripe esposta al frontend.</param>
public sealed record FrontendRuntimeConfig(string StripePublishableKey);
