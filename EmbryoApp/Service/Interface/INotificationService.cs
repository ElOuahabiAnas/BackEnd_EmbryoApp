using EmbryoApp.DTOs;
using EmbryoApp.DTOs.NotificationDtos;

namespace EmbryoApp.Service.Interface;

// Features/Notifications/INotificationService.cs


public interface INotificationService
{
    Task<PagedResult<NotificationResponse>> ListAsync(NotificationListQuery q, CancellationToken ct);
    Task<NotificationResponse?> GetByIdAsync(Guid id, CancellationToken ct);

    // création (ex: depuis un backoffice ou une action métier)
    Task<Guid> CreateAsync(CreateNotificationRequest req, CancellationToken ct);

    // marquer une notif comme lue (vérifie ownership)
    Task<bool> MarkReadAsync(Guid id, string callerUserId, bool isElevated, CancellationToken ct);

    // marquer toutes comme lues pour un user (caller = user lui-même, ou elevated)
    Task<int> MarkAllReadAsync(string targetUserId, string callerUserId, bool isElevated, CancellationToken ct);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct); // optionnel (élevé)
}
