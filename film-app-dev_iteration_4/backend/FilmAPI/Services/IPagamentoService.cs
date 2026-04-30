using FilmAPI.DTO;

namespace FilmAPI.Services;

public interface IPagamentoService
{
    Task<PayOrdineResponseDTO> PayOrdineAsync(int userId, int orderId, PayOrdineRequestDTO dto, string? idempotencyKey);
    Task<OrdineSummaryDTO> CancelPendingOrdineAsync(int userId, int orderId);
    Task HandleStripeWebhookAsync(string payload, string? signatureHeader);
    Task<CreateCheckoutSessionResponseDTO> CreateCheckoutSessionAsync(int userId, int orderId, CreateCheckoutSessionRequestDTO dto, string? idempotencyKey);
    Task<CheckoutStatusDTO> GetCheckoutStatusAsync(int userId, int orderId);
    Task<CheckoutStatusDTO> ReconcileCheckoutSessionAsync(int userId, int orderId);
}
