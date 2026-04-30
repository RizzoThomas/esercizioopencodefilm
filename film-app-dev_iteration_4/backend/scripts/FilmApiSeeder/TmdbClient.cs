using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace FilmApiSeeder;

internal sealed class TmdbClient
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<int, TmdbPersonDetails> _personCache = new();
    private string? _posterBaseUrl;

    public TmdbClient(HttpClient httpClient, string bearerToken)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.themoviedb.org/3/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<TmdbMovieDetails?> SearchMovieWithDetailsAsync(MovieTarget target, CancellationToken cancellationToken)
    {
        var queries = new List<(string Title, int? Year)>
        {
            (target.Title, target.Year)
        };

        foreach (var alias in target.Aliases.Where(a => !string.IsNullOrWhiteSpace(a)))
        {
            queries.Add((alias, target.Year));
        }

        foreach (var query in queries)
        {
            var result = await SearchMovieAsync(query.Title, query.Year, cancellationToken);
            if (result is null)
            {
                continue;
            }

            return await GetMovieDetailsAsync(result.Id, cancellationToken);
        }

        return null;
    }

    public async Task<IReadOnlyList<int>> DiscoverMovieIdsByGenreAsync(int genreId, int page, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetFromJsonAsync<TmdbPagedMovieResponse>(
            $"discover/movie?language=it-IT&include_adult=false&sort_by=popularity.desc&with_genres={genreId}&page={page}",
            cancellationToken);

        return response?.Results?.Select(r => r.Id).ToList() ?? [];
    }

    public async Task<TmdbMovieDetails?> GetMovieDetailsAsync(int movieId, CancellationToken cancellationToken)
    {
        var details = await _httpClient.GetFromJsonAsync<TmdbMovieDetails>(
            $"movie/{movieId}?language=it-IT&append_to_response=credits",
            cancellationToken);

        if (details is null)
        {
            return null;
        }

        details.PosterFullUrl = await BuildPosterUrlAsync(details.PosterPath, details.BackdropPath, cancellationToken);
        return details;
    }

    public async Task<TmdbPersonDetails?> GetPersonDetailsAsync(int personId, CancellationToken cancellationToken)
    {
        if (_personCache.TryGetValue(personId, out var cached))
        {
            return cached;
        }

        var details = await _httpClient.GetFromJsonAsync<TmdbPersonDetails>($"person/{personId}?language=it-IT", cancellationToken);
        if (details is not null)
        {
            _personCache[personId] = details;
        }

        return details;
    }

    private async Task<TmdbMovieSearchResult?> SearchMovieAsync(string title, int? year, CancellationToken cancellationToken)
    {
        var encodedTitle = Uri.EscapeDataString(title);
        var endpoint = $"search/movie?language=it-IT&include_adult=false&query={encodedTitle}";
        if (year.HasValue)
        {
            endpoint += $"&year={year.Value}";
        }

        var response = await _httpClient.GetFromJsonAsync<TmdbPagedMovieResponse>(endpoint, cancellationToken);
        var results = response?.Results ?? [];
        if (results.Count == 0 && year.HasValue)
        {
            response = await _httpClient.GetFromJsonAsync<TmdbPagedMovieResponse>(
                $"search/movie?language=it-IT&include_adult=false&query={encodedTitle}",
                cancellationToken);
            results = response?.Results ?? [];
        }

        return results
            .OrderBy(r => ComputeScore(r, title, year))
            .FirstOrDefault();
    }

    private static int ComputeScore(TmdbMovieSearchResult result, string requestedTitle, int? requestedYear)
    {
        var score = 0;

        if (!string.Equals(Normalize(result.Title), Normalize(requestedTitle), StringComparison.Ordinal))
        {
            score += 5;
        }

        if (!string.Equals(Normalize(result.OriginalTitle), Normalize(requestedTitle), StringComparison.Ordinal))
        {
            score += 2;
        }

        if (requestedYear.HasValue && result.ReleaseYear.HasValue)
        {
            score += Math.Abs(result.ReleaseYear.Value - requestedYear.Value);
        }

        return score;
    }

    private async Task<string?> BuildPosterUrlAsync(string? posterPath, string? backdropPath, CancellationToken cancellationToken)
    {
        var assetPath = !string.IsNullOrWhiteSpace(posterPath) ? posterPath : backdropPath;
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(_posterBaseUrl))
        {
            _posterBaseUrl = await ResolvePosterBaseUrlAsync(cancellationToken);
        }

        return $"{_posterBaseUrl}{assetPath}";
    }

    private async Task<string> ResolvePosterBaseUrlAsync(CancellationToken cancellationToken)
    {
        const string fallback = "https://image.tmdb.org/t/p/w780";

        var configuration = await _httpClient.GetFromJsonAsync<TmdbConfigurationResponse>("configuration", cancellationToken);
        var secureBaseUrl = configuration?.Images?.SecureBaseUrl;
        if (string.IsNullOrWhiteSpace(secureBaseUrl))
        {
            return fallback;
        }

        var preferredSize = configuration!.Images!.PosterSizes?
            .FirstOrDefault(size => string.Equals(size, "w780", StringComparison.OrdinalIgnoreCase))
            ?? configuration.Images.PosterSizes?.LastOrDefault()
            ?? "original";

        return $"{secureBaseUrl}{preferredSize}";
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }
}

