namespace EmbryoApp.Models;

// Models/Notification.cs
using System.ComponentModel.DataAnnotations;


public class Notification
{
    [Key]
    public Guid NotificationId { get; set; }

    [Required, MaxLength(180)]
    public string Title { get; set; } = default!;

    [Required, MaxLength(4000)]
    public string Body { get; set; } = default!;

    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsRead { get; set; } = false;

    [Required]
    public string UserId { get; set; } = default!;           // FK -> AspNetUsers(Id)
    public ApplicationUser? User { get; set; }                // navigation
}
