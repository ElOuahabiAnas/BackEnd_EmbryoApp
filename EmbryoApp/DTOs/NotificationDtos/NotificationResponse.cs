namespace EmbryoApp.DTOs.NotificationDtos;

public sealed class NotificationResponse
{
    public Guid NotificationId { get; set; }
    public string Title        { get; set; } = default!;
    public string Body         { get; set; } = default!;
    public DateTimeOffset SentAt { get; set; }
    public bool IsRead         { get; set; }
    public string UserId       { get; set; } = default!;
}