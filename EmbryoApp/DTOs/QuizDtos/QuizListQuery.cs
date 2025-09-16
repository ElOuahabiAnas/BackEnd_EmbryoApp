using EmbryoApp.Models;

namespace EmbryoApp.DTOs.QuizDtos;

public sealed class QuizListQuery
{
    public Guid? ModelId { get; set; }
    public ModelStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}