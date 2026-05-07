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

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var envCandidates = new[]
{
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".env")),
    Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "backend", ".env")),
    Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".env"))
};

var backendEnvPath = envCandidates.FirstOrDefault(File.Exists);
if (!string.IsNullOrWhiteSpace(backendEnvPath))
{
    Env.Load(backendEnvPath);
}
else
{
    Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(new FrontendRuntimeConfig(
    Environment.GetEnvironmentVariable("STRIPE_PUBLISHABLE_API_KEY")
    ?? Environment.GetEnvironmentVariable("STRIPE_PUBLISHABLE_KEY")
    ?? string.Empty));

var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "film-api-db";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "root";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "root";
var dbUseAutoDetect = (Environment.GetEnvironmentVariable("DB_USE_AUTODETECT") ?? "true")
    .Equals("true", StringComparison.OrdinalIgnoreCase);
var dbServerVersion = Environment.GetEnvironmentVariable("DB_SERVER_VERSION") ?? "10.11.0-mariadb";

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
builder.Services.AddHostedService<RefreshTokenCleanupService>();
builder.Services.AddHostedService<ExpiredHoldCleanupService>();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

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

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "SuperSecretKeyForCineBaseJWTAuth2026!";
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "CineBaseAPI";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "CineBaseWeb";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        RoleClaimType = "role"
    };
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

// ─── OAuth Social Login ──────────────────────────────────────────────
var frontendBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "http://localhost:5001";

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

var app = builder.Build();

app.UseCors("AllowCineBaseFrontend");
app.UseMiddleware<FilmAPI.Middleware.RateLimiterMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

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

app.MapGet("/config/frontend", (FrontendRuntimeConfig config) => Results.Ok(new
{
    stripePublishableKey = config.StripePublishableKey
})).AllowAnonymous();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
    
    // Applica migration automaticamente
    try { context.Database.Migrate(); } catch (Exception) { }
    
    var seeder = new DataSeeder(context);
    await seeder.SeedAsync();
}

app.Run();

public partial class Program;

public sealed record FrontendRuntimeConfig(string StripePublishableKey);
