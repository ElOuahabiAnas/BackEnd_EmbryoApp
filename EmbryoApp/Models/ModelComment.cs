namespace EmbryoApp.Models;

using System.ComponentModel.DataAnnotations;


public class ModelComment
{
    [Key]
    public Guid ModelCommentId { get; set; }

    // FK -> Model3D (ex: class Model3D { Guid ModelId; ... })
    public Guid ModelId { get; set; }
    public Model3D? Model { get; set; }

    // FK -> AspNetUsers
    [Required]
    public string UserId { get; set; } = default!;
    public ApplicationUser? User { get; set; }

    [Required, MaxLength(1000)]
    public string Content { get; set; } = default!;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
