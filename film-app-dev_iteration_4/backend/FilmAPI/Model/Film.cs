using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

/// <summary>
/// Film presente nel catalogo CineBase.
/// È usato dai servizi di programmazione, ricerca catalogo e acquisto biglietti e corrisponde alla tabella dei film.
/// </summary>
public class Film
{
    /// <summary>Identificativo univoco del film.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Titolo commerciale del film; massimo 200 caratteri.</summary>
    [Required]
    [MaxLength(200)]
    public string Titolo { get; set; } = string.Empty;

    /// <summary>Data di produzione del film usata per i dati anagrafici del catalogo.</summary>
    [Required]
    public DateTime DataProduzione { get; set; }

    /// <summary>Regista associato al film; chiave esterna obbligatoria.</summary>
    [Required]
    public int RegistaId { get; set; }

    /// <summary>Relazione con il regista del film.</summary>
    [ForeignKey(nameof(RegistaId))]
    public Regista? Regista { get; set; }

    /// <summary>Durata del film in minuti; obbligatoria per la programmazione delle proiezioni.</summary>
    [Required]
    public int Durata { get; set; }

    /// <summary>Percorso della copertina o poster; massimo 500 caratteri.</summary>
    [MaxLength(500)]
    public string? CopertinaPath { get; set; }

    /// <summary>Percorso del file video o media collegato; massimo 500 caratteri.</summary>
    [MaxLength(500)]
    public string? FilmatoPath { get; set; }

    /// <summary>Descrizione estesa del film usata nella scheda catalogo; massimo 2000 caratteri.</summary>
    [MaxLength(2000)]
    public string? DescrizioneLunga { get; set; }

    /// <summary>Testo cast o interpreti del film; massimo 2000 caratteri.</summary>
    [MaxLength(2000)]
    public string? CastText { get; set; }

    /// <summary>Data di rilascio pubblica del film; opzionale.</summary>
    public DateOnly? DataRilascio { get; set; }

    /// <summary>Associazioni ponte con le categorie del film.</summary>
    public ICollection<FilmCategoria> FilmCategorie { get; set; } = new List<FilmCategoria>();

    /// <summary>Proiezioni programmate per il film.</summary>
    public ICollection<Show> Shows { get; set; } = new List<Show>();
}
