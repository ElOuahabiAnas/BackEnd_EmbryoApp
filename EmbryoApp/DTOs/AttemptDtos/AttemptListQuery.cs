namespace EmbryoApp.DTOs.AttemptDtos;

public sealed class AttemptListQuery
{
    // si fourni, filtre par Quiz
    public Guid? QuizId { get; set; }

    // si fourni, filtre par User (prof); pour un student on force l'id du token
    public string? UserId { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}