namespace FilmAPI.DTO;

public class AcquistaOffertaRequest
{
    public int ShowId { get; set; }
    public string MetodoPagamento { get; set; } = "credito";
}

public class CreateOffertaCheckoutSessionRequestDTO
{
    public int ShowId { get; set; }
}
