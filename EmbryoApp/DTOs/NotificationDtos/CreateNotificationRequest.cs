using System.ComponentModel.DataAnnotations;

namespace EmbryoApp.DTOs.NotificationDtos;

public sealed class CreateNotificationRequest
{
    // ciblage d’un utilisateur (string Id d’Identity)
    [Required] public string UserId { get; set; } = default!;

    [Required, MaxLength(180)]  public string Title { get; set; } = default!;
    [Required, MaxLength(4000)] public string Body  { get; set; } = default!;
}