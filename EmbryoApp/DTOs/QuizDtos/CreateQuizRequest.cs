using System.ComponentModel.DataAnnotations;
using EmbryoApp.Models;

namespace EmbryoApp.DTOs.QuizDtos;

public sealed class CreateQuizRequest
{
    [MaxLength(2000)] public string? Description { get; set; }
    public int?    TimeLimit   { get; set; }   // minutes
    public int?    Attempts    { get; set; }
    public ModelStatus Status  { get; set; } = ModelStatus.Draft;
    public Guid?   ModelId     { get; set; }   // optionnel
}