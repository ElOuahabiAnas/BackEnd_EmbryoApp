// Models/Model3D.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmbryoApp.Models;

public class Model3D
{
    [Key]
    public Guid ModelId { get; set; }

    [Required, MaxLength(150)]
    public string Title { get; set; } = default!;

    [MaxLength(100)]
    public string? Discipline { get; set; }

    public int? EmbryoDay { get; set; }

    public string? Description { get; set; }

    public ModelStatus Status { get; set; } = ModelStatus.Draft;

    public DateTimeOffset? PublishedAt { get; set; }

    // FK -> AspNetUsers (ApplicationUser)
    [Required]
    public string AuthorUserId { get; set; } = default!;
    public ApplicationUser Author { get; set; } = default!; // nav
}