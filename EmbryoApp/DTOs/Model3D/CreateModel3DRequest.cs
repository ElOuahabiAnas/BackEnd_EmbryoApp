using System.ComponentModel.DataAnnotations;
using EmbryoApp.Models;

namespace EmbryoApp.DTOs.Model3D;

public sealed class CreateModel3DRequest
{
    [Required, MaxLength(150)] public string Title { get; set; } = default!;
    [MaxLength(100)]           public string? Discipline { get; set; }
    public int?    EmbryoDay { get; set; }
    public string? Description { get; set; }
    public ModelStatus Status { get; set; } = ModelStatus.Draft;
}