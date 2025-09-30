namespace EmbryoApp.Service.Implementation;

using EmbryoApp.DTOs.GroupAccessDtos;
using EmbryoApp.Models;
using EmbryoApp.Service.Interface;
using EmbryoApp.Data;
using Microsoft.EntityFrameworkCore;


public sealed class GroupAccessRuleService : IGroupAccessRuleService
{
    private readonly AuthDbContext _db;
    public GroupAccessRuleService(AuthDbContext db) => _db = db;

    public async Task<Guid> CreateAsync(CreateGroupAccessRuleRequest req, CancellationToken ct)
    {
        var entity = new GroupAccessRule
        {
            RuleId = Guid.NewGuid(),
            GroupName = req.GroupName.Trim(),
            WeekDays = req.WeekDays,
            IsAllowed = req.IsAllowed,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.RuleId;
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateGroupAccessRuleRequest req, CancellationToken ct)
    {
        var rule = await _db.Set<GroupAccessRule>().FirstOrDefaultAsync(r => r.RuleId == id, ct);
        if (rule is null) return false;

        if (req.WeekDays.HasValue) rule.WeekDays = req.WeekDays.Value;
        if (req.IsAllowed.HasValue) rule.IsAllowed = req.IsAllowed.Value;

        rule.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var rule = await _db.Set<GroupAccessRule>().FirstOrDefaultAsync(r => r.RuleId == id, ct);
        if (rule is null) return false;

        _db.Remove(rule);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<GroupAccessRuleResponse>> ListAsync(string? groupName, CancellationToken ct)
    {
        var query = _db.Set<GroupAccessRule>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(groupName))
            query = query.Where(r => r.GroupName == groupName);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new GroupAccessRuleResponse
            {
                RuleId = r.RuleId,
                GroupName = r.GroupName,
                WeekDays = r.WeekDays,
                IsAllowed = r.IsAllowed,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync(ct);
        
    }
    
    public async Task<GroupAccessCheckResponse> CheckAccessAsync(string groupName, DateTimeOffset today, CancellationToken ct)
{
    var todayFlag = today.DayOfWeek switch
    {
        DayOfWeek.Monday    => WeekDays.Monday,
        DayOfWeek.Tuesday   => WeekDays.Tuesday,
        DayOfWeek.Wednesday => WeekDays.Wednesday,
        DayOfWeek.Thursday  => WeekDays.Thursday,
        DayOfWeek.Friday    => WeekDays.Friday,
        DayOfWeek.Saturday  => WeekDays.Saturday,
        DayOfWeek.Sunday    => WeekDays.Sunday,
        _ => WeekDays.None
    };

    var rules = await _db.Set<GroupAccessRule>()
        .AsNoTracking()
        .Where(r => r.GroupName == groupName)
        .ToListAsync(ct);

    // Par défaut = false s’il n’y a aucune règle
    if (!rules.Any())
    {
        return new GroupAccessCheckResponse
        {
            HasAccess = false,
            Message = "Aucune règle n’existe pour ton groupe. Accès refusé.",
            AllowedDays = new List<string>()
        };
    }

    // Vérifie si une règle couvre aujourd’hui
    var todayRules = rules.Where(r => (r.WeekDays & todayFlag) != 0).ToList();
    if (!todayRules.Any())
    {
        return new GroupAccessCheckResponse
        {
            HasAccess = false,
            Message = "Tu n’as pas le droit d’entrer aujourd’hui.",
            AllowedDays = ExtractAllowedDays(rules)
        };
    }

    // Si au moins une règle today existe mais est IsAllowed=false → bloqué
    if (todayRules.Any(r => r.IsAllowed == false))
    {
        return new GroupAccessCheckResponse
        {
            HasAccess = false,
            Message = "Tu n’as pas le droit d’entrer aujourd’hui.",
            AllowedDays = ExtractAllowedDays(rules)
        };
    }

    // Sinon → autorisé
    return new GroupAccessCheckResponse
    {
        HasAccess = true,
        Message = "Tu as accès aujourd’hui.",
        AllowedDays = ExtractAllowedDays(rules)
    };
}

private List<string> ExtractAllowedDays(List<GroupAccessRule> rules)
{
    var allowedFlags = rules.Where(r => r.IsAllowed).Select(r => r.WeekDays).ToList();
    var combined = allowedFlags.Aggregate(WeekDays.None, (acc, val) => acc | val);

    var result = new List<string>();
    if ((combined & WeekDays.Monday) != 0) result.Add("Lundi");
    if ((combined & WeekDays.Tuesday) != 0) result.Add("Mardi");
    if ((combined & WeekDays.Wednesday) != 0) result.Add("Mercredi");
    if ((combined & WeekDays.Thursday) != 0) result.Add("Jeudi");
    if ((combined & WeekDays.Friday) != 0) result.Add("Vendredi");
    if ((combined & WeekDays.Saturday) != 0) result.Add("Samedi");
    if ((combined & WeekDays.Sunday) != 0) result.Add("Dimanche");

    return result;
}



}
