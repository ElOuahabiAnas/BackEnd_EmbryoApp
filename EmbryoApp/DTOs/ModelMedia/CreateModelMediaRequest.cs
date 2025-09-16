using System.ComponentModel.DataAnnotations;
using EmbryoApp.Models;

namespace EmbryoApp.DTOs.ModelMedia;

public sealed class CreateModelMediaRequest
{
    [Required, Url, MaxLength(1024)] public string Url { get; set; } = default!;
    [Required]                        public MediaType MediaType { get; set; } = MediaType.Photo; // Photo | Video
    public string?  Legende   { get; set; }
    public int?     Position  { get; set; }
    public bool     IsPrimary { get; set; } = false;

    // FK parent
    [Required] public Guid ModelId { get; set; }
}