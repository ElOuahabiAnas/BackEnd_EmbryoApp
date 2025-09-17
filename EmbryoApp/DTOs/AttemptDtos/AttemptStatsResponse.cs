namespace EmbryoApp.DTOs.AttemptDtos;

public sealed class AttemptStatsResponse
{
    public Guid QuizId { get; set; }
    public int AttemptCount { get; set; }
    public decimal AverageScore { get; set; }
}