using FilmAPI.DTO;

namespace FilmAPI.Services;

public interface IValidazioneBigliettoService
{
    Task<TicketValidationLookupDTO?> GetTicketByCodeAsync(string code);
    Task<TicketValidationResultDTO> ValidateAsync(int operatorUserId, TicketValidationRequestDTO request);
}
