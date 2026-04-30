using FilmAPI.DTO;

namespace FilmAPI.Services;

public interface ICheckoutService
{
    Task<OrdineSummaryDTO> CreateOrdineAsync(int userId, CreateOrdineRequestDTO dto);
    Task<List<OrdineSummaryDTO>> GetOrdiniByUserAsync(int userId);
    Task<OrdineSummaryDTO?> GetOrdineByIdAsync(int orderId, int userId);
    Task<List<BigliettoSummaryDTO>> GetTicketsByUserAsync(int userId);
    Task<BigliettoDetailDTO?> GetTicketByIdAsync(int ticketId, int userId);
}
