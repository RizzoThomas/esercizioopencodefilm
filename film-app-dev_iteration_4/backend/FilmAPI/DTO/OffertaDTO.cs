namespace FilmAPI.DTO;

/// <summary>DTO di richiesta acquisto offerta; usato nelle API di checkout offerte.</summary>
public class AcquistaOffertaRequest
{
    /// <summary>ID dello show a cui applicare l'offerta.</summary>
    public int ShowId { get; set; }
    /// <summary>Metodo di pagamento scelto; default credito.</summary>
    public string MetodoPagamento { get; set; } = "credito";
}

/// <summary>DTO di richiesta checkout per acquistare un'offerta.</summary>
public class CreateOffertaCheckoutSessionRequestDTO
{
    /// <summary>ID dello show.</summary>
    public int ShowId { get; set; }
}
