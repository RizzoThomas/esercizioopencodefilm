using FilmAPI.DTO;

namespace FilmAPI.Services;

public class EmailSendResult
{
    /// <summary>
    /// Rappresenta la dipendenza o il dato esposto tramite la proprietà Success.
    /// </summary>
    /// <remarks>
    /// Serve al servizio per completare le sue operazioni di lettura, validazione, persistenza o integrazione esterna.
    /// </remarks>
    public bool Success { get; set; }
    /// <summary>
    /// Rappresenta la dipendenza o il dato esposto tramite la proprietà SentAtUtc.
    /// </summary>
    /// <remarks>
    /// Serve al servizio per completare le sue operazioni di lettura, validazione, persistenza o integrazione esterna.
    /// </remarks>
    public DateTime? SentAtUtc { get; set; }
    /// <summary>
    /// Rappresenta la dipendenza o il dato esposto tramite la proprietà ErrorMessage.
    /// </summary>
    /// <remarks>
    /// Serve al servizio per completare le sue operazioni di lettura, validazione, persistenza o integrazione esterna.
    /// </remarks>
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
