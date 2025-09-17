namespace EmbryoApp.DTOs.EventLogDtos;


public sealed class EventLogResponse
{
    public Guid EventLogId { get; set; }
    public string EventType { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public string? UserId { get; set; }
}
