using EmbryoApp.DTOs;
using EmbryoApp.DTOs.ModelFiles;

namespace EmbryoApp.Service.Interface;

public interface IModelFileService
{
    Task<ModelFileResponse?> GetByIdAsync(Guid fileId, CancellationToken ct);
    Task<PagedResult<ModelFileResponse>> ListByModelAsync(ModelFileListQuery q, CancellationToken ct);
    Task<Guid> CreateAsync(CreateModelFileRequest req, CancellationToken ct);
    Task<ModelFileResponse?> UpdateMetaAsync(Guid fileId, UpdateModelFileMetaRequest req, CancellationToken ct);
    Task<bool> DeleteAsync(Guid fileId, CancellationToken ct);
}