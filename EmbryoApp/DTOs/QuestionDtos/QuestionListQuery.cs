using System.ComponentModel.DataAnnotations;

namespace EmbryoApp.DTOs.QuestionDtos;
public sealed class QuestionListQuery
{
    [Required] public Guid QuizId { get; set; }
    public int Page     { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}