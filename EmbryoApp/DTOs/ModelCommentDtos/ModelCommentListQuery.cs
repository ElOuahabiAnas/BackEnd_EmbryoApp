namespace EmbryoApp.DTOs.ModelCommentDtos;

public sealed class ModelCommentListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
