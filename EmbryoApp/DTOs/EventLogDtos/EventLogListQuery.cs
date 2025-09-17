namespace EmbryoApp.DTOs.EventLogDtos;


public sealed class EventLogListQuery
{
    public string? UserId { get; set; }
    public string? EventType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
