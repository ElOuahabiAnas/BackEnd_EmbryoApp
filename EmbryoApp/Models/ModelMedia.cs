// Models/ModelMedia.cs
using System.ComponentModel.DataAnnotations;

namespace EmbryoApp.Models;

public class ModelMedia
{
    [Key]
    public Guid MediaId { get; set; }

    [Required, MaxLength(1024)]
    public string Url { get; set; } = default!;

    public MediaType MediaType { get; set; } = MediaType.Photo;  // 'photo' / 'video'

    [MaxLength(300)]
    public string? Legende { get; set; }

    public int? Position { get; set; }

    public bool IsPrimary { get; set; } = false;                  // default false (DB aussi)

    public DateTimeOffset CreatedAt { get; set; }                 // default NOW() (DB)

    // FK -> Model3D
    [Required]
    public Guid ModelId { get; set; }
    public Model3D Model { get; set; } = default!;
}