using FilmAPI.Data;
using FilmAPI.DTO.Auth;
using FilmAPI.DTO.User;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public interface IAuthService
{
    Task<(bool Success, LoginResponseDTO? Data, string? Error)> LoginAsync(LoginRequestDTO dto);
    Task<(bool Success, UserDTO? Data, string? Error)> RegisterAsync(RegisterRequestDTO dto);
    Task<(bool Success, LoginResponseDTO? Data, string? Error)> RefreshTokenAsync(RefreshTokenRequestDTO dto);
    Task LogoutAsync(int userId);
}

public class AuthService : IAuthService
{
    private readonly FilmDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly int _refreshTokenExpirationDays;

    public AuthService(FilmDbContext context, IJwtService jwtService, IConfiguration configuration)
    {
        _context = context;
        _jwtService = jwtService;
        _refreshTokenExpirationDays = int.TryParse(configuration["Jwt:RefreshTokenExpirationDays"], out var days) ? days : 7;
    }

    public async Task<(bool Success, LoginResponseDTO? Data, string? Error)> LoginAsync(LoginRequestDTO dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email && u.IsActive);

        if (user == null)
        {
            return (false, null, "Email o password non validi");
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return (false, null, "Email o password non validi");
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshTokenString = _jwtService.GenerateRefreshToken();

        // Save refresh token
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        var userDto = new UserDTO(
            user.Id,
            user.Email,
            user.Nome,
            user.Cognome,
            user.Telefono,
            user.DataNascita,
            user.Ruolo.ToString(),
            user.CreatedAt
        );

        var response = new LoginResponseDTO(
            accessToken,
            refreshTokenString,
            expiresAt,
            userDto
        );

        return (true, response, null);
    }

    public async Task<(bool Success, UserDTO? Data, string? Error)> RegisterAsync(RegisterRequestDTO dto)
    {
        // Check if email already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (existingUser != null)
        {
            return (false, null, "Un utente con questa email esiste già");
        }

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = passwordHash,
            Nome = dto.Nome,
            Cognome = dto.Cognome,
            Telefono = dto.Telefono,
            DataNascita = dto.DataNascita,
            Ruolo = UserRole.User, // Default role
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var userDto = new UserDTO(
            user.Id,
            user.Email,
            user.Nome,
            user.Cognome,
            user.Telefono,
            user.DataNascita,
            user.Ruolo.ToString(),
            user.CreatedAt
        );

        return (true, userDto, null);
    }

    public async Task<(bool Success, LoginResponseDTO? Data, string? Error)> RefreshTokenAsync(RefreshTokenRequestDTO dto)
    {
        var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken && !rt.IsRevoked);

        if (refreshToken == null || refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            return (false, null, "Refresh token non valido o scaduto");
        }

        if (!refreshToken.User.IsActive)
        {
            return (false, null, "Utente disattivato");
        }

        // Revoke old token
        refreshToken.IsRevoked = true;

        // Generate new tokens
        var newAccessToken = _jwtService.GenerateAccessToken(refreshToken.User);
        var newRefreshTokenString = _jwtService.GenerateRefreshToken();

        // Save new refresh token
        var newRefreshToken = new RefreshToken
        {
            UserId = refreshToken.UserId,
            Token = newRefreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            ReplacedByToken = newRefreshTokenString
        };

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        var userDto = new UserDTO(
            refreshToken.User.Id,
            refreshToken.User.Email,
            refreshToken.User.Nome,
            refreshToken.User.Cognome,
            refreshToken.User.Telefono,
            refreshToken.User.DataNascita,
            refreshToken.User.Ruolo.ToString(),
            refreshToken.User.CreatedAt
        );

        var response = new LoginResponseDTO(
            newAccessToken,
            newRefreshTokenString,
            expiresAt,
            userDto
        );

        return (true, response, null);
    }

    public async Task LogoutAsync(int userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
        }

        await _context.SaveChangesAsync();
    }
}
