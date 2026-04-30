using FilmAPI.DTO;
using FilmAPI.Services;

namespace FilmAPI.Endpoints;

public static class MediaEndpoints
{
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
