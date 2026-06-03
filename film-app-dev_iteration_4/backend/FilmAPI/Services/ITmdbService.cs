namespace FilmAPI.Services;

/// <summary>
///     Servizio per l'integrazione con l'API TMDB (The Movie Database).
///     Recupera film, dettagli, immagini e trailer dal catalogo TMDB.
///     Usato per l'importazione automatica di film e locandine.
/// </summary>
public interface ITmdbService
{
    /// <summary>Cerca film per titolo (testo libero).</summary>
    Task<TmdbSearchResult> SearchMoviesAsync(string query, int page = 1);

    /// <summary>Recupera dettagli di un film per ID TMDB.</summary>
    Task<TmdbMovieDetails?> GetMovieDetailsAsync(int tmdbId);

    /// <summary>Recupera dettagli di un film per ID IMDb.</summary>
    Task<TmdbMovieDetails?> GetMovieDetailsAsync(string imdbId);

    /// <summary>Recupera i film più popolari del momento.</summary>
    Task<TmdbSearchResult> GetPopularMoviesAsync(int page = 1);

    /// <summary>Recupera i film in uscita prossimamente.</summary>
    Task<TmdbSearchResult> GetUpcomingMoviesAsync(int page = 1);

    /// <summary>Recupera i film al momento in sala.</summary>
    Task<TmdbSearchResult> GetNowPlayingMoviesAsync(int page = 1);

    /// <summary>Genera URL per il poster di un film (taglia configurabile).</summary>
    Task<string?> GetPosterUrlAsync(string posterPath, string size = "w500");

    /// <summary>Genera URL per lo sfondo di un film.</summary>
    Task<string?> GetBackdropUrlAsync(string backdropPath, string size = "w1280");
}

/// <summary>Risultato paginato della ricerca TMDB.</summary>
public class TmdbSearchResult
{
    /// <summary>Pagina corrente.</summary>
    public int Page { get; set; }

    /// <summary>Lista dei film trovati.</summary>
    public List<TmdbMovie> Results { get; set; } = new();

    /// <summary>Numero totale di risultati (approssimativo).</summary>
    public int TotalResults { get; set; }

    /// <summary>Numero totale di pagine.</summary>
    public int TotalPages { get; set; }
}

/// <summary>Risultato sintetico di un film dalla ricerca TMDB.</summary>
public class TmdbMovie
{
    /// <summary>ID univoco del film su TMDB.</summary>
    public int Id { get; set; }

    /// <summary>Titolo del film.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Titolo originale (nella lingua d'origine).</summary>
    public string OriginalTitle { get; set; } = string.Empty;

    /// <summary>Sinossi / descrizione del film.</summary>
    public string Overview { get; set; } = string.Empty;

    /// <summary>Path relativo del poster (da combinare con base URL).</summary>
    public string PosterPath { get; set; } = string.Empty;

    /// <summary>Path relativo dello sfondo.</summary>
    public string BackdropPath { get; set; } = string.Empty;

    /// <summary>Data di uscita (formato YYYY-MM-DD).</summary>
    public string ReleaseDate { get; set; } = string.Empty;

    /// <summary>Lista degli ID dei generi TMDB.</summary>
    public List<int> GenreIds { get; set; } = new();

    /// <summary>Punteggio di popolarità TMDB.</summary>
    public double Popularity { get; set; }

    /// <summary>Voto medio (0-10).</summary>
    public double VoteAverage { get; set; }

    /// <summary>Numero di voti ricevuti.</summary>
    public int VoteCount { get; set; }

    /// <summary>Indica se il film è per adulti.</summary>
    public bool Adult { get; set; }

    /// <summary>Lingua originale del film (codice ISO).</summary>
    public string OriginalLanguage { get; set; } = string.Empty;

    /// <summary>Indica se è presente un video.</summary>
    public bool Video { get; set; }
}

/// <summary>Dettaglio completo di un film da TMDB.</summary>
public class TmdbMovieDetails
{
    /// <summary>ID TMDB.</summary>
    public int Id { get; set; }

    /// <summary>Titolo del film.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Titolo originale.</summary>
    public string OriginalTitle { get; set; } = string.Empty;

    /// <summary>Lingua originale (codice ISO).</summary>
    public string OriginalLanguage { get; set; } = string.Empty;

    /// <summary>Sinossi / trama.</summary>
    public string Overview { get; set; } = string.Empty;

    /// <summary>Tagline pubblicitaria.</summary>
    public string Tagline { get; set; } = string.Empty;

    /// <summary>Path relativo del poster.</summary>
    public string PosterPath { get; set; } = string.Empty;

