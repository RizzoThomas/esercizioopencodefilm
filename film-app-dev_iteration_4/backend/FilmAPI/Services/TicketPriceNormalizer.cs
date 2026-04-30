namespace FilmAPI.Services;

internal static class TicketPriceNormalizer
{
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
