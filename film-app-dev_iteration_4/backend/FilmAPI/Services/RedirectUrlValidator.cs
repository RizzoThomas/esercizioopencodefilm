namespace FilmAPI.Services;

public static class RedirectUrlValidator
{
    /// <summary>
    /// Esegue l''operazione IsValidRedirectPath del servizio.
    /// </summary>
    /// <param name="path">Parametro necessario per l'operazione: path.</param>
    /// <returns>Restituisce il risultato dell'operazione quando questa ha esito positivo; altrimenti il chiamante riceve un'eccezione o un risultato nullo/booleano secondo il contratto del metodo.</returns>
    /// <remarks>
    /// Effetti collaterali: non introduce effetti collaterali esterni evidenti oltre alla logica di lettura o validazione.
    /// </remarks>
    public static bool IsValidRedirectPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (path.Contains("://") || path.StartsWith("//")) return false;
        return path.StartsWith('/');
    }

    /// <summary>
    /// Esegue l''operazione Sanitize del servizio.
    /// </summary>
    /// <param name="path">Parametro necessario per l'operazione: path.</param>
    /// <param name="fallback">Parametro necessario per l'operazione: fallback.</param>
    /// <returns>Restituisce il risultato dell'operazione quando questa ha esito positivo; altrimenti il chiamante riceve un'eccezione o un risultato nullo/booleano secondo il contratto del metodo.</returns>
    /// <remarks>
    /// Effetti collaterali: non introduce effetti collaterali esterni evidenti oltre alla logica di lettura o validazione.
    /// </remarks>
    public static string Sanitize(string? path, string fallback = "/index.html")
    {
        return IsValidRedirectPath(path) ? path! : fallback;
    }
}
