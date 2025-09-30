namespace EmbryoApp.DTOs.GroupAccessDtos;

public sealed class GroupAccessCheckResponse
{
    public bool HasAccess { get; set; }
    public string Message { get; set; } = default!;
    public List<string> AllowedDays { get; set; } = new();
}
