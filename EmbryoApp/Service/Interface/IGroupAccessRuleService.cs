namespace EmbryoApp.Service.Interface;

using EmbryoApp.DTOs.GroupAccessDtos;


public interface IGroupAccessRuleService
{
    Task<Guid> CreateAsync(CreateGroupAccessRuleRequest req, CancellationToken ct);
    Task<bool> UpdateAsync(Guid id, UpdateGroupAccessRuleRequest req, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    Task<List<GroupAccessRuleResponse>> ListAsync(string? groupName, CancellationToken ct);
    Task<GroupAccessCheckResponse> CheckAccessAsync(string groupName, DateTimeOffset today, CancellationToken ct);

}
