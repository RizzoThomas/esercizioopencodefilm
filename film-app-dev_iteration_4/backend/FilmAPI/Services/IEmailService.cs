using FilmAPI.DTO;

namespace FilmAPI.Services;

public class EmailSendResult
{
    public bool Success { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IEmailService
{
    Task<EmailSendResult> SendOrderTicketsAsync(OrdineTicketDocumentDTO orderDocument, byte[] pdfBytes, string fileName, CancellationToken cancellationToken = default);
    Task<EmailSendResult> SendTopupConfirmationAsync(string recipientEmail, string recipientName, decimal amount, decimal newBalance, string transactionId, CancellationToken cancellationToken = default);
    Task<EmailSendResult> SendVoucherPurchaseAsync(string recipientEmail, string recipientName, string voucherCode, decimal importo, DateTime? scadenzaUtc, CancellationToken cancellationToken = default);
    Task<EmailSendResult> SendPasswordResetAsync(string recipientEmail, string recipientName, string resetLink, CancellationToken cancellationToken = default);
    Task<EmailSendResult> SendNewsletterWelcomeAsync(string recipientEmail, string? recipientName, CancellationToken cancellationToken = default);
    Task<EmailSendResult> SendNewOffersNotificationAsync(string recipientEmail, string? recipientName, string offersHtml, CancellationToken cancellationToken = default);
}
