using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace FilmAPI.Services;

/// <summary>
/// Fornisce il servizio  per le operazioni di dominio esposte da questo modulo.
/// </summary>
/// <remarks>
/// Usato dai controller o endpoint che gestiscono le funzioni di . Dipendenze iniettate nel costruttore: nessuna dichiarata esplicitamente.
/// </remarks>
public class AccountEmailService : IAccountEmailService
{
    private readonly ILogger<AccountEmailService> _logger;
    private readonly string _frontendBaseUrl;
    private readonly string? _smtpHost;
    private readonly int _smtpPort;
    private readonly string? _smtpUser;
    private readonly string? _smtpPassword;
    private readonly string? _fromEmail;
    private readonly string? _fromName;

    /// <summary>
    /// Esegue l''operazione AccountEmailService del servizio.
    /// </summary>
    /// <param name="logger">Parametro necessario per l'operazione: logger.</param>
    /// <returns>Restituisce il risultato dell'operazione quando questa ha esito positivo; altrimenti il chiamante riceve un'eccezione o un risultato nullo/booleano secondo il contratto del metodo.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public AccountEmailService(ILogger<AccountEmailService> logger)
    {
        _logger = logger;
        _frontendBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "http://localhost:5001";
        _smtpHost = ReadSetting("SMTP_HOST");
        _smtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587;
        _smtpUser = ReadSetting("SMTP_USER");
        _smtpPassword = ReadSetting("SMTP_PASSWORD");
        _fromEmail = ReadSetting("SMTP_FROM_EMAIL");
        _fromName = ReadSetting("SMTP_FROM_NAME") ?? "CineBase";

        if (HasCompleteConfiguration())
        {
            _logger.LogInformation(
                "AccountEmailService SMTP configurato: Host={Host}:{Port}, User={User}, From={From}",
                _smtpHost, _smtpPort, _smtpUser, _fromEmail);
        }
        else
        {
            _logger.LogWarning(
                "AccountEmailService SMTP non completamente configurato. Host={Host}, Port={Port}, User={User}, From={From}",
                _smtpHost ?? "MANCANTE", _smtpPort, _smtpUser ?? "MANCANTE", _fromEmail ?? "MANCANTE");
        }
    }

    /// <summary>
    /// Esegue l''operazione di business SendPasswordResetAsync del servizio.
    /// </summary>
    /// <param name="email">Indirizzo email usato per autenticazione, notifica o identificazione dell'utente.</param>
    /// <param name="nome">Parametro necessario per l'operazione: nome.</param>
    /// <param name="resetUrl">Parametro necessario per l'operazione: resetUrl.</param>
    /// <returns>Completa l'operazione in modo asincrono senza restituire un valore, lasciando al chiamante la sola gestione dell'esito tramite eccezioni.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task SendPasswordResetAsync(string email, string nome, string resetUrl)
    {
        if (!HasCompleteConfiguration())
        {
            _logger.LogWarning("SMTP non configurato. Saltato invio reset password per {Email}", email);
            return;
        }

        try
        {
            var message = BuildMessage(email, nome, "CineBase - Reset Password",
                $"Ciao {nome},\n\nAbbiamo ricevuto una richiesta di reset password per il tuo account CineBase.\n\nClicca il link per reimpostare la password:\n{resetUrl}\n\nSe non hai richiesto tu il reset, ignora questa email.\n\nCineBase Team",
                BuildPasswordResetHtml(nome, resetUrl));

            await SendAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invio email reset password fallito per {Email}", email);
        }
    }

    /// <summary>
    /// Esegue l''operazione di business SendSetPasswordAsync del servizio.
    /// </summary>
    /// <param name="email">Indirizzo email usato per autenticazione, notifica o identificazione dell'utente.</param>
    /// <param name="nome">Parametro necessario per l'operazione: nome.</param>
    /// <param name="setupUrl">Parametro necessario per l'operazione: setupUrl.</param>
    /// <returns>Completa l'operazione in modo asincrono senza restituire un valore, lasciando al chiamante la sola gestione dell'esito tramite eccezioni.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task SendSetPasswordAsync(string email, string nome, string setupUrl)
    {
        if (!HasCompleteConfiguration())
        {
            _logger.LogWarning("SMTP non configurato. Saltato invio set password per {Email}", email);
            return;
        }

        try
        {
            var message = BuildMessage(email, nome, "CineBase - Imposta Password",
                $"Ciao {nome},\n\nHai richiesto di impostare una password per il tuo account CineBase.\n\nClicca il link per impostare la password:\n{setupUrl}\n\nSe non sei stato tu, ignora questa email.\n\nCineBase Team",
                BuildSetPasswordHtml(nome, setupUrl));

            await SendAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invio email set password fallito per {Email}", email);
        }
    }

    /// <summary>
    /// Esegue l''operazione di business SendAdminInviteAsync del servizio.
    /// </summary>
    /// <param name="email">Indirizzo email usato per autenticazione, notifica o identificazione dell'utente.</param>
    /// <param name="nome">Parametro necessario per l'operazione: nome.</param>
    /// <param name="role">Parametro necessario per l'operazione: role.</param>
    /// <param name="inviteUrl">Parametro necessario per l'operazione: inviteUrl.</param>
    /// <returns>Completa l'operazione in modo asincrono senza restituire un valore, lasciando al chiamante la sola gestione dell'esito tramite eccezioni.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task SendAdminInviteAsync(string email, string nome, string role, string inviteUrl)
    {
        if (!HasCompleteConfiguration())
        {
            _logger.LogWarning("SMTP non configurato. Saltato invio invito admin per {Email}", email);
            return;
        }

        try
        {
            var message = BuildMessage(email, nome, "CineBase - Invito Amministratore",
                $"Ciao {nome},\n\nSei stato invitato come {role} su CineBase.\n\nClicca il link per completare la registrazione:\n{inviteUrl}\n\nCineBase Team",
                BuildAdminInviteHtml(nome, role, inviteUrl));

            await SendAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invio email invito admin fallito per {Email}", email);
        }
    }

