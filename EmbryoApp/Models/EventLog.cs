namespace EmbryoApp.Models;

using System.ComponentModel.DataAnnotations;


public class EventLog
{
    [Key]
    public Guid EventLogId { get; set; }

    [MaxLength(100)]
    [Required]
    public string EventType { get; set; } = default!; // ex: "ModelViewed", "QuizStarted"

    [Required]
    public string Payload { get; set; } = default!; // JSON string (contenu libre)

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // FK optionnelle vers User
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
}
