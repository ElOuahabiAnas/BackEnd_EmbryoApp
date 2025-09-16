using EmbryoApp.DTOs;
using EmbryoApp.DTOs.ModelMedia;

namespace EmbryoApp.Service.Interface;

public interface IModelMediaService
{
    Task<ModelMediaResponse?> GetByIdAsync(Guid mediaId, CancellationToken ct);
    Task<PagedResult<ModelMediaResponse>> ListByModelAsync(ModelMediaListQuery q, CancellationToken ct);
    Task<Guid> CreateAsync(CreateModelMediaRequest req, CancellationToken ct);
    Task<ModelMediaResponse?> UpdateMetaAsync(Guid mediaId, UpdateModelMediaMetaRequest req, CancellationToken ct);
    Task<bool> DeleteAsync(Guid mediaId, CancellationToken ct);
}