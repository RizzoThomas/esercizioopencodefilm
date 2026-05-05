using System.Security.Claims;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class SocialAuthEndpoints
{
    public static void MapSocialAuthEndpoints(this WebApplication app)
    {
        // ─── Google ──────────────────────────────────────────────────
        app.MapGet("/auth/login-google", (string? redirect) =>
        {
            var props = new AuthenticationProperties();
            props.Items["redirect"] = redirect ?? "/index.html";
            return Results.Challenge(props, ["Google"]);
        }).AllowAnonymous();

        // ─── Facebook ────────────────────────────────────────────────
        app.MapGet("/auth/login-facebook", (string? redirect) =>
        {
            var props = new AuthenticationProperties();
            props.Items["redirect"] = redirect ?? "/index.html";
            return Results.Challenge(props, ["Facebook"]);
        }).AllowAnonymous();

        // ─── Microsoft ───────────────────────────────────────────────
        app.MapGet("/auth/login-microsoft", (string? redirect) =>
        {
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

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                Nome = string.IsNullOrEmpty(name) ? email.Split('@')[0] : name,
                Cognome = surname,
                Ruolo = UserRole.User,
                DataRegistrazione = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var authResponse = await authService.SocialLoginAsync(user);
        Console.WriteLine($"[AUTH] JWT generated for user {user.Email}, redirecting to frontend");

        var redirect = properties.Items.TryGetValue("redirect", out var r) ? (r ?? "/index.html") : "/index.html";
        var url = $"{frontendBaseUrl}/login.html?token={Uri.EscapeDataString(authResponse.AccessToken)}" +
                  $"&refresh={Uri.EscapeDataString(authResponse.RefreshToken)}" +
                  $"&redirect={Uri.EscapeDataString(redirect)}";

        httpContext.Response.Redirect(url);
    }
}
