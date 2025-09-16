using System.ComponentModel.DataAnnotations;

namespace EmbryoApp.DTOs.AttemptDtos;

public sealed class CreateAttemptRequest
{
    // UserId n'est PAS dans le body : on le prend du token
    [Required] public Guid QuizId { get; set; }

    [Range(0, 100)] public decimal Score { get; set; }

    [Range(1, int.MaxValue)] public int Duration { get; set; } // sec
}