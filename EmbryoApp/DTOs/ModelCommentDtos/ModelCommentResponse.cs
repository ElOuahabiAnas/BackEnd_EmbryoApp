namespace EmbryoApp.DTOs.ModelCommentDtos;


public sealed class ModelCommentResponse
{
    public Guid ModelCommentId { get; set; }
    public Guid ModelId { get; set; }

    public string UserId { get; set; } = default!;
    public string? UserFirstName { get; set; }
    public string? UserLastName { get; set; }
    public string? UserEmail { get; set; }

    public string Content { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    
    public int?  UserRating    { get; set; }   // note 1..5 laissée par l’auteur du commentaire (ou null)
    public Guid? ModelRatingId { get; set; }   // id de la note (ou null)
}
