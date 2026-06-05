using System.Text.Json;

namespace FilmAPI.Services;

/// <summary>
///     Servizio per l'integrazione con l'API TMDB (The Movie Database).
///     Recupera dati cinematografici: film popolari, ricerca film,
///     dettagli film, immagini, cast. Usato per arricchire il catalogo
///     CineBase con dati esterni e importare film tramite API.
///     Chiave API configurata via TMDB_API_KEY o TMDB:ApiKey.
/// </summary>
public class TmdbService : ITmdbService
{
    /// <summary>HttpClient per chiamate REST all'API TMDB.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>Logger per errori e debug.</summary>
    private readonly ILogger<TmdbService> _logger;

    /// <summary>URL base dell'API TMDB v3.</summary>
    private readonly string _baseUrl = "https://api.themoviedb.org/3";

    /// <summary>URL base per le immagini TMDB.</summary>
    private readonly string _imageBaseUrl = "https://image.tmdb.org/t/p";

    /// <summary>Chiave API per autenticazione TMDB.</summary>
    private readonly string _apiKey;

    /// <summary>
    /// Esegue l''operazione TmdbService del servizio.
    /// </summary>
    /// <param name="httpClient">Parametro necessario per l'operazione: httpClient.</param>
    /// <param name="configuration">Parametro necessario per l'operazione: configuration.</param>
    /// <param name="logger">Parametro necessario per l'operazione: logger.</param>
    /// <returns>Restituisce il risultato dell'operazione quando questa ha esito positivo; altrimenti il chiamante riceve un'eccezione o un risultato nullo/booleano secondo il contratto del metodo.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può effettuare chiamate a servizi esterni o API HTTP.
    /// </remarks>
    public TmdbService(HttpClient httpClient, IConfiguration configuration, ILogger<TmdbService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Usa la chiave API fornita
        _apiKey = Environment.GetEnvironmentVariable("TMDB_API_KEY") 
            ?? configuration["TMDB:ApiKey"] 
            ?? "9ca1b83f444b1fb0a51ec1cd7e22f417"; // Chiave API di default
        
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    /// <summary>
    /// Esegue l''operazione SearchMoviesAsync del servizio.
    /// </summary>
    /// <param name="query">Parametro necessario per l'operazione: query.</param>
    /// <param name="page">Parametro necessario per l'operazione: page.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: può effettuare chiamate a servizi esterni o API HTTP.
    /// </remarks>
    public async Task<TmdbSearchResult> SearchMoviesAsync(string query, int page = 1)
    {
        try
        {
            var encodedQuery = Uri.EscapeDataString(query);
            var url = $"{_baseUrl}/search/movie?api_key={_apiKey}&query={encodedQuery}&page={page}&language=it-IT&include_adult=false";
            
            _logger.LogInformation("Ricerca TMDB: {Query}, Pagina: {Page}", query, page);
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TmdbSearchResult>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            return result ?? new TmdbSearchResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la ricerca TMDB: {Query}", query);
            throw;
        }
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetMovieDetailsAsync del servizio.
    /// </summary>
    /// <param name="tmdbId">Identificativo necessario per individuare l'entità o il contesto di lavoro: tmdbId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: può effettuare chiamate a servizi esterni o API HTTP.
    /// </remarks>
    public async Task<TmdbMovieDetails?> GetMovieDetailsAsync(int tmdbId)
    {
        try
        {
            var url = $"{_baseUrl}/movie/{tmdbId}?api_key={_apiKey}&language=it-IT&append_to_response=credits,videos,images";
            
            _logger.LogInformation("Recupero dettagli TMDB: {TmdbId}", tmdbId);
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TmdbMovieDetails>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dettagli TMDB: {TmdbId}", tmdbId);
            throw;
        }
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetMovieDetailsAsync del servizio.
    /// </summary>
    /// <param name="imdbId">Identificativo necessario per individuare l'entità o il contesto di lavoro: imdbId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: può effettuare chiamate a servizi esterni o API HTTP.
    /// </remarks>
    public async Task<TmdbMovieDetails?> GetMovieDetailsAsync(string imdbId)
    {
        try
        {
            var url = $"{_baseUrl}/find/{imdbId}?api_key={_apiKey}&external_source=imdb_id&language=it-IT";
            
            _logger.LogInformation("Recupero dettagli TMDB da IMDB ID: {ImdbId}", imdbId);
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TmdbFindResult>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            var movie = result?.MovieResults?.FirstOrDefault();
            if (movie != null)
            {
                return await GetMovieDetailsAsync(movie.Id);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dettagli TMDB da IMDB: {ImdbId}", imdbId);
            throw;
        }
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetPosterUrlAsync del servizio.
    /// </summary>
    /// <param name="posterPath">Parametro necessario per l'operazione: posterPath.</param>
    /// <param name="size">Parametro necessario per l'operazione: size.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: può effettuare chiamate a servizi esterni o API HTTP.
    /// </remarks>
    public Task<string?> GetPosterUrlAsync(string posterPath, string size = "w500")
    {
        if (string.IsNullOrEmpty(posterPath))
            return Task.FromResult<string?>(null);
        
        var cleanPath = posterPath.StartsWith("/") ? posterPath.Substring(1) : posterPath;
        return Task.FromResult<string?>($"{_imageBaseUrl}/{size}/{cleanPath}");
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetBackdropUrlAsync del servizio.
    /// </summary>
    /// <param name="backdropPath">Parametro necessario per l'operazione: backdropPath.</param>
    /// <param name="size">Parametro necessario per l'operazione: size.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: può effettuare chiamate a servizi esterni o API HTTP.
    /// </remarks>
    public Task<string?> GetBackdropUrlAsync(string backdropPath, string size = "w1280")
    {
        if (string.IsNullOrEmpty(backdropPath))
            return Task.FromResult<string?>(null);
        
        var cleanPath = backdropPath.StartsWith("/") ? backdropPath.Substring(1) : backdropPath;
        return Task.FromResult<string?>($"{_imageBaseUrl}/{size}/{cleanPath}");
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetPopularMoviesAsync del servizio.
    /// </summary>
    /// <param name="page">Parametro necessario per l'operazione: page.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: può effettuare chiamate a servizi esterni o API HTTP.
    /// </remarks>
    public async Task<TmdbSearchResult> GetPopularMoviesAsync(int page = 1)
    {
        try
        {
            var url = $"{_baseUrl}/movie/popular?api_key={_apiKey}&page={page}&language=it-IT";
            
            _logger.LogInformation("Recupero film popolari TMDB, Pagina: {Page}", page);
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TmdbSearchResult>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            return result ?? new TmdbSearchResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero film popolari");
            throw;
        }
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetUpcomingMoviesAsync del servizio.
    /// </summary>
    /// <param name="page">Parametro necessario per l'operazione: page.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: può effettuare chiamate a servizi esterni o API HTTP.
    /// </remarks>
    public async Task<TmdbSearchResult> GetUpcomingMoviesAsync(int page = 1)
    {
        try
        {
            var url = $"{_baseUrl}/movie/upcoming?api_key={_apiKey}&page={page}&language=it-IT";
            
            _logger.LogInformation("Recupero film in uscita TMDB, Pagina: {Page}", page);
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TmdbSearchResult>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            return result ?? new TmdbSearchResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero film in uscita");
            throw;
        }
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetNowPlayingMoviesAsync del servizio.
    /// </summary>
    /// <param name="page">Parametro necessario per l'operazione: page.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: può effettuare chiamate a servizi esterni o API HTTP.
    /// </remarks>
    public async Task<TmdbSearchResult> GetNowPlayingMoviesAsync(int page = 1)
    {
        try
        {
            var url = $"{_baseUrl}/movie/now_playing?api_key={_apiKey}&page={page}&language=it-IT&region=IT";
            
            _logger.LogInformation("Recupero film al cinema TMDB, Pagina: {Page}", page);
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TmdbSearchResult>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            return result ?? new TmdbSearchResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero film al cinema");
            throw;
        }
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetYoutubeTrailerUrl del servizio.
    /// </summary>
    /// <param name="movie">Parametro necessario per l'operazione: movie.</param>
    /// <returns>Restituisce il risultato dell'operazione quando questa ha esito positivo; altrimenti il chiamante riceve un'eccezione o un risultato nullo/booleano secondo il contratto del metodo.</returns>
    /// <remarks>
    /// Effetti collaterali: non introduce effetti collaterali esterni evidenti oltre alla logica di lettura o validazione.
    /// </remarks>
    public string? GetYoutubeTrailerUrl(TmdbMovieDetails movie)
    {
        if (movie?.Videos?.Results == null || !movie.Videos.Results.Any())
            return null;

        var trailer = movie.Videos.Results
            .FirstOrDefault(v => v.Type == "Trailer" && v.Site == "YouTube" && v.Iso6391 == "it" && v.Official)
            ?? movie.Videos.Results.FirstOrDefault(v => v.Type == "Trailer" && v.Site == "YouTube" && v.Official)
            ?? movie.Videos.Results.FirstOrDefault(v => v.Type == "Trailer" && v.Site == "YouTube")
            ?? movie.Videos.Results.FirstOrDefault(v => v.Site == "YouTube");

        if (trailer != null)
        {
            return $"https://www.youtube.com/watch?v={trailer.Key}";
        }

        return null;
    }
}

public class TmdbFindResult
{
    public List<TmdbMovie> MovieResults { get; set; } = new();
    public List<TmdbTvResult> TvResults { get; set; } = new();
    public List<TmdbPersonResult> PersonResults { get; set; } = new();
}

public class TmdbTvResult
{
    /// <summary>
    /// Rappresenta la dipendenza o il dato esposto tramite la proprietà Id.
    /// </summary>
    /// <remarks>
    /// Serve al servizio per completare le sue operazioni di lettura, validazione, persistenza o integrazione esterna.
    /// </remarks>
    public int Id { get; set; }
    /// <summary>
    /// Rappresenta la dipendenza o il dato esposto tramite la proprietà Name.
    /// </summary>
    /// <remarks>
    /// Serve al servizio per completare le sue operazioni di lettura, validazione, persistenza o integrazione esterna.
    /// </remarks>
    public string Name { get; set; } = string.Empty;
}

public class TmdbPersonResult
{
    /// <summary>
    /// Rappresenta la dipendenza o il dato esposto tramite la proprietà Id.
    /// </summary>
    /// <remarks>
    /// Serve al servizio per completare le sue operazioni di lettura, validazione, persistenza o integrazione esterna.
    /// </remarks>
    public int Id { get; set; }
    /// <summary>
    /// Rappresenta la dipendenza o il dato esposto tramite la proprietà Name.
    /// </summary>
    /// <remarks>
    /// Serve al servizio per completare le sue operazioni di lettura, validazione, persistenza o integrazione esterna.
    /// </remarks>
    public string Name { get; set; } = string.Empty;
}
