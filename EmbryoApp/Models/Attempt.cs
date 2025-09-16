namespace EmbryoApp.Models;

// Models/Attempt.cs
using System.ComponentModel.DataAnnotations;


public class Attempt
{
    [Key]
    public Guid AttemptId { get; set; }

    // Score sur 100 (2 décimales)
    public decimal Score { get; set; } // 0..100

    // Horodatage de la tentative (UTC)
    public DateTimeOffset AttemptedAt { get; set; } = DateTimeOffset.UtcNow;

    // Durée en secondes
    public int Duration { get; set; }

    // FK vers AspNetUsers
    [Required]
    public string UserId { get; set; } = default!;

    // FK vers Quiz
    [Required]
    public Guid QuizId { get; set; }

    // Navigations (optionnelles)
    public ApplicationUser? User { get; set; }
    public Quiz? Quiz { get; set; }
}
