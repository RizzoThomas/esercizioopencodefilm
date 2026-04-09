using FilmAPI.Data;
using FilmAPI.DTO.User;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public interface IUserService
{
    Task<List<UserDTO>> GetAllAsync();
    Task<UserDTO?> GetByIdAsync(int id);
    Task<UserDTO?> UpdateAsync(int id, UserUpdateDTO dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> ChangePasswordAsync(int id, ChangePasswordDTO dto);
    Task<bool> UpdateRoleAsync(int id, UserRole newRole);
}

public class UserService : IUserService
{
    private readonly FilmDbContext _context;

    public UserService(FilmDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserDTO>> GetAllAsync()
    {
        return await _context.Users
            .Where(u => u.IsActive)
            .Select(u => new UserDTO(
                u.Id,
                u.Email,
                u.Nome,
                u.Cognome,
                u.Telefono,
                u.DataNascita,
                u.Ruolo.ToString(),
                u.CreatedAt
            ))
            .ToListAsync();
    }

    public async Task<UserDTO?> GetByIdAsync(int id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

        if (user == null) return null;

        return new UserDTO(
            user.Id,
            user.Email,
            user.Nome,
            user.Cognome,
            user.Telefono,
            user.DataNascita,
            user.Ruolo.ToString(),
            user.CreatedAt
        );
    }

    public async Task<UserDTO?> UpdateAsync(int id, UserUpdateDTO dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

        if (user == null) return null;

        // Update only provided fields
        if (!string.IsNullOrEmpty(dto.Nome))
            user.Nome = dto.Nome;

        if (!string.IsNullOrEmpty(dto.Cognome))
            user.Cognome = dto.Cognome;

        if (dto.Telefono != null)
            user.Telefono = dto.Telefono;

        if (dto.DataNascita.HasValue)
            user.DataNascita = dto.DataNascita;

        await _context.SaveChangesAsync();

        return new UserDTO(
            user.Id,
            user.Email,
            user.Nome,
            user.Cognome,
            user.Telefono,
            user.DataNascita,
            user.Ruolo.ToString(),
            user.CreatedAt
        );
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

        if (user == null) return false;

        // Soft delete
        user.IsActive = false;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ChangePasswordAsync(int id, ChangePasswordDTO dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

        if (user == null) return false;

        // Verify current password
        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            return false;
        }

        // Hash and update new password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateRoleAsync(int id, UserRole newRole)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

        if (user == null) return false;

        user.Ruolo = newRole;
        await _context.SaveChangesAsync();

        return true;
    }
}
