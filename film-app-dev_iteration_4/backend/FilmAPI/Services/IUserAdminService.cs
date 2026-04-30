using FilmAPI.DTO;
using FilmAPI.Model;

namespace FilmAPI.Services;

public interface IUserAdminService
{
    Task<List<UserAdminDTO>> GetAllUsersAsync();
    Task<UserAdminDTO?> UpdateUserRoleAsync(int userId, UpdateRuoloDTO dto, int requestingUserId);
    Task<UserAdminDTO?> UpdateUserCreditoAsync(int userId, UpdateCreditoDTO dto);
}
