namespace EmbryoApp.DTOs.AttemptAnswerDtos;

public sealed class AttemptAnswerResponse
{
    public Guid AttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public string? Response { get; set; }
    public bool IsCorrect { get; set; }
}