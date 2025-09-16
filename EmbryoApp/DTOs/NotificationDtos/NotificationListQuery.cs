namespace EmbryoApp.DTOs.NotificationDtos;

public sealed class NotificationListQuery
{
    // pour filtrer côté prof/admin (ex: liste d’un user spécifique)
    public string? UserId { get; set; }

    // optionnel: ne renvoyer que non lues
    public bool? UnreadOnly { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}