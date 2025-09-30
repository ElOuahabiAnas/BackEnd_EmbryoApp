namespace EmbryoApp.DTOs.GroupAccessDtos;

using EmbryoApp.Models;


public sealed class UpdateGroupAccessRuleRequest
{
    public WeekDays? WeekDays { get; set; }
    public bool? IsAllowed { get; set; }
}
