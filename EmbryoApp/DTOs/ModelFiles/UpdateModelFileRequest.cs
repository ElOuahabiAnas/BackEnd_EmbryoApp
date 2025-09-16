using System.ComponentModel.DataAnnotations;

namespace EmbryoApp.DTOs.ModelFiles;

public sealed class UpdateModelFileRequest
{
    [Required, MaxLength(1024)] public string Path { get; set; } = default!;
    [MaxLength(50)]             public string? FileType { get; set; }
    [MaxLength(50)]             public string? FileRole { get; set; }
    public bool   IsPrimary { get; set; } = false;
    public int?   Position  { get; set; }
}