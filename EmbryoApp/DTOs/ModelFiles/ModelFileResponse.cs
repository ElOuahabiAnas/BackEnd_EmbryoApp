namespace EmbryoApp.DTOs.ModelFiles;

public sealed class ModelFileResponse
{
    public Guid   FileId   { get; set; }
    public string Path     { get; set; } = default!;
    public string? FileType{ get; set; }
    public string? FileRole{ get; set; }
    public bool   IsPrimary{ get; set; }
    public int?   Position { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid   ModelId  { get; set; }
}
