using System.ComponentModel.DataAnnotations.Schema;

namespace CognomeNomeAPI.Model;

public class Film
{
    public int Id { get; set; }
    public string Titolo { get; set; } = null!;
    public DateTime DataProduzione { get; set; }
    public int RegistaId { get; set; }
    public int Durata { get; set; } // in minuti
    public Regista? Regista { get; set; }
}
