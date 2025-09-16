namespace EmbryoApp.DTOs.QuestionDtos;

public sealed class QuestionResponse
{
    public Guid QuestionId { get; set; }
    public string QuestionType { get; set; } = default!;
    public string Statement { get; set; } = default!;
    public List<string>? Options { get; set; }
    public string? CorrectAnswer { get; set; }
    public Guid QuizId { get; set; }
}