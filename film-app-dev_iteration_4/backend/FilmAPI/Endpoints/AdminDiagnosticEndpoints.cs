using FilmAPI.Services;
using System.Net.Sockets;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace FilmAPI.Endpoints;

public static class AdminDiagnosticEndpoints
{
    public static void MapDiagnosticEndpoints(this WebApplication app)
    {
        var adminGroup = app.MapGroup("/admin").RequireAuthorization("PowerUserOrAdmin");

        adminGroup.MapGet("/email/test", async (ILogger<EmailService> logger) =>
        {
            // Leggiamo le variabili direttamente per il test
            var host = Environment.GetEnvironmentVariable("SMTP_HOST");
            var portStr = Environment.GetEnvironmentVariable("SMTP_PORT");
            var user = Environment.GetEnvironmentVariable("SMTP_USER");
            var password = Environment.GetEnvironmentVariable("SMTP_PASSWORD")?.Replace(" ", string.Empty);
            var from = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL");

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            {
                return Results.Ok(new
                {
                    success = false,
                    message = "Configurazione SMTP incompleta nel file .env",
                    config = new { host, port = portStr ?? "587", user, password_loaded = !string.IsNullOrWhiteSpace(password), from }
                });
            }

            try
            {
                using var client = new SmtpClient();
                client.Timeout = 10000; // 10 secondi

                // Test solo connessione (senza inviare)
                await client.ConnectAsync(host, int.TryParse(portStr, out var p) ? p : 587, SecureSocketOptions.StartTls);

                if (client.IsConnected)
                {
                    await client.AuthenticateAsync(user, password);

                    if (client.IsAuthenticated)
                    {
                        await client.DisconnectAsync(true);
                        return Results.Ok(new
                        {
                            success = true,
                            message = $"Connessione e autenticazione SMTP riuscite! Server: {host}:{portStr ?? "587"}",
                            config = new { host, port = portStr ?? "587", user, from }
                        });
                    }
                }

                return Results.Ok(new
                {
                    success = false,
                    message = "Connesso al server ma non autenticato.",
                    config = new { host, port = portStr ?? "587", user }
                });
            }
            catch (SocketException ex)
            {
                logger.LogWarning(ex, "Test SMTP fallito - timeout connessione");
                return Results.Ok(new
                {
                    success = false,
                    message = $"Impossibile raggiungere il server SMTP ({host}:{portStr ?? "587"}). La porta 587 potrebbe essere bloccata dal firewall o dalla rete.",
                    error = ex.Message,
                    config = new { host, port = portStr ?? "587" }
                });
            }
            catch (AuthenticationException ex)
            {
                logger.LogWarning(ex, "Test SMTP fallito - autenticazione");
                return Results.Ok(new
                {
                    success = false,
                    message = "Autenticazione SMTP fallita. Verifica che la password sia una 'App Password' di Gmail (non la password normale dell'account).",
                    error = ex.Message,
                    config = new { host, port = portStr ?? "587", user }
                });
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Test SMTP fallito");
                return Results.Ok(new
                {
                    success = false,
                    message = $"Errore test SMTP: {ex.Message}",
                    error = ex.ToString(),
                    config = new { host, port = portStr ?? "587", user }
                });
            }
        });
    }
}
