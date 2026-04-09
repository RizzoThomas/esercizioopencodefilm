using FilmAPI.DTO.Auth;
using FilmAPI.DTO.User;
using FilmAPI.Model;
using FilmAPI.Services;
using FilmAPI.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace FilmAPI.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth")
            .WithTags("Authentication");

        // POST /auth/login - Pubblico
        group.MapPost("/login", async (LoginRequestDTO dto, IAuthService authService) =>
        {
            var (success, data, error) = await authService.LoginAsync(dto);
            return success
                ? Results.Ok(data)
                : Results.Unauthorized();
        })
        .AllowAnonymous()
        .WithName("Login")
        .Produces<LoginResponseDTO>(200)
        .Produces(401);

        // POST /auth/register - Pubblico
        group.MapPost("/register", async (RegisterRequestDTO dto, IAuthService authService) =>
        {
            var (success, data, error) = await authService.RegisterAsync(dto);
            return success
                ? Results.Created($"/users/{data!.Id}", data)
                : Results.BadRequest(new { Message = error });
        })
        .AllowAnonymous()
        .WithName("Register")
        .Produces<UserDTO>(201)
        .Produces(400);

        // POST /auth/refresh - Pubblico (richiede refresh token valido)
        group.MapPost("/refresh", async (RefreshTokenRequestDTO dto, IAuthService authService) =>
        {
            var (success, data, error) = await authService.RefreshTokenAsync(dto);
            return success
                ? Results.Ok(data)
                : Results.Unauthorized();
        })
        .AllowAnonymous()
        .WithName("RefreshToken")
        .Produces<LoginResponseDTO>(200)
        .Produces(401);

        // POST /auth/logout - Richiede autenticazione
        group.MapPost("/logout", async (HttpContext httpContext, IAuthService authService) =>
        {
            var userId = httpContext.User.GetUserId();
            await authService.LogoutAsync(userId);
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("Logout")
        .Produces(204);

        // GET /auth/me - Richiede autenticazione
        group.MapGet("/me", async (HttpContext httpContext, IUserService userService) =>
        {
            var userId = httpContext.User.GetUserId();
            var user = await userService.GetByIdAsync(userId);
            return user is not null ? Results.Ok(user) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .Produces<UserDTO>(200)
        .Produces(404);
    }
}
