namespace EmbryoApp.DTOs.EventLogDtos;

using System.ComponentModel.DataAnnotations;

public sealed class CreateEventLogRequest
{
    [Required, MaxLength(100)]
    public string EventType { get; set; } = default!;

    [Required]
    public string Payload { get; set; } = default!;
}
