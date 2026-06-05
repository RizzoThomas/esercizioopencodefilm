using System.Security.Claims;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

/// <summary>
/// Raggruppa gli endpoint pubblici per login social esterno.
/// </summary>
public static class SocialAuthEndpoints
{
    /// <summary>
    /// Mappa le rotte per avviare e completare il login tramite provider social esterni.
    /// Le rotte sono pubbliche con <c>AllowAnonymous</c>.
    /// Esegue scambio codici e creazione token con possibili effetti sui record utente collegati ai provider.
    /// </summary>
    /// <param name="app">Applicazione web su cui registrare gli endpoint.</param>
    /// <returns>Non restituisce valori.</returns>
    public static void MapSocialAuthEndpoints(this WebApplication app)
    {
        // ─── Google ──────────────────────────────────────────────────
        app.MapGet("/auth/login-google", (string? redirect) =>
        {
            var googleId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
            if (string.IsNullOrEmpty(googleId))
                return Results.Content("<html><body style='font-family:sans-serif;background:#1a1a2e;color:white;display:flex;align-items:center;justify-content:center;height:100vh'><div style='text-align:center'><h2>⚙️ Google OAuth non configurato</h2><p>Contatta l'amministratore per abilitare l'accesso con Google.</p><a href='https://www.cinebase.shop/login.html' style='color:#d4af37'>← Torna al login</a></div></body></html>", "text/html");

            var props = new AuthenticationProperties();
            props.Items["redirect"] = redirect ?? "/index.html";
            return Results.Challenge(props, ["Google"]);
        }).AllowAnonymous();

        // ─── Facebook ────────────────────────────────────────────────
        app.MapGet("/auth/login-facebook", (string? redirect) =>
        {
            var fbId = Environment.GetEnvironmentVariable("FACEBOOK_APP_ID");
            if (string.IsNullOrEmpty(fbId))
                return Results.Content("<html><body style='font-family:sans-serif;background:#1a1a2e;color:white;display:flex;align-items:center;justify-content:center;height:100vh'><div style='text-align:center'><h2>⚙️ Facebook OAuth non configurato</h2><p>Contatta l'amministratore per abilitare l'accesso con Facebook.</p><a href='https://www.cinebase.shop/login.html' style='color:#d4af37'>← Torna al login</a></div></body></html>", "text/html");

            var props = new AuthenticationProperties();
            props.Items["redirect"] = redirect ?? "/index.html";
            return Results.Challenge(props, ["Facebook"]);
        }).AllowAnonymous();

        // ─── Microsoft ───────────────────────────────────────────────
        app.MapGet("/auth/login-microsoft", (string? redirect) =>
        {
            var msId = Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_ID");
            if (string.IsNullOrEmpty(msId))
                return Results.Content("<html><body style='font-family:sans-serif;background:#1a1a2e;color:white;display:flex;align-items:center;justify-content:center;height:100vh'><div style='text-align:center'><h2>⚙️ Microsoft OAuth non configurato</h2><p>Contatta l'amministratore per abilitare l'accesso con Microsoft.</p><a href='https://www.cinebase.shop/login.html' style='color:#d4af37'>← Torna al login</a></div></body></html>", "text/html");

            Console.WriteLine($"[AUTH] Microsoft login requested, redirect={redirect}");
            var props = new AuthenticationProperties();
            props.Items["redirect"] = redirect ?? "/index.html";
            return Results.Challenge(props, ["Microsoft"]);
        }).AllowAnonymous();
    }

