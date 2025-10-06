namespace EmbryoApp.Service.Interface;
using EmbryoApp.DTOs;
using EmbryoApp.DTOs.ModelCommentDtos;


public interface IModelCommentService
{
    Task<PagedResult<ModelCommentResponse>> ListAsync(Guid modelId, ModelCommentListQuery q, CancellationToken ct);
    Task<ModelCommentResponse?> CreateAsync(Guid modelId, string userId, CreateModelCommentRequest req, CancellationToken ct);
    Task<bool> DeleteAsync(Guid modelCommentId, string callerUserId, bool isProfessor, CancellationToken ct);
    Task<(ModelCommentResponse? Response, string? Error)> UpdateAsync(
        Guid modelCommentId,
        string callerUserId,
        UpdateModelCommentRequest req,
        CancellationToken ct);
    Task<(IReadOnlyList<ModelCommentResponse> Items, int Total, int Page, int PageSize)> ListMineAsync(
        string userId, MyCommentsQuery query, CancellationToken ct);

}
