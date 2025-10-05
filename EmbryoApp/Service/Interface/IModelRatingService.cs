namespace EmbryoApp.Service.Interface;

using EmbryoApp.DTOs.ModelRatingDtos;


public interface IModelRatingService
{
    Task<ModelRatingResponse> UpsertAsync(Guid modelId, string userId, int rating, CancellationToken ct);
    Task<bool> DeleteMyRatingAsync(Guid modelId, string userId, CancellationToken ct);
    Task<ModelRatingSummaryResponse> GetSummaryAsync(Guid modelId, string? currentUserId, CancellationToken ct);
    Task<int?> GetMyRatingAsync(Guid modelId, string userId, CancellationToken ct);
}
