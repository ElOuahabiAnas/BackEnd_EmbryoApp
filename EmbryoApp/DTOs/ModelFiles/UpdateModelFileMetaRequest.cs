namespace EmbryoApp.DTOs.ModelFiles;

// Features/ModelFiles/Dto/UpdateModelFileMetaRequest.cs
using System.ComponentModel.DataAnnotations;

public sealed class UpdateModelFileMetaRequest
{
    // nullable => champ optionnel; on ne change que ce qui est fourni
    public string? FileRole { get; set; }
    public bool?  IsPrimary { get; set; }
    public int?   Position  { get; set; }
}
