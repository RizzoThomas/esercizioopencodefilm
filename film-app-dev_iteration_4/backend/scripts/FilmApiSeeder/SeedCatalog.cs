using FilmAPI.Model;

namespace FilmApiSeeder;

internal sealed record MovieTarget(string Title, int? Year = null, params string[] Aliases);

internal sealed record CinemaSeed(
    string CodiceLocale,
    string Nome,
    string Citta,
    string Indirizzo,
    double Latitudine,
    double Longitudine,
    string Telefono,
    int NumeroSale,
    bool HasXl,
    bool HasIsense);

internal static class SeedCatalog
{
    public static IReadOnlyList<MovieTarget> MovieTargets { get; } =
    [
        new("Oppenheimer", 2023),
        new("Barbie", 2023),
        new("Dune: Part Two", 2024, "Dune - Parte Due"),
        new("Pulp Fiction", 1994),
        new("Interstellar", 2014),
        new("Inception", 2010),
        new("The Dark Knight", 2008),
        new("Tenet", 2020),
        new("Mad Max: Fury Road", 2015),
        new("The Matrix", 1999),
        new("Blade Runner 2049", 2017),
        new("Arrival", 2016),
        new("Alien", 1979),
        new("Get Out", 2017),
        new("The Shining", 1980),
        new("A Quiet Place", 2018),
        new("Spirited Away", 2001),
        new("Coco", 2017),
        new("Inside Out", 2015),
        new("Ratatouille", 2007),
        new("WALL-E", 2008),
        new("The Lord of the Rings: The Fellowship of the Ring", 2001),
        new("The Lord of the Rings: The Two Towers", 2002),
        new("The Lord of the Rings: The Return of the King", 2003),
        new("Harry Potter and the Prisoner of Azkaban", 2004),
        new("Pan's Labyrinth", 2006),
        new("Jurassic Park", 1993),
        new("Raiders of the Lost Ark", 1981),
        new("Gladiator", 2000),
        new("The Martian", 2015),
        new("La La Land", 2016),
        new("Titanic", 1997),
        new("Pride & Prejudice", 2005),
        new("Before Sunrise", 1995),
        new("Her", 2013),
        new("Casablanca", 1942),
        new("The Grand Budapest Hotel", 2014),
        new("Superbad", 2007),
        new("Knives Out", 2019),
        new("Parasite", 2019),
        new("Whiplash", 2014),
        new("The Social Network", 2010),
        new("The Godfather", 1972),
        new("Schindler's List", 1993),
        new("12 Years a Slave", 2013),
        new("1917", 2019),
        new("Dunkirk", 2017),
        new("Killers of the Flower Moon", 2023),
        new("Poor Things", 2023),
        new("Past Lives", 2023),
        new("Top Gun: Maverick", 2022),
        new("Avatar: The Way of Water", 2022),
        new("The Batman", 2022),
        new("Everything Everywhere All at Once", 2022),
        new("Apollo 11", 2019),
        new("Free Solo", 2018),
        new("March of the Penguins", 2005),
        new("Won't You Be My Neighbor?", 2018),
        new("Black Swan", 2010),
        new("Gone Girl", 2014),
        new("Se7en", 1995),
        new("The Silence of the Lambs", 1991),
        new("The Shape of Water", 2017),
        new("No Time to Die", 2021)
    ];

