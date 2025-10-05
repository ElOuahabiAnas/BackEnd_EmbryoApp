namespace EmbryoApp.DTOs.ModelRatingDtos;


public sealed class ModelRatingResponse
{
    public Guid ModelRatingId { get; set; }
    public Guid ModelId { get; set; }
    public string UserId { get; set; } = default!;
    public int Rating { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
