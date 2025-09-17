using EmbryoApp.DTOs;
using EmbryoApp.DTOs.AttemptDtos;

namespace EmbryoApp.Service.Interface;


public interface IAttemptService
{
    Task<PagedResult<AttemptResponse>> ListAsync(AttemptListQuery q, CancellationToken ct);
    Task<AttemptResponse?> GetByIdAsync(Guid attemptId, CancellationToken ct);
    Task<Guid> CreateAsync(string userId, CreateAttemptRequest req, CancellationToken ct);
    Task<bool> DeleteAsync(Guid attemptId, CancellationToken ct); // réservé prof si tu veux
    Task<List<AttemptStatsResponse>> GetUserStatsAsync(string userId, CancellationToken ct);
    Task<AttemptGlobalStatsResponse> GetUserGlobalStatsAsync(string userId, CancellationToken ct);

}
