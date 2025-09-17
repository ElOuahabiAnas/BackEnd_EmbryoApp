using EmbryoApp.DTOs;
using EmbryoApp.DTOs.EventLogDtos;

namespace EmbryoApp.Service.Interface;


public interface IEventLogService
{
    Task<PagedResult<EventLogResponse>> ListAsync(EventLogListQuery q, CancellationToken ct);
    Task<EventLogResponse?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Guid> CreateAsync(string? userId, CreateEventLogRequest req, CancellationToken ct);
}
