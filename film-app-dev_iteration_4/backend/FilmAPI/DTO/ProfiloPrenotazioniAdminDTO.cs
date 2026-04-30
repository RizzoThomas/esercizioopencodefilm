using System.ComponentModel.DataAnnotations;

namespace FilmAPI.DTO;

public class ProfiloUpdateDTO
{
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Cognome { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Telefono { get; set; }
}

public class PrenotazioneCreateDTO
{
    [Required]
    public int ProiezioneId { get; set; }

    [Required]
    [Range(1, 100)]
    public int NumeroPosti { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}

public class PrenotazioneDTO
{
    public int Id { get; set; }
    public int ProiezioneId { get; set; }
    public string TitoloFilm { get; set; } = string.Empty;
    public string NomeCinema { get; set; } = string.Empty;
    public DateTime DataProiezione { get; set; }
    public DateTime OraProiezione { get; set; }
    public int NumeroPosti { get; set; }
    public string? Note { get; set; }
    public DateTime DataPrenotazione { get; set; }
}

public class UserAdminDTO
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Cognome { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Ruolo { get; set; } = string.Empty;
    public DateTime DataRegistrazione { get; set; }
    public decimal CreditoResiduo { get; set; }
}

public class UpdateRuoloDTO
{
    [Required]
    [RegularExpression("^(User|PowerUser|Admin)$")]
    public string NuovoRuolo { get; set; } = string.Empty;
}

public class UpdateCreditoDTO
{
    public decimal NuovoCredito { get; set; }
}
