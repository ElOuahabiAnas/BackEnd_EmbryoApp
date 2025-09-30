namespace EmbryoApp.Models;

using System.ComponentModel.DataAnnotations;


[Flags]
public enum WeekDays
{
    None      = 0,
    Monday    = 1 << 0,
    Tuesday   = 1 << 1,
    Wednesday = 1 << 2,
    Thursday  = 1 << 3,
    Friday    = 1 << 4,
    Saturday  = 1 << 5,
    Sunday    = 1 << 6,
    All = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday
}

public class GroupAccessRule
{
    [Key]
    public Guid RuleId { get; set; }

    [MaxLength(100)]
    public string GroupName { get; set; } = default!;

    public WeekDays WeekDays { get; set; }

    public bool IsAllowed { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
