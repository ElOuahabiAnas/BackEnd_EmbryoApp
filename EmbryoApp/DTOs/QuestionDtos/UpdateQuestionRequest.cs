using System.ComponentModel.DataAnnotations;
using EmbryoApp.Models;

namespace EmbryoApp.DTOs.QuestionDtos;

public sealed class UpdateQuestionRequest
{
    public QuestionType? QuestionType { get; set; }
    [MaxLength(2000)]
    public string? Statement { get; set; }
    public List<string>? Options { get; set; }
    public string? CorrectAnswer { get; set; }
    public Guid? QuizId { get; set; } // déplacer vers un autre quiz si besoin
}