    /// <summary>Path relativo dello sfondo.</summary>
    public string BackdropPath { get; set; } = string.Empty;

    /// <summary>Data di uscita (YYYY-MM-DD).</summary>
    public string ReleaseDate { get; set; } = string.Empty;

    /// <summary>Durata in minuti.</summary>
    public int Runtime { get; set; }

    /// <summary>Stato del film (Released, Planned, In Production, etc.).</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Popolarità TMDB.</summary>
    public double Popularity { get; set; }

    /// <summary>Voto medio (0-10).</summary>
    public double VoteAverage { get; set; }

    /// <summary>Numero voti.</summary>
    public int VoteCount { get; set; }

    /// <summary>ID IMDb (formato "ttXXXX").</summary>
    public string ImdbId { get; set; } = string.Empty;

    /// <summary>Sito web ufficiale del film.</summary>
    public string Homepage { get; set; } = string.Empty;

    /// <summary>Budget di produzione in dollari.</summary>
    public long Budget { get; set; }

    /// <summary>Incasso totale in dollari.</summary>
    public long Revenue { get; set; }

    /// <summary>Lista dei generi con nome.</summary>
    public List<TmdbGenre> Genres { get; set; } = new();

    /// <summary>Case di produzione.</summary>
    public List<TmdbProductionCompany> ProductionCompanies { get; set; } = new();

    /// <summary>Paesi di produzione.</summary>
    public List<TmdbProductionCountry> ProductionCountries { get; set; } = new();

    /// <summary>Lingue parlate nel film.</summary>
    public List<TmdbSpokenLanguage> SpokenLanguages { get; set; } = new();

    /// <summary>Crediti (cast e crew).</summary>
    public TmdbCredits? Credits { get; set; }

    /// <summary>Video e trailer.</summary>
    public TmdbVideos? Videos { get; set; }

    /// <summary>Imagini extra (backdrop, poster, logo).</summary>
    public TmdbImages? Images { get; set; }
}

/// <summary>Genere cinematografico TMDB.</summary>
public class TmdbGenre { public int Id { get; set; } public string Name { get; set; } = string.Empty; }

/// <summary>Casa di produzione TMDB.</summary>
public class TmdbProductionCompany { public int Id { get; set; } public string Name { get; set; } = string.Empty; public string LogoPath { get; set; } = string.Empty; public string OriginCountry { get; set; } = string.Empty; }

/// <summary>Paese di produzione TMDB.</summary>
public class TmdbProductionCountry { public string Iso31661 { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }

/// <summary>Lingua parlata TMDB.</summary>
public class TmdbSpokenLanguage { public string Iso6391 { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string EnglishName { get; set; } = string.Empty; }

/// <summary>Crediti TMDB (cast + crew).</summary>
public class TmdbCredits { public List<TmdbCastMember> Cast { get; set; } = new(); public List<TmdbCrewMember> Crew { get; set; } = new(); }

/// <summary>Membro del cast TMDB.</summary>
public class TmdbCastMember { public int Id { get; set; } public string Name { get; set; } = string.Empty; public string Character { get; set; } = string.Empty; public string ProfilePath { get; set; } = string.Empty; public int Order { get; set; } }

/// <summary>Membro della crew TMDB.</summary>
public class TmdbCrewMember { public int Id { get; set; } public string Name { get; set; } = string.Empty; public string Job { get; set; } = string.Empty; public string Department { get; set; } = string.Empty; public string ProfilePath { get; set; } = string.Empty; }

/// <summary>Video TMDB (trailer, teaser).</summary>
public class TmdbVideos { public List<TmdbVideo> Results { get; set; } = new(); }

/// <summary>Singolo video TMDB.</summary>
public class TmdbVideo { public string Id { get; set; } = string.Empty; public string Iso6391 { get; set; } = string.Empty; public string Iso31661 { get; set; } = string.Empty; public string Key { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string Site { get; set; } = string.Empty; public int Size { get; set; } public string Type { get; set; } = string.Empty; public bool Official { get; set; } public string PublishedAt { get; set; } = string.Empty; }

/// <summary>Immagini TMDB (backdrop, poster, logo).</summary>
public class TmdbImages { public List<TmdbImage> Backdrops { get; set; } = new(); public List<TmdbImage> Posters { get; set; } = new(); public List<TmdbImage> Logos { get; set; } = new(); }

/// <summary>Singola immagine TMDB.</summary>
public class TmdbImage { public string FilePath { get; set; } = string.Empty; public int Width { get; set; } public int Height { get; set; } public double AspectRatio { get; set; } public string Iso6391 { get; set; } = string.Empty; public double VoteAverage { get; set; } public int VoteCount { get; set; } }