    /// <summary>
    /// Processa il ticket OAuth ricevuto da OnTicketReceived.
    /// Crea/trova l'utente, genera JWT e reindirizza al frontend.
    /// </summary>
    public static async Task ProcessOAuthTicket(
        HttpContext httpContext,
        ClaimsPrincipal principal,
        AuthenticationProperties properties,
        string provider,
        string frontendBaseUrl)
    {
        Console.WriteLine($"[AUTH] OnTicketReceived: provider={provider}");

        var email = principal.FindFirstValue(ClaimTypes.Email);
        var name = principal.FindFirstValue(ClaimTypes.Name)
                ?? principal.FindFirstValue(ClaimTypes.GivenName);
        var surname = principal.FindFirstValue(ClaimTypes.Surname) ?? "";

        Console.WriteLine($"[AUTH] Claims: email={email}, name={name}, surname={surname}");

        if (string.IsNullOrEmpty(email))
        {
            httpContext.Response.Redirect($"{frontendBaseUrl}/login.html?error=no_email");
            return;
        }

        // Microsoft: accetta solo email del dominio scolastico @issgreppi.it
        if (provider.Equals("Microsoft", StringComparison.OrdinalIgnoreCase) &&
            !email.EndsWith("@issgreppi.it", StringComparison.OrdinalIgnoreCase))
        {
            httpContext.Response.Redirect($"{frontendBaseUrl}/login.html?error=domain_not_allowed");
            return;
        }

        var db = httpContext.RequestServices.GetRequiredService<FilmDbContext>();
        var authService = httpContext.RequestServices.GetRequiredService<IAuthService>();

        var normalizedEmail = email.Trim().ToUpperInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

        // Blocca social login per PowerUser e Admin
        if (user != null && (user.Ruolo == UserRole.PowerUser || user.Ruolo == UserRole.Admin))
        {
            Console.WriteLine($"[AUTH] Social login rifiutato: utente {email} ha ruolo elevato ({user.Ruolo})");
            httpContext.Response.Redirect($"{frontendBaseUrl}/login.html?error=elevated_role");
            return;
        }

        var providerEnum = provider.Equals("Google", StringComparison.OrdinalIgnoreCase) ? ExternalLoginProvider.Google
            : provider.Equals("Microsoft", StringComparison.OrdinalIgnoreCase) ? ExternalLoginProvider.Microsoft
            : ExternalLoginProvider.Facebook;

        var providerUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub")
            ?? principal.FindFirstValue("oid")
            ?? email;

        var tenantId = principal.FindFirstValue("tid");

        if (user == null)
        {
            user = new User
            {
                Email = email,
                NormalizedEmail = normalizedEmail,
                PasswordHash = null,          // social-only: nessuna password locale
                LocalCredentialsEnabled = false,
                Nome = string.IsNullOrEmpty(name) ? email.Split('@')[0] : name,
                Cognome = surname,
                Ruolo = UserRole.User,
                DataRegistrazione = DateTime.UtcNow,
                EmailVerifiedAtUtc = DateTime.UtcNow  // email verificata dal provider
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // Registra external login
            db.UserExternalLogins.Add(new UserExternalLogin
            {
                UserId = user.Id,
                Provider = providerEnum,
                ProviderUserId = providerUserId,
                ProviderTenantId = tenantId,
                EmailAtLogin = email,
                LinkedAtUtc = DateTime.UtcNow,
                LastLoginAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
        else
        {
            // Aggiorna o crea collegamento external login
            var existingLink = await db.UserExternalLogins
                .FirstOrDefaultAsync(el => el.UserId == user.Id && el.Provider == providerEnum);

            if (existingLink != null)
            {
                existingLink.LastLoginAtUtc = DateTime.UtcNow;
            }
            else
            {
                db.UserExternalLogins.Add(new UserExternalLogin
                {
                    UserId = user.Id,
                    Provider = providerEnum,
                    ProviderUserId = providerUserId,
                    ProviderTenantId = tenantId,
                    EmailAtLogin = email,
                    LinkedAtUtc = DateTime.UtcNow,
                    LastLoginAtUtc = DateTime.UtcNow
                });
            }

            user.LastLoginAtUtc = DateTime.UtcNow;
            user.LastLoginProvider = provider;
            await db.SaveChangesAsync();
        }

        // Genera exchange code invece di passare token in URL
        var exchangeCode = Convert.ToBase64String(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            .Replace("/", "_").Replace("+", "-").TrimEnd('=');

        var exchangeCodeHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(exchangeCode)))
            .Replace("/", "_").Replace("+", "-").TrimEnd('=');

        var exchangeTtl = int.Parse(
            Environment.GetEnvironmentVariable("AUTH_EXTERNAL_EXCHANGE_TTL_MINUTES") ?? "2");

        db.ExternalAuthExchangeCodes.Add(new ExternalAuthExchangeCode
        {
            UserId = user.Id,
            CodeHash = exchangeCodeHash,
            RedirectPath = properties.Items.TryGetValue("redirect", out var r)
                ? (r ?? "/index.html") : "/index.html",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(exchangeTtl),
            Provider = providerEnum
        });
        await db.SaveChangesAsync();

        Console.WriteLine($"[AUTH] Exchange code generato per user {user.Email}, redirecting a social-login-complete");

        // Audit
        db.UserSecurityAuditLogs.Add(new UserSecurityAuditLog
        {
            UserId = user.Id,
            EventType = "ExternalLoginSucceeded",
            Provider = provider,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.FirstOrDefault(),
            MetadataJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                email,
                provider = providerEnum.ToString(),
                isNewUser = user.DataRegistrazione > DateTime.UtcNow.AddMinutes(-1)
            })
        });
        await db.SaveChangesAsync();

        var redirect = properties.Items.TryGetValue("redirect", out var rd) ? (rd ?? "/index.html") : "/index.html";
        // Validazione redirect: solo path relativi
        if (!redirect.StartsWith('/') || redirect.Contains("://"))
            redirect = "/index.html";

        var url = $"{frontendBaseUrl}/social-login-complete.html?code={Uri.EscapeDataString(exchangeCode)}" +
                  $"&redirect={Uri.EscapeDataString(redirect)}";

        httpContext.Response.Redirect(url);
    }
}
