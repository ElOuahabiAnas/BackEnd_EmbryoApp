namespace EmbryoApp.DTOs.AttemptDtos;

public sealed class AttemptResponse
{
    public Guid AttemptId { get; set; }
    public decimal Score { get; set; }
    public DateTimeOffset AttemptedAt { get; set; }
    public int Duration { get; set; }
    public string UserId { get; set; } = default!;
    public Guid QuizId { get; set; }
}