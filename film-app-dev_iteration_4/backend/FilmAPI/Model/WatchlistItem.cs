using System.ComponentModel.DataAnnotations;

namespace FilmAPI.Model;

/// <summary>
/// Elemento della watchlist utente nella piattaforma CineBase.
/// È usato dal catalogo per salvare i film preferiti o da seguire e mappa la tabella di relazione utente-film.
/// </summary>
public class WatchlistItem
{
    /// <summary>Identificativo univoco dell'elemento watchlist.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Utente proprietario della watchlist; chiave esterna obbligatoria.</summary>
    public int UserId { get; set; }

    /// <summary>Film salvato nella watchlist; chiave esterna obbligatoria.</summary>
    public int FilmId { get; set; }

    /// <summary>Data/ora UTC di aggiunta del film alla watchlist.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Relazione con l'utente proprietario.</summary>
    public User User { get; set; } = null!;

    /// <summary>Relazione con il film salvato in watchlist.</summary>
    public Film Film { get; set; } = null!;
}
