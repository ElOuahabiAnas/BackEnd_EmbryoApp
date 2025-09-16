// Models/ModelFile.cs
using System.ComponentModel.DataAnnotations;

namespace EmbryoApp.Models;

public class ModelFile
{
    [Key]
    public Guid FileId { get; set; }

    [Required, MaxLength(1024)]
    public string Path { get; set; } = default!;

    [MaxLength(50)]
    public string? FileType { get; set; }        // ex: glb, fbx, jpg, pdf...

    [MaxLength(50)]
    public string? FileRole { get; set; }        // ex: model, texture, thumbnail...

    public bool IsPrimary { get; set; } = false; // default false (DB)

    public int? Position { get; set; }

    public DateTimeOffset CreatedAt { get; set; } // default NOW() (DB)

    // FK -> Model3D
    [Required]
    public Guid ModelId { get; set; }
    public Model3D Model { get; set; } = default!;
}