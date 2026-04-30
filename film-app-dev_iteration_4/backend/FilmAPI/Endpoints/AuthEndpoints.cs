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
    }
}
