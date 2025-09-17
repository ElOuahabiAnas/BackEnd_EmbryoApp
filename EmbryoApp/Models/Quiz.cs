namespace EmbryoApp.Models;

using System.ComponentModel.DataAnnotations;

public class Quiz
{
    [Key]
    public Guid QuizId { get; set; }
    
    [MaxLength(255)]
    [Required]
    public string Title { get; set; } = default!; 

    [MaxLength(2000)]
    public string? Description { get; set; }   // NB: j’ai corrigé "Descriptionn" -> "Description"

    public int? TimeLimit { get; set; }        // en minutes (null = pas de limite)
    public int? Attempts  { get; set; }        // null = illimité

    public ModelStatus Status { get; set; } = ModelStatus.Draft;  // Draft|Active|Closed
    public DateTimeOffset? PublishedAt { get; set; }

    // FK optionnelle vers Model3D
    public Guid? ModelId { get; set; }
    public Model3D? Model { get; set; }
}
