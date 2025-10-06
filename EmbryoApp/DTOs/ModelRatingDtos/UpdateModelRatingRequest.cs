namespace EmbryoApp.DTOs.ModelRatingDtos;

using System.ComponentModel.DataAnnotations;

public sealed class UpdateModelRatingRequest
{
    [Range(1,5)]
    public int Rating { get; set; }
}
