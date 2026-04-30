namespace FilmAPI.Services;

public interface ITmdbService
{
    Task<TmdbSearchResult> SearchMoviesAsync(string query, int page = 1);
    Task<TmdbMovieDetails?> GetMovieDetailsAsync(int tmdbId);
    Task<TmdbMovieDetails?> GetMovieDetailsAsync(string imdbId);
    Task<TmdbSearchResult> GetPopularMoviesAsync(int page = 1);
    Task<TmdbSearchResult> GetUpcomingMoviesAsync(int page = 1);
    Task<TmdbSearchResult> GetNowPlayingMoviesAsync(int page = 1);
    Task<string?> GetPosterUrlAsync(string posterPath, string size = "w500");
    Task<string?> GetBackdropUrlAsync(string backdropPath, string size = "w1280");
}

public class TmdbSearchResult
{
    public int Page { get; set; }
    public List<TmdbMovie> Results { get; set; } = new();
    public int TotalResults { get; set; }
    public int TotalPages { get; set; }
}

public class TmdbMovie
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OriginalTitle { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    public string PosterPath { get; set; } = string.Empty;
    public string BackdropPath { get; set; } = string.Empty;
    public string ReleaseDate { get; set; } = string.Empty;
    public List<int> GenreIds { get; set; } = new();
    public double Popularity { get; set; }
    public double VoteAverage { get; set; }
    public int VoteCount { get; set; }
    public bool Adult { get; set; }
    public string OriginalLanguage { get; set; } = string.Empty;
    public bool Video { get; set; }
}

public class TmdbMovieDetails
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OriginalTitle { get; set; } = string.Empty;
    public string OriginalLanguage { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    public string Tagline { get; set; } = string.Empty;
    public string PosterPath { get; set; } = string.Empty;
    public string BackdropPath { get; set; } = string.Empty;
    public string ReleaseDate { get; set; } = string.Empty;
    public int Runtime { get; set; }
    public string Status { get; set; } = string.Empty;
    public double Popularity { get; set; }
    public double VoteAverage { get; set; }
    public int VoteCount { get; set; }
    public string ImdbId { get; set; } = string.Empty;
    public string Homepage { get; set; } = string.Empty;
    public long Budget { get; set; }
    public long Revenue { get; set; }
    public List<TmdbGenre> Genres { get; set; } = new();
    public List<TmdbProductionCompany> ProductionCompanies { get; set; } = new();
    public List<TmdbProductionCountry> ProductionCountries { get; set; } = new();
    public List<TmdbSpokenLanguage> SpokenLanguages { get; set; } = new();
    public TmdbCredits? Credits { get; set; }
    public TmdbVideos? Videos { get; set; }
    public TmdbImages? Images { get; set; }
}

public class TmdbGenre
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TmdbProductionCompany
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LogoPath { get; set; } = string.Empty;
    public string OriginCountry { get; set; } = string.Empty;
}

public class TmdbProductionCountry
{
    public string Iso31661 { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class TmdbSpokenLanguage
{
    public string Iso6391 { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
}

public class TmdbCredits
{
    public List<TmdbCastMember> Cast { get; set; } = new();
    public List<TmdbCrewMember> Crew { get; set; } = new();
}

public class TmdbCastMember
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Character { get; set; } = string.Empty;
    public string ProfilePath { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class TmdbCrewMember
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Job { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string ProfilePath { get; set; } = string.Empty;
}

public class TmdbVideos
{
    public List<TmdbVideo> Results { get; set; } = new();
}

public class TmdbVideo
{
    public string Id { get; set; } = string.Empty;
    public string Iso6391 { get; set; } = string.Empty;
    public string Iso31661 { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Site { get; set; } = string.Empty;
    public int Size { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool Official { get; set; }
    public string PublishedAt { get; set; } = string.Empty;
}

public class TmdbImages
{
    public List<TmdbImage> Backdrops { get; set; } = new();
    public List<TmdbImage> Posters { get; set; } = new();
    public List<TmdbImage> Logos { get; set; } = new();
}

public class TmdbImage
{
    public string FilePath { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public double AspectRatio { get; set; }
    public string Iso6391 { get; set; } = string.Empty;
    public double VoteAverage { get; set; }
    public int VoteCount { get; set; }
}
