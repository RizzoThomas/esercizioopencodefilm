using System.ComponentModel.DataAnnotations;

namespace FilmAPI.DTO;

/// <summary>DTO per aggiornare il profilo utente.</summary>
public class ProfiloUpdateDTO
{
    /// <summary>Nome; obbligatorio e limitato a 100 caratteri per il profilo.</summary>
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    /// <summary>Cognome; obbligatorio e limitato a 100 caratteri per il profilo.</summary>
    [Required]
    [MaxLength(100)]
    public string Cognome { get; set; } = string.Empty;

    /// <summary>Telefono opzionale limitato a 20 caratteri.</summary>
    [MaxLength(20)]
    public string? Telefono { get; set; }
}

/// <summary>DTO di creazione prenotazione; usato nelle API di booking.</summary>
public class PrenotazioneCreateDTO
{
    /// <summary>ID della proiezione da prenotare; obbligatorio.</summary>
    [Required]
    public int ProiezioneId { get; set; }

    /// <summary>Numero di posti richiesti; deve essere tra 1 e 100 per evitare richieste non valide.</summary>
    [Required]
    [Range(1, 100)]
    public int NumeroPosti { get; set; }

    /// <summary>Note opzionali limitate a 500 caratteri.</summary>
    [MaxLength(500)]
    public string? Note { get; set; }
}

/// <summary>DTO di una prenotazione mostrata al cliente o all'admin.</summary>
public class PrenotazioneDTO
{
    /// <summary>ID univoco della prenotazione.</summary>
    public int Id { get; set; }
    /// <summary>ID della proiezione.</summary>
    public int ProiezioneId { get; set; }
    /// <summary>Titolo del film.</summary>
    public string TitoloFilm { get; set; } = string.Empty;
    /// <summary>Nome del cinema.</summary>
    public string NomeCinema { get; set; } = string.Empty;
    /// <summary>Data della proiezione.</summary>
    public DateTime DataProiezione { get; set; }
    /// <summary>Ora della proiezione.</summary>
    public DateTime OraProiezione { get; set; }
    /// <summary>Numero di posti prenotati.</summary>
    public int NumeroPosti { get; set; }
    /// <summary>Note opzionali.</summary>
    public string? Note { get; set; }
    /// <summary>Data di prenotazione.</summary>
    public DateTime DataPrenotazione { get; set; }
}

/// <summary>DTO utente usato nell'amministrazione prenotazioni/profilo.</summary>
public class UserAdminDTO
{
    /// <summary>ID univoco dell'utente.</summary>
    public int Id { get; set; }
    /// <summary>Email dell'utente.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>Nome dell'utente.</summary>
    public string Nome { get; set; } = string.Empty;
    /// <summary>Cognome dell'utente.</summary>
    public string Cognome { get; set; } = string.Empty;
    /// <summary>Telefono opzionale.</summary>
    public string? Telefono { get; set; }
    /// <summary>Ruolo dell'utente.</summary>
    public string Ruolo { get; set; } = string.Empty;
    /// <summary>Data di registrazione.</summary>
    public DateTime DataRegistrazione { get; set; }
    /// <summary>Credito residuo.</summary>
    public decimal CreditoResiduo { get; set; }
}

/// <summary>DTO di aggiornamento ruolo utente.</summary>
public class UpdateRuoloDTO
{
    /// <summary>Nuovo ruolo; la regex limita i valori ammessi.</summary>
    [Required]
    [RegularExpression("^(User|PowerUser|Admin)$")]
    public string NuovoRuolo { get; set; } = string.Empty;
}

/// <summary>DTO di aggiornamento credito utente.</summary>
public class UpdateCreditoDTO
{
    /// <summary>Nuovo valore del credito.</summary>
    public decimal NuovoCredito { get; set; }
}
