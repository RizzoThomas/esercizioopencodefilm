using Microsoft.AspNetCore.Mvc;
using FilmAPI.Services;
using FilmAPI.Data;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class TmdbEndpoints
{
    public static IEndpointRouteBuilder MapTmdbEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tmdb")
            .WithTags("TMDB")
            .WithOpenApi()
            .RequireAuthorization();

        // Ricerca film su TMDB
        group.MapGet("/search", async (
            [FromServices] ITmdbService tmdbService,
            [FromQuery] string query,
            [FromQuery] int page = 1) =>
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Results.BadRequest(new { error = "Query di ricerca richiesta" });
            }

            var result = await tmdbService.SearchMoviesAsync(query, page);
            return Results.Ok(result);
        })
        .WithName("SearchTmdbMovies")
        .WithDescription("Cerca film su The Movie Database (TMDB)")
        .Produces<TmdbSearchResult>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // Dettagli film TMDB per ID
        group.MapGet("/movie/{tmdbId:int}", async (
            [FromServices] ITmdbService tmdbService,
            int tmdbId) =>
        {
            var movie = await tmdbService.GetMovieDetailsAsync(tmdbId);
            
            if (movie == null)
            {
                return Results.NotFound(new { error = $"Film TMDB {tmdbId} non trovato" });
            }

            return Results.Ok(movie);
        })
        .WithName("GetTmdbMovieDetails")
        .WithDescription("Recupera dettagli completi di un film da TMDB")
        .Produces<TmdbMovieDetails>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // Film popolari
        group.MapGet("/popular", async (
            [FromServices] ITmdbService tmdbService,
            [FromQuery] int page = 1) =>
        {
            var result = await tmdbService.GetPopularMoviesAsync(page);
            return Results.Ok(result);
        })
        .WithName("GetPopularMovies")
        .WithDescription("Recupera film popolari da TMDB")
        .Produces<TmdbSearchResult>(StatusCodes.Status200OK);

        // Film in uscita
        group.MapGet("/upcoming", async (
            [FromServices] ITmdbService tmdbService,
            [FromQuery] int page = 1) =>
        {
            var result = await tmdbService.GetUpcomingMoviesAsync(page);
            return Results.Ok(result);
        })
        .WithName("GetUpcomingMovies")
        .WithDescription("Recupera film in uscita da TMDB")
        .Produces<TmdbSearchResult>(StatusCodes.Status200OK);

        // Film al cinema ora
        group.MapGet("/now-playing", async (
            [FromServices] ITmdbService tmdbService,
            [FromQuery] int page = 1) =>
        {
            var result = await tmdbService.GetNowPlayingMoviesAsync(page);
            return Results.Ok(result);
        })
        .WithName("GetNowPlayingMovies")
        .WithDescription("Recupera film attualmente al cinema da TMDB")
        .Produces<TmdbSearchResult>(StatusCodes.Status200OK);

        // Importa film da TMDB nel database locale
        group.MapPost("/import/{tmdbId:int}", async (
            [FromServices] ITmdbService tmdbService,
            [FromServices] FilmDbContext context,
            int tmdbId) =>
        {
            // Recupera dettagli dal TMDB
            var tmdbMovie = await tmdbService.GetMovieDetailsAsync(tmdbId);
            
            if (tmdbMovie == null)
            {
                return Results.NotFound(new { error = $"Film TMDB {tmdbId} non trovato" });
            }

            // Verifica se il film esiste già (per TMDB ID)
            var existingFilm = await context.Films
                .FirstOrDefaultAsync(f => f.TmdbId == tmdbId);
            
            if (existingFilm != null)
            {
                return Results.Conflict(new { 
                    error = "Film già esistente nel database",
                    filmId = existingFilm.Id,
                    message = $"Il film '{existingFilm.Titolo}' è già stato importato (ID: {existingFilm.Id})"
                });
            }

            // Cerca o crea il regista
            var registaNome = tmdbMovie.Credits?.Crew
                ?.FirstOrDefault(c => c.Job == "Director")?.Name ?? "Regista sconosciuto";
            
            var nomeParts = registaNome.Split(' ', 2);
            var nome = nomeParts[0];
            var cognome = nomeParts.Length > 1 ? nomeParts[1] : "";

            var regista = await context.Registi
                .FirstOrDefaultAsync(r => r.Nome == nome && r.Cognome == cognome);
            
            if (regista == null)
            {
                regista = new Regista
                {
                    Nome = nome,
                    Cognome = cognome,
                    Nazionalita = "Sconosciuta"
                };
                context.Registi.Add(regista);
                await context.SaveChangesAsync();
            }

            // Estrai cast principale (primi 5 attori)
            var castPrincipale = tmdbMovie.Credits?.Cast
                ?.Take(5)
                .Select(c => $"{c.Name} ({c.Character})")
                .ToList() ?? new List<string>();

            // Costruisci URL poster
            var posterUrl = await tmdbService.GetPosterUrlAsync(tmdbMovie.PosterPath, "w500");

            // Crea il nuovo film
            var film = new Film
            {
                Titolo = tmdbMovie.Title,
                DataProduzione = string.IsNullOrEmpty(tmdbMovie.ReleaseDate) 
                    ? DateTime.UtcNow 
                    : DateTime.Parse(tmdbMovie.ReleaseDate),
                RegistaId = regista.Id,
                Durata = tmdbMovie.Runtime,
                CopertinaPath = posterUrl,
                DescrizioneLunga = tmdbMovie.Overview,
                CastText = string.Join(", ", castPrincipale),
                DataRilascio = string.IsNullOrEmpty(tmdbMovie.ReleaseDate) 
                    ? null 
                    : DateOnly.Parse(tmdbMovie.ReleaseDate),
                TmdbId = tmdbId,
                ImdbId = tmdbMovie.ImdbId,
                VoteAverage = tmdbMovie.VoteAverage,
                VoteCount = tmdbMovie.VoteCount,
                Popularity = tmdbMovie.Popularity,
                BackdropPath = tmdbMovie.BackdropPath,
                OriginalLanguage = tmdbMovie.OriginalLanguage,
                Homepage = tmdbMovie.Homepage
            };

            context.Films.Add(film);
            await context.SaveChangesAsync();

            // Associa categorie (genres) se esistono
            if (tmdbMovie.Genres?.Any() == true)
            {
                foreach (var genre in tmdbMovie.Genres)
                {
                    var categoria = await context.Categorie
                        .FirstOrDefaultAsync(c => c.Nome == genre.Name);
                    
                    if (categoria != null)
                    {
                        context.FilmCategorie.Add(new FilmCategoria
                        {
                            FilmId = film.Id,
                            CategoriaId = categoria.Id
                        });
                    }
                }
                await context.SaveChangesAsync();
            }

            return Results.Created($"/api/films/{film.Id}", new 
            {
                message = $"Film '{film.Titolo}' importato con successo",
                filmId = film.Id,
                tmdbId = film.TmdbId,
                posterUrl = film.CopertinaPath
            });
        })
        .WithName("ImportTmdbMovie")
        .WithDescription("Importa un film da TMDB nel database locale")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        // URL immagine poster
        group.MapGet("/image/poster", (
            [FromServices] ITmdbService tmdbService,
            [FromQuery] string path,
            [FromQuery] string size = "w500") =>
        {
            if (string.IsNullOrEmpty(path))
            {
                return Results.BadRequest(new { error = "Path immagine richiesto" });
            }

            var url = tmdbService.GetPosterUrlAsync(path, size).Result;
            return Results.Ok(new { url });
        })
        .WithName("GetPosterUrl")
        .WithDescription("Genera URL completo per poster TMDB")
        .AllowAnonymous();

        // URL immagine backdrop
        group.MapGet("/image/backdrop", (
            [FromServices] ITmdbService tmdbService,
            [FromQuery] string path,
            [FromQuery] string size = "w1280") =>
        {
            if (string.IsNullOrEmpty(path))
            {
                return Results.BadRequest(new { error = "Path immagine richiesto" });
            }

            var url = tmdbService.GetBackdropUrlAsync(path, size).Result;
            return Results.Ok(new { url });
        })
        .WithName("GetBackdropUrl")
        .WithDescription("Genera URL completo per backdrop TMDB")
        .AllowAnonymous();

        return app;
    }
}
