namespace FilmAPI.Model;

/// <summary>
/// Provider di autenticazione esterna supportato da CineBase.
/// È usato dai servizi di login federato e viene salvato nel database come enum numerico.
/// </summary>
public enum ExternalLoginProvider
{
    /// <summary>Login tramite Google.</summary>
    Google = 0,
    /// <summary>Login tramite Microsoft.</summary>
    Microsoft = 1,
    /// <summary>Login tramite Facebook.</summary>
    Facebook = 2
}
