
using System.ComponentModel.DataAnnotations;

namespace EmbryoApp.Models;

public enum QuestionType { QCM, VraiFaux, Redaction }

public class Question
{
    [Key]
    public Guid QuestionId { get; set; }

    [Required]
    public QuestionType QuestionType { get; set; } = QuestionType.QCM;

    [Required, MaxLength(2000)]
    public string Statement { get; set; } = default!;

    // Stocké en JSON (ex: ["A","B","C","D"]) — pertinent pour QCM
    public List<string>? Options { get; set; }

    // Pour QCM: doit appartenir à Options; pour VraiFaux: "true" ou "false"; pour Redaction: libre/peut être null
    public string? CorrectAnswer { get; set; }

    [Required]
    public Guid QuizId { get; set; }
    public Quiz Quiz { get; set; } = default!;
}
