using System.ComponentModel.DataAnnotations;
using EmbryoApp.Models;

namespace EmbryoApp.DTOs.ModelMedia;

public sealed class UpdateModelMediaRequest
{
    [Required, Url, MaxLength(1024)] public string Url { get; set; } = default!;
    [Required]                        public MediaType MediaType { get; set; } = MediaType.Photo;
    public string?  Legende   { get; set; }
    public int?     Position  { get; set; }
    public bool     IsPrimary { get; set; } = false;
}