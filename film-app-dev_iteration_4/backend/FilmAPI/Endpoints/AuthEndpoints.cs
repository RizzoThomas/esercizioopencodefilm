using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FilmAPI.DTO;
using FilmAPI.Services;

namespace FilmAPI.Endpoints;

public static class AuthEndpoints
{
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

        group.MapPost("/login", async (LoginRequestDTO dto, IAuthService service) =>
        {
            try
            {
                var result = await service.LoginAsync(dto);
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

        // Login con 2FA (per utenti che hanno 2FA abilitato)
        group.MapPost("/login-2fa", async (TwoFactorVerifyRequestDTO dto, IAuthService service) =>
        {
            try
            {
                var result = await service.LoginWith2FaAsync(dto.Email, dto.Password, dto.Code, dto.DeviceId);
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
    }

    private static int? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
