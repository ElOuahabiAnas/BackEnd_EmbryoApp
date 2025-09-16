using EmbryoApp.DTOs;
using EmbryoApp.DTOs.Model3D;

namespace EmbryoApp.Service.Interface;

public interface IModel3DService
{
    Task<Guid> CreateAsync(string authorUserId, CreateModel3DRequest req, CancellationToken ct);
    Task<Model3DResponse?> GetByIdAsync(Guid modelId, CancellationToken ct);
    Task<PagedResult<Model3DResponse>> ListAsync(Model3DListQuery query, CancellationToken ct);
    Task<Model3DResponse?> UpdateAsync(Guid modelId, UpdateModel3DRequest req, CancellationToken ct);
    Task<bool> DeleteAsync(Guid modelId, CancellationToken ct);
}