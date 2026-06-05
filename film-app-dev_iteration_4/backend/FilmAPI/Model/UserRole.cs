namespace FilmAPI.Model;

/// <summary>
/// Ruolo autorizzativo dell'utente nella piattaforma CineBase.
/// È usato dai servizi di autorizzazione e viene salvato nel database come enum.
/// </summary>
public enum UserRole
{
    /// <summary>Utente base con permessi di consultazione e acquisto.</summary>
    User = 0,
    /// <summary>Utente avanzato con permessi operativi estesi.</summary>
    PowerUser = 1,
    /// <summary>Amministratore della piattaforma.</summary>
    Admin = 2
}
