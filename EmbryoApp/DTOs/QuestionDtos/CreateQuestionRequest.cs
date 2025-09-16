using System.ComponentModel.DataAnnotations;
using EmbryoApp.Models;

namespace EmbryoApp.DTOs.QuestionDtos;

public sealed class CreateQuestionRequest
{
    public QuestionType QuestionType { get; set; } = QuestionType.QCM;

    [Required, MaxLength(2000)]
    public string Statement { get; set; } = default!;

    // Pour QCM: au moins 2 options (ex: ["A","B","C","D"])
    public List<string>? Options { get; set; }

    // QCM: doit être une des Options; VraiFaux: "true" ou "false"; Redaction: optionnel
    public string? CorrectAnswer { get; set; }

    [Required]
    public Guid QuizId { get; set; }
}