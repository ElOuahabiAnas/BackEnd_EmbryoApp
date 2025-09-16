namespace EmbryoApp.DTOs.ModelMedia;

public sealed class ModelMediaResponse
{
    public Guid   MediaId   { get; set; }
    public string Url       { get; set; } = default!;
    public string MediaType { get; set; } = default!;
    public string? Legende  { get; set; }
    public int?   Position  { get; set; }
    public bool   IsPrimary { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid   ModelId   { get; set; }
}