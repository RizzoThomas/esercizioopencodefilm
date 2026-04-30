namespace FilmAPI.Model;

public enum OrdineState
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
    Cancelled = 3,
    Expired = 4,
    CheckoutInProgress = 5
}