internal sealed class TmdbConfigurationResponse
{
    [JsonPropertyName("images")]
    public TmdbImagesConfiguration? Images { get; set; }
}

internal sealed class TmdbImagesConfiguration
{
    [JsonPropertyName("secure_base_url")]
    public string? SecureBaseUrl { get; set; }

    [JsonPropertyName("poster_sizes")]
    public List<string>? PosterSizes { get; set; }
}

internal sealed class TmdbPagedMovieResponse
{
    [JsonPropertyName("results")]
    public List<TmdbMovieSearchResult> Results { get; set; } = [];
}

internal sealed class TmdbMovieSearchResult
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("original_title")]
    public string OriginalTitle { get; set; } = string.Empty;

    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; set; }

    [JsonIgnore]
    public int? ReleaseYear => DateOnly.TryParse(ReleaseDate, out var date) ? date.Year : null;
}

internal sealed class TmdbMovieDetails
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("original_title")]
    public string OriginalTitle { get; set; } = string.Empty;

    [JsonPropertyName("overview")]
    public string? Overview { get; set; }

    [JsonPropertyName("runtime")]
    public int? Runtime { get; set; }

    [JsonPropertyName("release_date")]
    public string? ReleaseDateRaw { get; set; }

    [JsonPropertyName("poster_path")]
    public string? PosterPath { get; set; }

    [JsonPropertyName("backdrop_path")]
    public string? BackdropPath { get; set; }

    [JsonPropertyName("genres")]
    public List<TmdbGenre> Genres { get; set; } = [];

    [JsonPropertyName("credits")]
    public TmdbCredits? Credits { get; set; }

    [JsonIgnore]
    public string? PosterFullUrl { get; set; }

    [JsonIgnore]
    public DateOnly? ReleaseDate => DateOnly.TryParse(ReleaseDateRaw, out var date) ? date : null;
}

internal sealed class TmdbGenre
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

internal sealed class TmdbCredits
{
    [JsonPropertyName("cast")]
    public List<TmdbCastMember> Cast { get; set; } = [];

    [JsonPropertyName("crew")]
    public List<TmdbCrewMember> Crew { get; set; } = [];
}

internal sealed class TmdbCastMember
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("order")]
    public int Order { get; set; }
}

internal sealed class TmdbCrewMember
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("job")]
    public string Job { get; set; } = string.Empty;

    [JsonPropertyName("department")]
    public string Department { get; set; } = string.Empty;
}

internal sealed class TmdbPersonDetails
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("place_of_birth")]
    public string? PlaceOfBirth { get; set; }
}
