using FilmAPI.DTO;
using FilmAPI.Services;

namespace FilmAPI.Endpoints;

public static class ProgrammazioneEndpoints
{
    public static void MapProgrammazioneEndpoints(this WebApplication app)
    {
        MapProgrammazioneFilms(app);
        MapProgrammazioneCinemas(app);
        MapFilmScheda(app);
        MapMyCinemas(app);
    }

    private static void MapProgrammazioneFilms(WebApplication app)
    {
        app.MapGet("/programmazione/films", async (
            string? tab,
            string? search,
            int? categoriaId,
            int? cinemaId,
            int? page,
            int? pageSize,
            IProgrammazioneService service) =>
        {
            var results = await service.GetFilmsAsync(tab, search, categoriaId, cinemaId, page ?? 1, pageSize ?? 20);
            return Results.Ok(results);
        }).AllowAnonymous();
    }

    private static void MapProgrammazioneCinemas(WebApplication app)
    {
        app.MapGet("/programmazione/cinemas", async (
            double? lat,
            double? lng,
            IProgrammazioneService service) =>
        {
            var results = await service.GetCinemasAsync(lat, lng);
            return Results.Ok(results);
        }).AllowAnonymous();
    }

    private static void MapFilmScheda(WebApplication app)
    {
        app.MapGet("/films/{id}/scheda", async (
            int id,
            int? cinemaId,
            IProgrammazioneService service) =>
        {
            var result = await service.GetFilmSchedaAsync(id, cinemaId);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).AllowAnonymous();
    }

    private static void MapMyCinemas(WebApplication app)
    {
        app.MapGet("/my-cinemas", async (IProgrammazioneService service, double? lat, double? lng) =>
        {
            var results = lat.HasValue && lng.HasValue
                ? await service.GetCinemasAsync(lat, lng)
                : await service.GetMyCinemasAsync();
            return Results.Ok(results);
        }).AllowAnonymous();

        app.MapGet("/my-cinemas/{cinemaId}/schedule", async (
            int cinemaId,
            DateOnly? date,
            IProgrammazioneService service) =>
        {
            var result = await service.GetCinemaScheduleAsync(cinemaId, date);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).AllowAnonymous();
    }
}
