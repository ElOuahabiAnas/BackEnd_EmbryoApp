namespace EmbryoApp.DTOs.GroupAccessDtos;

using System.ComponentModel.DataAnnotations;
using EmbryoApp.Models;


public sealed class CreateGroupAccessRuleRequest
{
    [Required, MaxLength(100)]
    public string GroupName { get; set; } = default!;

    [Required]
    public WeekDays WeekDays { get; set; }

    public bool IsAllowed { get; set; } = true;
}
