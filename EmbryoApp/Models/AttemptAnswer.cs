namespace EmbryoApp.Models;

// Models/AttemptAnswer.cs
using System.ComponentModel.DataAnnotations;


public class AttemptAnswer
{
    [Required]
    public Guid AttemptId { get; set; }

    [Required]
    public Guid QuestionId { get; set; }

    public string? Response { get; set; }

    [Required]
    public bool IsCorrect { get; set; }

    // Navigations
    public Attempt Attempt { get; set; } = default!;
    public Question Question { get; set; } = default!;
}
