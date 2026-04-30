using FilmAPI.DTO;

namespace FilmAPI.Services;

public interface IPdfService
{
    byte[] GenerateOrderTicketsPdf(OrdineTicketDocumentDTO orderDocument);
}
