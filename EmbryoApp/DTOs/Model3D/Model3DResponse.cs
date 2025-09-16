namespace EmbryoApp.DTOs.Model3D;

public sealed class Model3DResponse
{
    public Guid         ModelId      { get; set; }
    public string       Title        { get; set; } = default!;
    public string?      Discipline   { get; set; }
    public int?         EmbryoDay    { get; set; }
    public string?      Description  { get; set; }
    public string       Status       { get; set; } = default!;
    public DateTimeOffset? PublishedAt { get; set; }
    public string       AuthorUserId { get; set; } = default!;
}