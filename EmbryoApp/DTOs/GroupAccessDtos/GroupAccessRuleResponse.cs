namespace EmbryoApp.DTOs.GroupAccessDtos;
using EmbryoApp.Models;


public sealed class GroupAccessRuleResponse
{
    public Guid RuleId { get; set; }
    public string GroupName { get; set; } = default!;
    public WeekDays WeekDays { get; set; }
    public bool IsAllowed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
