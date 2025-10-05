namespace EmbryoApp.DTOs.ModelCommentDtos;
using System.ComponentModel.DataAnnotations;


public sealed class CreateModelCommentRequest
{
    [Required, MaxLength(1000)]
    public string Content { get; set; } = default!;
}
