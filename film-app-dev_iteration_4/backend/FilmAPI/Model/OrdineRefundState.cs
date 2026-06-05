namespace FilmAPI.Model;

/// <summary>Stato del rimborso di un ordine CineBase.</summary>
public enum OrdineRefundState
{
    /// <summary>Rimborso richiesto, in attesa di elaborazione.</summary>
    Pending = 0,

    /// <summary>Rimborso completato con successo.</summary>
    Completed = 1,

    /// <summary>Rimborso fallito.</summary>
    Failed = 2
}
