namespace EmbryoApp.Models;

using System.ComponentModel.DataAnnotations;


public class ModelRating
{
    [Key]
    public Guid ModelRatingId { get; set; }

    // FK -> Model3D (assume classe Model3D { Guid ModelId; ... })
    public Guid ModelId { get; set; }
    public Model3D? Model { get; set; }

    // FK -> AspNetUsers
    [Required]
    public string UserId { get; set; } = default!;
    public ApplicationUser? User { get; set; }

    // 1..5
    [Range(1, 5)]
    public int Rating { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
