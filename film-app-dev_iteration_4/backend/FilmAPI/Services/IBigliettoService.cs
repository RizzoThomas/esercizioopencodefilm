using FilmAPI.DTO;

namespace FilmAPI.Services;

public interface IBigliettoService
{
    Task EmitTicketsForOrderAsync(int orderId);
    Task<OrdineTicketDocumentDTO> GetOrderTicketDocumentAsync(int orderId);
    Task<TicketValidationLookupDTO?> GetTicketValidationLookupAsync(string code);
}
