using System.ComponentModel.DataAnnotations;

namespace EmbryoApp.DTOs.ModelMedia;

public sealed class ModelMediaListQuery
{
    [Required] public Guid ModelId { get; set; }
    public int Page     { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}