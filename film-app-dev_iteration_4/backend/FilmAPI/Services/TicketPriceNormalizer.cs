namespace FilmAPI.Services;

internal static class TicketPriceNormalizer
{
    /// <summary>
    /// Esegue l''operazione NormalizeUnitPrice del servizio.
    /// </summary>
    /// <param name="amount">Parametro necessario per l'operazione: amount.</param>
    /// <returns>Restituisce il risultato dell'operazione quando questa ha esito positivo; altrimenti il chiamante riceve un'eccezione o un risultato nullo/booleano secondo il contratto del metodo.</returns>
    /// <remarks>
    /// Effetti collaterali: non introduce effetti collaterali esterni evidenti oltre alla logica di lettura o validazione.
    /// </remarks>
    public static decimal NormalizeUnitPrice(decimal amount)
    {
        // Guard rail for environments where "8.50" was parsed with an Italian culture
        // as 850 instead of 8.50 during seed/import. Cinema ticket prices above 50 EUR
        // per seat are considered invalid for this application domain.
        if (amount > 50m && amount <= 5000m)
        {
            return amount / 100m;
        }

        return amount;
    }

    /// <summary>
    /// Esegue l''operazione NormalizeTotal del servizio.
    /// </summary>
    /// <param name="amount">Parametro necessario per l'operazione: amount.</param>
    /// <param name="seatCount">Parametro necessario per l'operazione: seatCount.</param>
    /// <returns>Restituisce il risultato dell'operazione quando questa ha esito positivo; altrimenti il chiamante riceve un'eccezione o un risultato nullo/booleano secondo il contratto del metodo.</returns>
    /// <remarks>
    /// Effetti collaterali: non introduce effetti collaterali esterni evidenti oltre alla logica di lettura o validazione.
    /// </remarks>
    public static decimal NormalizeTotal(decimal amount, int seatCount)
    {
        if (seatCount <= 0)
        {
            return NormalizeUnitPrice(amount);
        }

        var unitAmount = amount / seatCount;
        if (unitAmount > 50m && unitAmount <= 5000m)
        {
            return amount / 100m;
        }

        return amount;
    }
}
