using System.ComponentModel.DataAnnotations;

namespace EmbryoApp.DTOs.AttemptAnswerDtos;

public sealed class CreateAttemptAnswerRequest
{
    [Required] public Guid QuestionId { get; set; }
    public string? Response { get; set; }
    public bool IsCorrect { get; set; }
}