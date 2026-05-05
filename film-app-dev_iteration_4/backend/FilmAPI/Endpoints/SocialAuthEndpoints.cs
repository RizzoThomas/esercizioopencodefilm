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
            var props = new AuthenticationProperties
            {
                RedirectUri = $"/auth/signin-google?redirect={Uri.EscapeDataString(redirect ?? "/index.html")}"
            };
            return Results.Challenge(props, ["Google"]);
        }).AllowAnonymous();

        // ─── Facebook ────────────────────────────────────────────────
        app.MapGet("/auth/login-facebook", (string? redirect) =>
        {
            var props = new AuthenticationProperties
            {
                RedirectUri = $"/auth/signin-facebook?redirect={Uri.EscapeDataString(redirect ?? "/index.html")}"
            };
            return Results.Challenge(props, ["Facebook"]);
        }).AllowAnonymous();

        // ─── Microsoft ───────────────────────────────────────────────
        app.MapGet("/auth/login-microsoft", (string? redirect) =>
        {
            var props = new AuthenticationProperties
            {
                RedirectUri = $"/auth/signin-microsoft?redirect={Uri.EscapeDataString(redirect ?? "/index.html")}"
            };
            return Results.Challenge(props, ["Microsoft"]);
        }).AllowAnonymous();

        // ─── Callback unificato ──────────────────────────────────────
        app.MapGet("/auth/signin-{provider}", async (string provider, string? redirect, HttpContext context) =>
        {
            return await HandleSocialCallback(context, provider, redirect);
        }).AllowAnonymous();
    }

    private static async Task<IResult> HandleSocialCallback(HttpContext context, string provider, string? redirect)
    {
        // Normalizza: URL route dà "google", ma lo schema registrato è "Google"
        var scheme = provider.Length > 0
            ? char.ToUpper(provider[0]) + provider[1..]
            : provider;
        var result = await context.AuthenticateAsync(scheme);
        if (!result.Succeeded || result.Principal == null)
        {
            var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:5001";
            return Results.Redirect($"{frontendUrl}/login.html?error=social_failed");
        }

        var email = result.Principal.FindFirstValue(ClaimTypes.Email);
        var name = result.Principal.FindFirstValue(ClaimTypes.Name)
                ?? result.Principal.FindFirstValue(ClaimTypes.GivenName);
        var surname = result.Principal.FindFirstValue(ClaimTypes.Surname) ?? "";

        if (string.IsNullOrEmpty(email))
        {
            var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:5001";
            return Results.Redirect($"{frontendUrl}/login.html?error=no_email");
        }

        var db = context.RequestServices.GetRequiredService<FilmDbContext>();
        var authService = context.RequestServices.GetRequiredService<IAuthService>();

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

        var frontendRedirect = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:5001";
        var target = redirect ?? "/index.html";
        var url = $"{frontendRedirect}/login.html?token={Uri.EscapeDataString(authResponse.AccessToken)}" +
                  $"&refresh={Uri.EscapeDataString(authResponse.RefreshToken)}" +
                  $"&redirect={Uri.EscapeDataString(target)}";

        return Results.Redirect(url);
    }
}
