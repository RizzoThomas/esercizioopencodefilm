using System.Net;
using System.Net.Sockets;
using System.Text;
using FilmAPI.DTO;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Globalization;

namespace FilmAPI.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly string? _smtpHost;
    private readonly int _smtpPort;
    private readonly string? _smtpUser;
    private readonly string? _smtpPassword;
    private readonly string? _fromEmail;
    private readonly string? _fromName;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
        _smtpHost = ReadSetting("SMTP_HOST");
        _smtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587;
        _smtpUser = ReadSetting("SMTP_USER");
        _smtpPassword = ReadSetting("SMTP_PASSWORD");
        _fromEmail = ReadSetting("SMTP_FROM_EMAIL");
        _fromName = ReadSetting("SMTP_FROM_NAME") ?? "CineBase";

        if (HasCompleteConfiguration())
        {
            _logger.LogInformation(
                "SMTP configurato: Host={Host}:{Port}, User={User}, From={From}",
                _smtpHost, _smtpPort, _smtpUser, _fromEmail);
        }
        else
        {
            _logger.LogWarning(
                "SMTP non completamente configurato. Host={Host}, Port={Port}, User={User}, Password={HasPwd}, From={From}",
                _smtpHost ?? "MANCANTE",
                _smtpPort,
                _smtpUser ?? "MANCANTE",
                string.IsNullOrWhiteSpace(_smtpPassword) ? "MANCANTE" : "PRESENTE",
                _fromEmail ?? "MANCANTE");
        }
    }

    public async Task<EmailSendResult> SendOrderTicketsAsync(OrdineTicketDocumentDTO orderDocument, byte[] pdfBytes, string fileName, CancellationToken cancellationToken = default)
    {
        if (!HasCompleteConfiguration())
        {
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = "Configurazione SMTP incompleta. Verificare le variabili SMTP_* del backend."
            };
        }

        if (string.IsNullOrWhiteSpace(orderDocument.RecipientEmail))
        {
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = "Email destinatario non disponibile per l'ordine richiesto."
            };
        }

        try
        {
            var smtpHost = _smtpHost!;
            var smtpUser = _smtpUser!;
            var smtpPassword = _smtpPassword!.Replace(" ", string.Empty);
            var fromEmail = _fromEmail!;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(orderDocument.RecipientEmail));
            message.Subject = $"CineBase - Biglietti ordine {orderDocument.CodiceOrdine}";

            var bodyBuilder = new BodyBuilder
            {
                TextBody = BuildTextBody(orderDocument),
                HtmlBody = BuildHtmlBody(orderDocument)
            };

            bodyBuilder.Attachments.Add(fileName, pdfBytes, ContentType.Parse("application/pdf"));
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            client.Timeout = 15000;

            // Prova StartTls su porta 587, poi SslOnConnect su 465 come fallback
            try
            {
                await client.ConnectAsync(smtpHost, _smtpPort, SecureSocketOptions.StartTls, cancellationToken);
            }
            catch (SocketException) when (_smtpPort == 587)
            {
                _logger.LogWarning("StartTls su porta 587 fallito, provo SSL diretto su porta 465");
                using var client2 = new SmtpClient { Timeout = 15000 };
                await client2.ConnectAsync(smtpHost, 465, SecureSocketOptions.SslOnConnect, cancellationToken);
                await client2.AuthenticateAsync(smtpUser, smtpPassword, cancellationToken);
                await client2.SendAsync(message, cancellationToken);
                await client2.DisconnectAsync(true, cancellationToken);
                return new EmailSendResult { Success = true, SentAtUtc = DateTime.UtcNow };
            }

            await client.AuthenticateAsync(smtpUser, smtpPassword, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            return new EmailSendResult
            {
                Success = true,
                SentAtUtc = DateTime.UtcNow
            };
        }
        catch (SocketException ex)
        {
            _logger.LogWarning(ex, "Impossibile connettersi al server SMTP {Host}:{Port}. Verificare la connettività di rete.", _smtpHost, _smtpPort);
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = $"Impossibile raggiungere il server SMTP ({_smtpHost}:{_smtpPort}). Verifica la connessione Internet."
            };
        }
        catch (AuthenticationException ex)
        {
            _logger.LogWarning(ex, "Autenticazione SMTP fallita per utente {User}", _smtpUser);
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = "Autenticazione SMTP fallita. Verifica username e password nell'app Gmail."
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invio email ticket fallito per ordine {OrderId}", orderDocument.OrdineId);
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = $"Errore invio email: {ex.Message}"
                };
            }
    }

    private bool HasCompleteConfiguration()
    {
        return !string.IsNullOrWhiteSpace(_smtpHost)
            && !string.IsNullOrWhiteSpace(_smtpUser)
            && !string.IsNullOrWhiteSpace(_smtpPassword)
            && !string.IsNullOrWhiteSpace(_fromEmail);
    }

    private static string BuildTextBody(OrdineTicketDocumentDTO orderDocument)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Conferma acquisto ordine {orderDocument.CodiceOrdine}");
        builder.AppendLine();
        builder.AppendLine($"Film: {orderDocument.FilmTitolo}");
        builder.AppendLine($"Cinema: {orderDocument.CinemaNome}");
        builder.AppendLine($"Sala: {orderDocument.SalaNome}");
        builder.AppendLine($"Data e ora: {FormatShowDateTime(orderDocument.StartAtUtc)}");
        builder.AppendLine($"Numero biglietti: {orderDocument.NumeroBiglietti}");
        builder.AppendLine($"Totale: {FormatAmount(orderDocument.TotaleLordo)} EUR");
        builder.AppendLine();
        builder.AppendLine("Codici ticket:");

        foreach (var ticket in orderDocument.Tickets)
            builder.AppendLine($"- {ticket.CodiceBiglietto} | {ticket.Settore} fila {ticket.Fila} posto {ticket.Numero}");

        builder.AppendLine();
        builder.AppendLine("In allegato trovi il PDF multipagina dei biglietti.");
        builder.AppendLine("Il profilo utente resta il punto di recupero ufficiale dell'ordine.");
        return builder.ToString();
    }

    private static string BuildHtmlBody(OrdineTicketDocumentDTO orderDocument)
    {
        var title = WebUtility.HtmlEncode(orderDocument.FilmTitolo);
        var cinema = WebUtility.HtmlEncode(orderDocument.CinemaNome);
        var sala = WebUtility.HtmlEncode(orderDocument.SalaNome);
        var orderCode = WebUtility.HtmlEncode(orderDocument.CodiceOrdine);

        var ticketsHtml = string.Join(string.Empty, orderDocument.Tickets.Select(ticket =>
            $"<li><strong>{WebUtility.HtmlEncode(ticket.CodiceBiglietto)}</strong> - {WebUtility.HtmlEncode(ticket.Settore)} fila {ticket.Fila} posto {ticket.Numero}</li>"));

        return $"""
<html>
  <body style="font-family: Arial, sans-serif; color: #111827; line-height: 1.5;">
    <h1 style="margin-bottom: 8px;">Conferma acquisto CineBase</h1>
    <p>Ordine <strong>{orderCode}</strong> completato con successo.</p>
    <p><strong>Film:</strong> {title}<br />
       <strong>Cinema:</strong> {cinema}<br />
       <strong>Sala:</strong> {sala}<br />
       <strong>Data e ora:</strong> {FormatShowDateTime(orderDocument.StartAtUtc)}<br />
       <strong>Totale:</strong> {FormatAmount(orderDocument.TotaleLordo)} EUR</p>
    <p><strong>Biglietti emessi:</strong></p>
    <ul>{ticketsHtml}</ul>
    <p>In allegato trovi il PDF multipagina dei biglietti.</p>
    <p>Il profilo utente resta il punto di recupero ufficiale dell'ordine.</p>
  </body>
</html>
""";
    }

    private static string? ReadSetting(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return IsPlaceholder(trimmed) ? null : trimmed;
    }

    private static bool IsPlaceholder(string value)
    {
        return value.StartsWith('<') && value.EndsWith('>');
    }

    public async Task<EmailSendResult> SendTopupConfirmationAsync(string recipientEmail, string recipientName, decimal amount, decimal newBalance, string transactionId, CancellationToken cancellationToken = default)
    {
        if (!HasCompleteConfiguration())
        {
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = "Configurazione SMTP incompleta. Verificare le variabili SMTP_* del backend."
            };
        }

        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = "Email destinatario non disponibile."
            };
        }

        try
        {
            var smtpHost = _smtpHost!;
            var smtpUser = _smtpUser!;
            var smtpPassword = _smtpPassword!.Replace(" ", string.Empty);
            var fromEmail = _fromEmail!;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(recipientEmail));
            message.Subject = "CineBase - Conferma ricarica credito";

            var amountStr = FormatAmount(amount);
            var balanceStr = FormatAmount(newBalance);

            var bodyBuilder = new BodyBuilder
            {
                TextBody = BuildTopupTextBody(recipientName, amountStr, balanceStr, transactionId),
                HtmlBody = BuildTopupHtmlBody(recipientName, amountStr, balanceStr, transactionId)
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            client.Timeout = 15000;

            try
            {
                await client.ConnectAsync(smtpHost, _smtpPort, SecureSocketOptions.StartTls, cancellationToken);
            }
            catch (SocketException) when (_smtpPort == 587)
            {
                _logger.LogWarning("StartTls su porta 587 fallito per topup, provo SSL diretto su porta 465");
                using var client2 = new SmtpClient { Timeout = 15000 };
                await client2.ConnectAsync(smtpHost, 465, SecureSocketOptions.SslOnConnect, cancellationToken);
                await client2.AuthenticateAsync(smtpUser, smtpPassword, cancellationToken);
                await client2.SendAsync(message, cancellationToken);
                await client2.DisconnectAsync(true, cancellationToken);
                return new EmailSendResult { Success = true, SentAtUtc = DateTime.UtcNow };
            }

            await client.AuthenticateAsync(smtpUser, smtpPassword, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            return new EmailSendResult
            {
                Success = true,
                SentAtUtc = DateTime.UtcNow
            };
        }
        catch (SocketException ex)
        {
            _logger.LogWarning(ex, "Impossibile connettersi al server SMTP {Host}:{Port} per conferma ricarica", _smtpHost, _smtpPort);
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = $"Impossibile raggiungere il server SMTP ({_smtpHost}:{_smtpPort}). Verifica la connessione Internet."
                };
            }
            catch (AuthenticationException ex)
            {
                _logger.LogWarning(ex, "Autenticazione SMTP fallita per conferma ricarica");
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = "Autenticazione SMTP fallita. Verifica username e password nell'app Gmail."
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invio email conferma ricarica fallito per {Email}", recipientEmail);
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = $"Errore invio email: {ex.Message}"
                };
            }
    }

    private static string BuildTopupTextBody(string recipientName, string amount, string newBalance, string transactionId)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Ciao {recipientName},");
        builder.AppendLine();
        builder.AppendLine($"La tua ricarica di {amount} EUR è stata confermata!");
        builder.AppendLine($"Nuovo saldo disponibile: {newBalance} EUR");
        builder.AppendLine();
        if (!string.IsNullOrWhiteSpace(transactionId))
            builder.AppendLine($"Riferimento transazione: {transactionId}");
        builder.AppendLine();
        builder.AppendLine("Grazie per aver scelto CineBase!");
        return builder.ToString();
    }

    private static string BuildTopupHtmlBody(string recipientName, string amount, string newBalance, string transactionId)
    {
        var name = WebUtility.HtmlEncode(recipientName);
        var txRef = WebUtility.HtmlEncode(transactionId ?? string.Empty);

        return $"""
<html>
  <body style="font-family: Arial, sans-serif; color: #111827; line-height: 1.5;">
    <h1 style="margin-bottom: 8px;">Ricarica confermata</h1>
    <p>Ciao {name},</p>
    <p>La tua ricarica di <strong>{amount} EUR</strong> è stata confermata!</p>
    <p><strong>Nuovo saldo disponibile:</strong> {newBalance} EUR</p>
    {(string.IsNullOrWhiteSpace(transactionId) ? "" : $"<p style=\"color:#666; font-size:0.85em;\">Riferimento transazione: {txRef}</p>")}
    <p>Grazie per aver scelto CineBase!</p>
  </body>
</html>
""";
    }

    private static string FormatShowDateTime(DateTime startAtUtc)
    {
        var timeZone = ResolveItalyTimeZone();
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(startAtUtc, DateTimeKind.Utc), timeZone);
        return localTime.ToString("dd/MM/yyyy HH:mm");
    }

    private static string FormatAmount(decimal amount)
    {
        return amount.ToString("0.00", CultureInfo.GetCultureInfo("it-IT"));
    }

    public async Task<EmailSendResult> SendPasswordResetAsync(string recipientEmail, string recipientName, string resetLink, CancellationToken cancellationToken = default)
    {
        if (!HasCompleteConfiguration())
        {
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = "Configurazione SMTP incompleta."
            };
        }

        try
        {
            var smtpHost = _smtpHost!;
            var smtpUser = _smtpUser!;
            var smtpPassword = _smtpPassword!.Replace(" ", string.Empty);
            var fromEmail = _fromEmail!;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(recipientEmail));
            message.Subject = "CineBase - Reset Password";

            var name = WebUtility.HtmlEncode(recipientName);
            var link = WebUtility.HtmlEncode(resetLink);

            var bodyBuilder = new BodyBuilder
            {
                TextBody = $"Ciao {recipientName},\n\n" +
                           $"Abbiamo ricevuto una richiesta di reset password per il tuo account CineBase.\n\n" +
                           $"Clicca il link per reimpostare la password (valido 1 ora):\n{resetLink}\n\n" +
                           $"Se non hai richiesto tu il reset, ignora questa email.\n\n" +
                           $"CineBase Team",
                HtmlBody = $"""
<html>
  <body style="font-family: Arial, sans-serif; color: #111827; line-height: 1.5;">
    <h1 style="margin-bottom: 8px;">Reset Password CineBase</h1>
    <p>Ciao {name},</p>
    <p>Abbiamo ricevuto una richiesta di reset password per il tuo account.</p>
    <p style="margin: 24px 0;">
      <a href="{link}" style="background: #da291c; color: #fff; padding: 14px 32px; 
         text-decoration: none; font-weight: bold; text-transform: uppercase; 
         letter-spacing: 1.4px; display: inline-block;">
        Reimposta Password
      </a>
    </p>
    <p style="color: #666; font-size: 0.85em;">Il link è valido per 1 ora.</p>
    <p style="color: #666; font-size: 0.85em;">Se non hai richiesto tu il reset, ignora questa email.</p>
    <p>CineBase Team</p>
  </body>
</html>
"""
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient { Timeout = 15000 };
            await client.ConnectAsync(smtpHost, _smtpPort, SecureSocketOptions.StartTls, cancellationToken);
            await client.AuthenticateAsync(smtpUser, smtpPassword, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            return new EmailSendResult { Success = true, SentAtUtc = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invio email reset password fallito per {Email}", recipientEmail);
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = $"Errore invio email: {ex.Message}"
            };
        }
    }

    private static TimeZoneInfo ResolveItalyTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        }
    }
}
