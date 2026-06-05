using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

/// <summary>
/// Raggruppa gli endpoint di autenticazione, 2FA, login social e gestione account.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Mappa le rotte del gruppo <c>/auth</c> per registrazione, login, refresh, logout, profilo utente, reset password, scambio codice social, configurazione 2FA, cambio password, richiesta password e sicurezza account.
    /// Le rotte pubbliche usano <c>AllowAnonymous</c>; le altre richiedono <c>RequireAuthorization("Authenticated")</c>.
    /// Esegue operazioni su credenziali, token, invio email e stato di sicurezza dell'account con effetti sul database e sui token di accesso.
    /// </summary>
    /// <param name="app">Applicazione web su cui registrare gli endpoint.</param>
    /// <returns>Non restituisce valori.</returns>
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/register", async (RegisterRequestDTO dto, IAuthService service) =>
        {
            try
            {
                var result = await service.RegisterAsync(dto);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);
            }
        }).AllowAnonymous();

        group.MapPost("/login", async (LoginRequestDTO dto, IAuthService service, HttpContext context) =>
        {
            try
            {
                var result = await service.LoginAsync(dto, context);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        }).AllowAnonymous();

        group.MapPost("/refresh", async (RefreshTokenRequestDTO dto, IAuthService service) =>
        {
            try
            {
                var result = await service.RefreshAsync(dto.RefreshToken, dto.DeviceId);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        }).AllowAnonymous();

        group.MapPost("/logout", async (RefreshTokenRequestDTO dto, IAuthService service) =>
        {
            var result = await service.LogoutAsync(dto.RefreshToken, dto.DeviceId);
            return result ? Results.Ok() : Results.NotFound();
        }).RequireAuthorization("Authenticated");

        group.MapGet("/me", async (HttpContext context, IAuthService service) =>
        {
            var userIdClaim = context.User.FindFirst("sub")?.Value
                ?? context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var userInfo = await service.GetUserByIdAsync(userId);
            return userInfo is null ? Results.Unauthorized() : Results.Ok(userInfo);
        }).RequireAuthorization("Authenticated");

        // ─── Password Reset ──────────────────────────────────────────

        group.MapPost("/forgot-password", async (ForgotPasswordRequestDTO dto, IAuthService service) =>
        {
            await service.ForgotPasswordAsync(dto.Email);
            // Restituisci sempre OK per non rivelare se l'email esiste
            return Results.Ok(new { message = "Se l'email esiste, riceverai un link di reset." });
        }).AllowAnonymous();

        group.MapPost("/reset-password", async (ResetPasswordRequestDTO dto, IAuthService service) =>
        {
            var result = await service.ResetPasswordAsync(dto.Token, dto.NewPassword);
            return result
                ? Results.Ok(new { message = "Password reimpostata con successo." })
                : Results.BadRequest(new { error = "Token non valido o scaduto." });
        }).AllowAnonymous();

        // ─── Social Login Exchange ──────────────────────────────────────

        group.MapPost("/external/exchange", async (ExternalExchangeRequestDTO dto, FilmDbContext db, IAuthService authService) =>
        {
            if (string.IsNullOrEmpty(dto.Code))
                return Results.BadRequest(new { error = "Code mancante." });

            // Hash del code per lookup
            var codeHash = Convert.ToBase64String(
                SHA256.HashData(Encoding.UTF8.GetBytes(dto.Code)))
                .Replace("/", "_").Replace("+", "-").TrimEnd('=');

            var exchangeCode = await db.ExternalAuthExchangeCodes
                .Include(ec => ec.User)
                .FirstOrDefaultAsync(ec => ec.CodeHash == codeHash);

            if (exchangeCode == null || exchangeCode.ConsumedAtUtc != null)
                return Results.BadRequest(new { error = "Code non valido o già usato." });

            if (exchangeCode.ExpiresAtUtc < DateTime.UtcNow)
                return Results.BadRequest(new { error = "Code scaduto." });

            // Consuma il code
            exchangeCode.ConsumedAtUtc = DateTime.UtcNow;

            // Genera token applicativi
            var authResponse = await authService.SocialLoginAsync(exchangeCode.User!);
            await db.SaveChangesAsync();

            return Results.Ok(authResponse);
        }).AllowAnonymous();

        // ─── 2FA ──────────────────────────────────────────────────────

        group.MapPost("/2fa/setup", async (HttpContext context, IAuthService service) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();

            try
            {
                var setup = await service.GenerateTwoFactorSetupAsync(userId.Value);
                return Results.Ok(setup);
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        }).RequireAuthorization("Authenticated");

        group.MapPost("/2fa/enable", async (HttpContext context, TwoFactorEnableRequestDTO dto, IAuthService service) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();

            var result = await service.EnableTwoFactorAsync(userId.Value, dto.Code);
            return result
                ? Results.Ok(new { message = "2FA abilitato con successo." })
                : Results.BadRequest(new { error = "Codice non valido. Riprova." });
        }).RequireAuthorization("Authenticated");

        group.MapPost("/2fa/disable", async (HttpContext context, IAuthService service) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();

            await service.DisableTwoFactorAsync(userId.Value);
            return Results.Ok(new { message = "2FA disabilitato." });
        }).RequireAuthorization("Authenticated");

        // Login con 2FA (dopo aver ricevuto tempToken dal login)
        group.MapPost("/login-2fa", async (TwoFactorLoginRequestDTO dto, IAuthService service, HttpContext context) =>
        {
            try
            {
                var result = await service.LoginWith2FaAsync(dto.TempToken, dto.Code, dto.TrustDevice, dto.DeviceId, context);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).AllowAnonymous();

        // ─── Change Password ──────────────────────────────────────────

        group.MapPost("/change-password", async (HttpContext context, ChangePasswordRequestDTO dto, IAuthService service) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();

            var result = await service.ChangePasswordAsync(userId.Value, dto.CurrentPassword, dto.NewPassword);
            return result
                ? Results.Ok(new { message = "Password modificata con successo." })
                : Results.BadRequest(new { error = "Password attuale non valida o account senza password locale." });
        }).RequireAuthorization("Authenticated");

        // ─── Set Password Request (social-only → locale) ──────────────

        group.MapPost("/set-password/request", async (HttpContext context, IAuthService service) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();

            var result = await service.RequestSetPasswordAsync(userId.Value);
            return result
                ? Results.Ok(new { message = "Email per impostare la password inviata." })
                : Results.NotFound(new { error = "Utente non trovato." });
        }).RequireAuthorization("Authenticated");

        // ─── Account Security ─────────────────────────────────────────

        group.MapGet("/security/me", async (HttpContext context, IAuthService service) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();

            var security = await service.GetAccountSecurityAsync(userId.Value);
            return security is null
                ? Results.NotFound(new { error = "Utente non trovato." })
                : Results.Ok(security);
        }).RequireAuthorization("Authenticated");
    }

    private static int? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
