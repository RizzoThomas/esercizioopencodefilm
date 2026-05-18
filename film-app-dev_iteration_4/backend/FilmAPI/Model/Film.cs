// ============================================================================
// Film.cs — ENTITÀ PRINCIPALE DEL DOMINIO CINEMATOGRAFICO
// ============================================================================
// Questa classe rappresenta un film nel database.
// Ogni proprietà pubblica corrisponde a una colonna nella tabella Films.
// Le annotation [Required], [MaxLength], [ForeignKey] definiscono i vincoli
// del database che Entity Framework Core tradurrà nello schema MySQL.
// ============================================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class Film
{
    // ─── CHIAVE PRIMARIA ──────────────────────────────────────────────────
    // [Key] indica che questo è l'ID primario (auto-increment in MySQL)
    [Key]
    public int Id { get; set; }

    // ─── CAMPI OBBLIGATORI ────────────────────────────────────────────────
    // [Required] → NOT NULL nel database
    // [MaxLength] → VARCHAR(200) in MySQL
    [Required]
    [MaxLength(200)]
    public string Titolo { get; set; } = string.Empty;   // Titolo del film

    [Required]
    public DateTime DataProduzione { get; set; }          // Anno di produzione

    // ─── RELAZIONE CON REGISTA ────────────────────────────────────────────
    // RegistaId è una Foreign Key verso la tabella Registi
    // Il ForeignKey attribute collega la proprietà di navigazione
    [Required]
    public int RegistaId { get; set; }                    // FK verso Regista

    [ForeignKey(nameof(RegistaId))]
    public Regista? Regista { get; set; }                 // Navigation property (lazy loading)

    [Required]
    public int Durata { get; set; }                       // Durata in minuti

    // ─── CAMPI OPZIONALI ──────────────────────────────────────────────────
    // I campi nullable (string?) diventano NULL in MySQL
    [MaxLength(500)]
    public string? CopertinaPath { get; set; }            // URL poster/immagine copertina

    [MaxLength(500)]
    public string? FilmatoPath { get; set; }              // URL trailer

    [MaxLength(2000)]
    public string? DescrizioneLunga { get; set; }         // Trama del film

    [MaxLength(2000)]
    public string? CastText { get; set; }                 // Cast principale (testo concatenato)

    public DateOnly? DataRilascio { get; set; }           // Data di uscita nelle sale

    // ─── CAMPI TMDB (The Movie Database) ───────────────────────────────────
    // Questi campi vengono popolati dal seeder FilmApiSeeder
    // che chiama l'API TMDB per importare dati reali
    public int? TmdbId { get; set; }                      // ID univoco su TMDB

    [MaxLength(50)]
    public string? ImdbId { get; set; }                   // ID su IMDb

    public double? VoteAverage { get; set; }              // Voto medio (0-10)
    public int? VoteCount { get; set; }                   // Numero voti
    public double? Popularity { get; set; }                // Popolarità TMDB

    [MaxLength(500)]
    public string? BackdropPath { get; set; }             // URL immagine di sfondo

    [MaxLength(10)]
    public string? OriginalLanguage { get; set; }          // Lingua originale (es. "en", "it")

    [MaxLength(500)]
    public string? Homepage { get; set; }                  // Sito web del film

    // ─── COLLECTION NAVIGATION PROPERTIES ─────────────────────────────────
    // Queste NON sono colonne del database, ma relazioni:
    //   ICollection<Proiezione> = FK Proiezione.FilmId punta a Film.Id
    //   ICollection<FilmCategoria> = tabella ponte per relazione N:M
    //   ICollection<Show> = tutte le programmazioni di questo film
    //
    // Entity Framework Core carica automaticamente queste collection
    // con Include() o Lazy Loading
    public ICollection<Proiezione> Proiezioni { get; set; } = new List<Proiezione>();    // Legacy
    public ICollection<FilmCategoria> FilmCategorie { get; set; } = new List<FilmCategoria>(); // Categorie
    public ICollection<Show> Shows { get; set; } = new List<Show>();  // Spettacoli programmati
}
