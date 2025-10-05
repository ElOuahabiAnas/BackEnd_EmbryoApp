namespace EmbryoApp.DTOs.ModelRatingDtos;


public sealed class ModelRatingSummaryResponse
{
    public Guid ModelId { get; set; }
    public double Average { get; set; }   // moyenne 1..5
    public int Count { get; set; }        // nb de notes
    public int? MyRating { get; set; }    // optionnel : note de l'utilisateur connecté si fourni
}
