namespace FilmAPI.Services;

public interface IAccountEmailService
{
    Task SendPasswordResetAsync(string email, string nome, string resetUrl);
    Task SendSetPasswordAsync(string email, string nome, string setupUrl);
    Task SendAdminInviteAsync(string email, string nome, string role, string inviteUrl);
    Task SendPasswordChangedAsync(string email, string nome);
}
