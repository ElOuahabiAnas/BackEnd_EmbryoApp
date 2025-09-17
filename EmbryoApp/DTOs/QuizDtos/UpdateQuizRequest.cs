using System.ComponentModel.DataAnnotations;
using EmbryoApp.Models;

namespace EmbryoApp.DTOs.QuizDtos;

public sealed class UpdateQuizRequest
{
    [MaxLength(255)]
    public string? Title { get; set; } 
    [MaxLength(2000)] public string? Description { get; set; }
    public int?    TimeLimit   { get; set; }
    public int?    Attempts    { get; set; }
    public ModelStatus? Status { get; set; }
    public Guid?   ModelId     { get; set; }   // on peut rattacher/détacher
}