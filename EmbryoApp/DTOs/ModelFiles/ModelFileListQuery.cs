using System.ComponentModel.DataAnnotations;

namespace EmbryoApp.DTOs.ModelFiles;

public sealed class ModelFileListQuery
{
    [Required] public Guid ModelId { get; set; }
    public int Page     { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}