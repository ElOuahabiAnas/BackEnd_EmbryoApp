namespace EmbryoApp.DTOs.AuthDtos;

using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;


public sealed class ImportStudentsRequest
{
    [Required]
    public IFormFile File { get; set; } = default!;
}
