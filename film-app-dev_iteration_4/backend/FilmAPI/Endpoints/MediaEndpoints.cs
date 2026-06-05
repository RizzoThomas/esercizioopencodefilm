using FilmAPI.DTO;
using FilmAPI.Services;

namespace FilmAPI.Endpoints;

/// <summary>
/// Raggruppa gli endpoint protetti per la gestione dei media caricati.
/// </summary>
public static class MediaEndpoints
{
    /// <summary>
    /// Mappa il gruppo <c>/media</c> per operazioni di upload, consultazione e gestione dei contenuti multimediali.
    /// Richiede <c>RequireAuthorization("PowerUserOrAdmin")</c>.
    /// Esegue salvataggio e gestione di file e metadati con effetti sullo storage applicativo.
    /// </summary>
    /// <param name="app">Applicazione web su cui registrare gli endpoint.</param>
    /// <returns>Non restituisce valori.</returns>
    public static void MapMediaEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/media").RequireAuthorization("PowerUserOrAdmin");

        group.MapPost("/covers", async (HttpRequest request, IMediaService service) =>
        {
            try
            {
                IFormFile? file = null;
                
                try
                {
                    var form = await request.ReadFormAsync();
                    file = form.Files.GetFile("file");
                }
                catch
                {
                    return Results.BadRequest("Nessun file caricato");
                }
                
                if (file is null || file.Length == 0)
                {
                    return Results.BadRequest("Nessun file caricato");
                }

                var result = await service.UploadCoverAsync(file);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .DisableAntiforgery();
    }
}