    public static IReadOnlyList<CinemaSeed> Cinemas { get; } =
    [
        new("CBR001", "CineBase Roma", "Roma", "Via Ostiense 131/L", 41.8566, 12.4798, "06 94851201", 5, true, true),
        new("CBM001", "CineBase Milano", "Milano", "Viale Sarca 228", 45.5141, 9.2139, "02 94751302", 5, true, true),
        new("CBN001", "CineBase Napoli", "Napoli", "Via Nuova Poggioreale 158", 40.8524, 14.2936, "081 19364010", 4, true, false),
        new("CBT001", "CineBase Torino Lingotto", "Torino", "Via Nizza 262", 45.0357, 7.6661, "011 19645001", 4, true, true),
        new("CBB001", "CineBase Bologna Navile", "Bologna", "Via Cristoforo Colombo 7", 44.5205, 11.3418, "051 19645002", 4, true, false),
        new("CBF001", "CineBase Firenze Novoli", "Firenze", "Via Forlanini 29", 43.8013, 11.2141, "055 19645003", 4, false, true),
        new("CBV001", "CineBase Mestre Laguna", "Venezia", "Via Don Tosatto 22", 45.4937, 12.2476, "041 19645004", 4, true, false),
        new("CBP001", "CineBase Palermo Forum", "Palermo", "Via Filippo Pecoraino 29", 38.0976, 13.3992, "091 19645005", 4, true, true),
        new("CBC001", "CineBase Catania Etna", "Catania", "Via Cristoforo Colombo 46", 37.5074, 15.0830, "095 19645006", 4, false, true),
        new("CBA001", "CineBase Bari Levante", "Bari", "Via Amendola 172/C", 41.1113, 16.8860, "080 19645007", 4, true, false),
        new("CBV002", "CineBase Verona Arena", "Verona", "Viale del Lavoro 47", 45.4279, 10.9932, "045 19645008", 4, false, true),
        new("CBG001", "CineBase Genova Porto", "Genova", "Via Milano 83R", 44.4162, 8.9105, "010 19645009", 4, true, false),
        new("CBP002", "CineBase Padova Est", "Padova", "Via Venezia 61", 45.4128, 11.9095, "049 19645010", 4, false, true),
        new("CBC002", "CineBase Cagliari Santa Gilla", "Cagliari", "Viale Monastir 128", 39.2389, 9.0985, "070 19645011", 3, true, false),
        new("CBL001", "CineBase Lecce Salento", "Lecce", "Via San Cesario 126", 40.3423, 18.1773, "0832 19645012", 3, false, false),
        new("CBP003", "CineBase Perugia Collestrada", "Perugia", "Via della Valtiera 181", 43.0968, 12.4380, "075 19645013", 3, true, false),
        new("CBP004", "CineBase Parma Ducale", "Parma", "Via Emilia Est 7/A", 44.8094, 10.3521, "0521 19645014", 3, false, true),
        new("CBT002", "CineBase Trieste Adriatico", "Trieste", "Via Flavia 23", 45.6341, 13.8019, "040 19645015", 3, true, false),
        new("CBB002", "CineBase Bergamo Orio", "Bergamo", "Via Portico 71", 45.6702, 9.7038, "035 19645016", 3, false, true),
        new("CBS001", "CineBase Salerno Arechi", "Salerno", "Via San Leonardo 120", 40.6521, 14.8044, "089 19645017", 3, true, false)
    ];

    public static IReadOnlyList<string> CategoriaNames { get; } =
    [
        "Drammatico",
        "Commedia",
        "Avventura",
        "Fantasy",
        "Horror",
        "Azione",
        "Fantascienza",
        "Thriller",
        "Animazione",
        "Documentario",
        "Romantico",
        "Storico"
    ];

    public static IReadOnlyDictionary<TipoSala, decimal> SupplementiBySala { get; } =
        new Dictionary<TipoSala, decimal>
        {
            [TipoSala.DueD] = 0m,
            [TipoSala.TreD] = 2.50m,
            [TipoSala.XL] = 3.50m,
            [TipoSala.ISENSE] = 4.50m
        };

    public static IReadOnlyDictionary<int, string> TmdbGenreToCategoria { get; } =
        new Dictionary<int, string>
        {
            [18] = "Drammatico",
            [35] = "Commedia",
            [12] = "Avventura",
            [14] = "Fantasy",
            [27] = "Horror",
            [28] = "Azione",
            [878] = "Fantascienza",
            [53] = "Thriller",
            [16] = "Animazione",
            [99] = "Documentario",
            [10749] = "Romantico",
            [36] = "Storico"
        };

    public static IReadOnlyList<int> FallbackDiscoverGenres { get; } =
    [28, 12, 16, 18, 35, 36, 14, 27, 53, 99, 10749, 878];
}
