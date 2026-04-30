using System.Text.Json;

namespace FilmAPI.Services;

public class TmdbService : ITmdbService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TmdbService> _logger;
    private readonly string _baseUrl = "https://api.themoviedb.org/3";
    private readonly string _imageBaseUrl = "https://image.tmdb.org/t/p";
    private readonly string _apiKey;

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

    public Task<string?> GetPosterUrlAsync(string posterPath, string size = "w500")
    {
        if (string.IsNullOrEmpty(posterPath))
            return Task.FromResult<string?>(null);
        
        var cleanPath = posterPath.StartsWith("/") ? posterPath.Substring(1) : posterPath;
        return Task.FromResult<string?>($"{_imageBaseUrl}/{size}/{cleanPath}");
    }

    public Task<string?> GetBackdropUrlAsync(string backdropPath, string size = "w1280")
    {
        if (string.IsNullOrEmpty(backdropPath))
            return Task.FromResult<string?>(null);
        
        var cleanPath = backdropPath.StartsWith("/") ? backdropPath.Substring(1) : backdropPath;
        return Task.FromResult<string?>($"{_imageBaseUrl}/{size}/{cleanPath}");
    }

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
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TmdbPersonResult
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
