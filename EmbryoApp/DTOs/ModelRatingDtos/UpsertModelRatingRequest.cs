namespace EmbryoApp.DTOs.ModelRatingDtos;

using System.ComponentModel.DataAnnotations;


public sealed class UpsertModelRatingRequest
{
    [Range(1,5)]
    public int Rating { get; set; }
}
