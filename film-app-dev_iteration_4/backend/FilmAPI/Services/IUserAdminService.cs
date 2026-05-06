using FilmAPI.DTO;
using FilmAPI.Model;

namespace FilmAPI.Services;

public interface IUserAdminService
{
    Task<List<UserAdminDTO>> GetAllUsersAsync();
    Task<AdminUserPagedResultDTO> GetUsersPagedAsync(int page, int pageSize, string? search, string? role);
    Task<UserAdminDTO?> UpdateUserRoleAsync(int userId, UpdateRuoloDTO dto, int requestingUserId);
    Task<UserAdminDTO?> UpdateUserCreditoAsync(int userId, UpdateCreditoDTO dto);
    Task<AdminUserListItemDTO?> CreateInviteAsync(CreateAdminUserInviteDTO dto, int adminUserId);
    Task<bool> SendPasswordSetupAsync(int userId, int adminUserId);
    Task<AdminUserSecurityDTO?> GetUserSecurityAsync(int userId);
}
