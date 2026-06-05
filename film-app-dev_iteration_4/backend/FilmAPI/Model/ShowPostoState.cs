namespace FilmAPI.Model;

/// <summary>
/// Stato logico del posto per una proiezione CineBase.
/// Serve ai servizi di ticketing per distinguere hold e vendita e viene salvato nel database come enum numerico.
/// </summary>
public enum ShowPostoState
{
    /// <summary>Posto bloccato temporaneamente.</summary>
    Hold = 0,
    /// <summary>Posto venduto definitivamente.</summary>
    Sold = 1
}
