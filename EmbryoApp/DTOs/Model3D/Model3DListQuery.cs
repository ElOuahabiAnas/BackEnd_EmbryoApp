using EmbryoApp.Models;

namespace EmbryoApp.DTOs.Model3D;

public sealed class Model3DListQuery
{
    public string?      Search       { get; set; }    // search on Title/Discipline
    public ModelStatus? Status       { get; set; }
    public string?      AuthorUserId { get; set; }
    public int          Page         { get; set; } = 1;
    public int          PageSize     { get; set; } = 20;
}