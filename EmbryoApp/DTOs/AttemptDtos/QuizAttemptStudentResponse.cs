namespace EmbryoApp.DTOs.AttemptDtos;

public sealed class QuizAttemptStudentResponse
{
    public string UserId { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public decimal LastScore { get; set; }
    public DateTimeOffset LastAttemptedAt { get; set; }
}
