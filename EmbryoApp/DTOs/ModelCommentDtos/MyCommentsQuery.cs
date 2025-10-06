namespace EmbryoApp.DTOs.ModelCommentDtos;


public sealed class MyCommentsQuery
{
    public Guid? ModelId  { get; set; }
    public string? Q      { get; set; }   // recherche texte dans Content
    public int? Page      { get; set; }   // optionnel
    public int? PageSize  { get; set; }   // optionnel
}
