using FilmAPI.DTO;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using ZXing;
using ZXing.OneD;
using ZXing.QrCode;
using ZXing.Rendering;

namespace FilmAPI.Services;

/// <summary>
/// Fornisce il servizio  per le operazioni di dominio esposte da questo modulo.
/// </summary>
/// <remarks>
/// Usato dai controller o endpoint che gestiscono le funzioni di . Dipendenze iniettate nel costruttore: nessuna dichiarata esplicitamente.
/// </remarks>
public class PdfService : IPdfService
{
    /// <summary>
    /// Esegue l''operazione di business GenerateOrderTicketsPdf del servizio.
    /// </summary>
    /// <param name="orderDocument">Parametro necessario per l'operazione: orderDocument.</param>
    /// <returns>Restituisce il risultato dell'operazione quando questa ha esito positivo; altrimenti il chiamante riceve un'eccezione o un risultato nullo/booleano secondo il contratto del metodo.</returns>
    /// <remarks>
    /// Effetti collaterali: non introduce effetti collaterali esterni evidenti oltre alla logica di lettura o validazione.
    /// </remarks>
    public byte[] GenerateOrderTicketsPdf(OrdineTicketDocumentDTO orderDocument)
    {
        if (orderDocument.Tickets.Count == 0)
            throw new InvalidOperationException("Nessun ticket disponibile per generare il PDF.");

        return Document.Create(container =>
            {
                foreach (var ticket in orderDocument.Tickets)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(24);

                        page.Header().Column(column =>
                        {
                            column.Spacing(4);
                            column.Item().Text("CineBase - Biglietto digitale").Bold().FontSize(20);
                            column.Item().Text($"Ordine {orderDocument.CodiceOrdine}").FontSize(11).FontColor(Colors.Grey.Darken2);
                        });

                        page.Content().PaddingVertical(12).Column(column =>
                        {
                            column.Spacing(12);

                            column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(info =>
                            {
                                info.Spacing(6);
                                info.Item().Text($"Film: {ticket.FilmTitolo}").SemiBold().FontSize(16);
                                info.Item().Text($"Data e ora show: {FormatShowDateTime(ticket.StartAtUtc)}");
                                info.Item().Text($"Cinema: {ticket.CinemaNome}, {ticket.CinemaCitta}");
                                info.Item().Text($"Indirizzo: {ticket.CinemaIndirizzo}");
                                info.Item().Text($"Codice locale: {ticket.CinemaCodiceLocale ?? "-"}");
                                info.Item().Text($"Sala: {ticket.SalaNome} (Sala #{ticket.SalaNumeroProgressivo})");
                                info.Item().Text($"Settore: {ticket.Settore}");
                                info.Item().Text($"Fila: {ticket.Fila} - Posto: {ticket.Numero}");
                            });

                            column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(price =>
                            {
                                price.Spacing(4);
                                price.Item().Text("Riepilogo economico").SemiBold();
                                price.Item().Text($"Prezzo base: {FormatAmount(ticket.PrezzoBase)} EUR");
                                price.Item().Text($"Supplemento: {FormatAmount(ticket.Supplemento)} EUR");
                                price.Item().Text($"Totale: {FormatAmount(ticket.PrezzoTotale)} EUR").Bold();
                            });

                            column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(code =>
                            {
                                code.Spacing(6);
                                code.Item().Text("Codici ticket").SemiBold();
                                code.Item().Text($"Codice biglietto: {ticket.CodiceBiglietto}").Bold();
                                code.Item().Text($"Barcode: {ticket.BarcodeValue}");
                            });

                            column.Item().Row(row =>
                            {
                                row.Spacing(16);

                                row.ConstantItem(130).Column(qr =>
                                {
                                    qr.Spacing(6);
                                    qr.Item().Text("QR code").SemiBold();
                                    qr.Item().Width(110).Height(110).Image(GenerateQrCode(ticket.ValidationUrl)).FitArea();
                                });

                                row.RelativeItem().Column(barcode =>
                                {
                                    barcode.Spacing(6);
                                    barcode.Item().Text("Barcode grafico").SemiBold();
                                    barcode.Item().Height(90).PaddingVertical(4).Svg(size => GenerateBarcodeSvg(ticket.BarcodeValue, size));
                                    barcode.Item().Text(ticket.BarcodeValue).FontSize(10);
                                    barcode.Item().Text($"URL validazione: {ticket.ValidationUrl}").FontSize(9).FontColor(Colors.Grey.Darken2);
                                });
                            });
                        });

                        page.Footer().AlignCenter().Text($"Presentare il ticket all'ingresso - {ticket.CodiceBiglietto}").FontSize(10);
                    });
                }
            })
            .WithSettings(new DocumentSettings
            {
                CompressDocument = false,
                ImageCompressionQuality = ImageCompressionQuality.High
            })
            .GeneratePdf();
    }

    private static byte[] GenerateQrCode(string value)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(value, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(data);
        return pngQr.GetGraphic(8);
    }

    private static string GenerateBarcodeSvg(string value, Size size)
    {
        var writer = new Code128Writer();
        var width = Math.Max(120, (int)size.Width);
        var height = Math.Max(60, (int)size.Height);
        var matrix = writer.encode(value, BarcodeFormat.CODE_128, width, height);
        var renderer = new SvgRenderer { FontName = "Arial", FontSize = 12 };
        return renderer.Render(matrix, BarcodeFormat.CODE_128, value).Content;
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
