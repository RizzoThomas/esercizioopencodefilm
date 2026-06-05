namespace FilmAPI.Model;

/// <summary>
/// Tipologia tecnica della sala del cinema nel dominio CineBase.
/// È usata dai servizi di pricing e programmazione e viene salvata nel database come enum numerico.
/// </summary>
public enum TipoSala
{
    /// <summary>Sala standard 2D.</summary>
    DueD = 0,
    /// <summary>Sala con proiezione tridimensionale 3D.</summary>
    TreD = 1,
    /// <summary>Sala premium iSense con esperienza immersiva.</summary>
    ISENSE = 2,
    /// <summary>Sala extra large con schermo o capienza maggiorati.</summary>
    XL = 3
}