    /// <summary>
    /// Esegue l''operazione di business SendPasswordChangedAsync del servizio.
    /// </summary>
    /// <param name="email">Indirizzo email usato per autenticazione, notifica o identificazione dell'utente.</param>
    /// <param name="nome">Parametro necessario per l'operazione: nome.</param>
    /// <returns>Completa l'operazione in modo asincrono senza restituire un valore, lasciando al chiamante la sola gestione dell'esito tramite eccezioni.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task SendPasswordChangedAsync(string email, string nome)
    {
        if (!HasCompleteConfiguration())
        {
            _logger.LogWarning("SMTP non configurato. Saltato notifica cambio password per {Email}", email);
            return;
        }

        try
        {
            var message = BuildMessage(email, nome, "CineBase - Password Modificata",
                $"Ciao {nome},\n\nLa password del tuo account CineBase e stata modificata con successo.\n\nSe non sei stato tu, contatta immediatamente il supporto.\n\nCineBase Team",
                BuildPasswordChangedHtml(nome));

            await SendAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invio notifica cambio password fallito per {Email}", email);
        }
    }

    private MimeMessage BuildMessage(string email, string nome, string subject, string textBody, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_fromName, _fromEmail!));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            TextBody = textBody,
            HtmlBody = htmlBody
        };

        message.Body = bodyBuilder.ToMessageBody();
        return message;
    }

    private async Task SendAsync(MimeMessage message)
    {
        using var client = new SmtpClient { Timeout = 15000 };
        await client.ConnectAsync(_smtpHost!, _smtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_smtpUser!, _smtpPassword!.Replace(" ", string.Empty));
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private static string BuildPasswordResetHtml(string nome, string resetUrl)
    {
        var name = WebUtility.HtmlEncode(nome);
        var link = WebUtility.HtmlEncode(resetUrl);
        return $"""
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
    <p style="color: #666; font-size: 0.85em;">Se non hai richiesto tu il reset, ignora questa email.</p>
    <p>CineBase Team</p>
  </body>
</html>
""";
    }

    private static string BuildSetPasswordHtml(string nome, string setupUrl)
    {
        var name = WebUtility.HtmlEncode(nome);
        var link = WebUtility.HtmlEncode(setupUrl);
        return $"""
<html>
  <body style="font-family: Arial, sans-serif; color: #111827; line-height: 1.5;">
    <h1 style="margin-bottom: 8px;">Imposta Password CineBase</h1>
    <p>Ciao {name},</p>
    <p>Hai richiesto di impostare una password per il tuo account CineBase.</p>
    <p style="margin: 24px 0;">
      <a href="{link}" style="background: #da291c; color: #fff; padding: 14px 32px;
         text-decoration: none; font-weight: bold; text-transform: uppercase;
         letter-spacing: 1.4px; display: inline-block;">
        Imposta Password
      </a>
    </p>
    <p style="color: #666; font-size: 0.85em;">Se non sei stato tu, ignora questa email.</p>
    <p>CineBase Team</p>
  </body>
</html>
""";
    }

    private static string BuildAdminInviteHtml(string nome, string role, string inviteUrl)
    {
        var name = WebUtility.HtmlEncode(nome);
        var r = WebUtility.HtmlEncode(role);
        var link = WebUtility.HtmlEncode(inviteUrl);
        return $"""
<html>
  <body style="font-family: Arial, sans-serif; color: #111827; line-height: 1.5;">
    <h1 style="margin-bottom: 8px;">Invito CineBase</h1>
    <p>Ciao {name},</p>
    <p>Sei stato invitato come <strong>{r}</strong> su CineBase.</p>
    <p style="margin: 24px 0;">
      <a href="{link}" style="background: #da291c; color: #fff; padding: 14px 32px;
         text-decoration: none; font-weight: bold; text-transform: uppercase;
         letter-spacing: 1.4px; display: inline-block;">
        Completa Registrazione
      </a>
    </p>
    <p>CineBase Team</p>
  </body>
</html>
""";
    }

    private static string BuildPasswordChangedHtml(string nome)
    {
        var name = WebUtility.HtmlEncode(nome);
        return $"""
<html>
  <body style="font-family: Arial, sans-serif; color: #111827; line-height: 1.5;">
    <h1 style="margin-bottom: 8px;">Password Modificata</h1>
    <p>Ciao {name},</p>
    <p>La password del tuo account CineBase e stata modificata con successo.</p>
    <p style="color: #666; font-size: 0.85em;">Se non sei stato tu, contatta immediatamente il supporto.</p>
    <p>CineBase Team</p>
  </body>
</html>
""";
    }

    private bool HasCompleteConfiguration()
    {
        return !string.IsNullOrWhiteSpace(_smtpHost)
            && !string.IsNullOrWhiteSpace(_smtpUser)
            && !string.IsNullOrWhiteSpace(_smtpPassword)
            && !string.IsNullOrWhiteSpace(_fromEmail);
    }

    private static string? ReadSetting(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return (trimmed.StartsWith('<') && trimmed.EndsWith('>')) ? null : trimmed;
    }
}
