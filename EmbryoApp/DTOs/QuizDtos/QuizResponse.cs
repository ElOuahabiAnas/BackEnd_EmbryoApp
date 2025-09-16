namespace EmbryoApp.DTOs.QuizDtos;

public sealed class QuizResponse
{
    public Guid QuizId { get; set; }
    public string? Description { get; set; }
    public int? TimeLimit { get; set; }
    public int? Attempts { get; set; }
    public string Status { get; set; } = default!;
    public DateTimeOffset? PublishedAt { get; set; }
    public Guid? ModelId { get; set; }
}