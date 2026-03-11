namespace CognomeNomeAPI.DTO;

public class NaturalLanguageTaskDTO
{
    public string Text { get; set; } = null!;
    public int? CreatorId { get; set; }
    public int? TeamId { get; set; }
}
