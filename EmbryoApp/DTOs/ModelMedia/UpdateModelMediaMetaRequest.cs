namespace EmbryoApp.DTOs.ModelMedia;

// Features/ModelMedia/Dto/UpdateModelMediaMetaRequest.cs

public sealed class UpdateModelMediaMetaRequest
{
    public string? Legende { get; set; }
    public int?    Position { get; set; }
    public bool?   IsPrimary { get; set; }
}
