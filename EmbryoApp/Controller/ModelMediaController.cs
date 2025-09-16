using EmbryoApp.DTOs;
using EmbryoApp.DTOs.ModelMedia;
using EmbryoApp.Models;
using EmbryoApp.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmbryoApp.Controller;

[ApiController]
public sealed class ModelMediaController : ControllerBase
{
    private readonly IModelMediaService _svc;
    public ModelMediaController(IModelMediaService svc) => _svc = svc;

    // LIST: médias d'un Model3D
    // GET /api/models/{modelId}/media?page=1&pageSize=20
    [HttpGet("api/models/{modelId:guid}/media")]
    [AllowAnonymous] // ou [Authorize]
    [ProducesResponseType(typeof(PagedResult<ModelMediaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ModelMediaResponse>>> ListByModel(
        Guid modelId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _svc.ListByModelAsync(
            new ModelMediaListQuery { ModelId = modelId, Page = page, PageSize = pageSize }, ct);
        return Ok(result);
    }

    
    // GET by id
    // GET /api/media/{mediaId}
    [HttpGet("api/media/{mediaId:guid}")]
    [AllowAnonymous] // ou [Authorize]
    [ProducesResponseType(typeof(ModelMediaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModelMediaResponse>> Get(Guid mediaId, CancellationToken ct)
    {
        var item = await _svc.GetByIdAsync(mediaId, ct);
        return item is null ? NotFound(new { error = "media_not_found", mediaId }) : Ok(item);
    }

    
    // CREATE (Professor)
    // POST /api/models/{modelId}/media
    [HttpPost("api/models/{modelId:guid}/media")]
    [Authorize(Roles = "Student,Professor")]
[RequestSizeLimit(200_000_000)] // 200 MB si besoin
[ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<ActionResult> Upload(
    Guid modelId,
    IFormFile file,                      // clé "file" dans multipart/form-data
    [FromForm] string? legende,
    [FromForm] bool isPrimary = false,
    [FromForm] int? position = null,
    [FromServices] IWebHostEnvironment env = null!,
    CancellationToken ct = default)
{
    if (file is null || file.Length == 0)
        return BadRequest(new { error = "file_required" });

    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    // extensions autorisées (ajoute/retire ce que tu veux)
    string[] photoExts = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    string[] videoExts = [".mp4", ".mov", ".avi", ".mkv", ".webm"];

    bool isPhoto = photoExts.Contains(ext);
    bool isVideo = videoExts.Contains(ext);
    if (!isPhoto && !isVideo)
        return BadRequest(new { error = "unsupported_extension", allowed = photoExts.Concat(videoExts) });

    // Assurer wwwroot
    var webRoot = env.WebRootPath;
    if (string.IsNullOrWhiteSpace(webRoot))
    {
        webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        Directory.CreateDirectory(webRoot);
    }

    // Dossier cible: wwwroot/uploads/media/{modelId}
    var targetDir = Path.Combine(webRoot, "uploads", "media", modelId.ToString());
    Directory.CreateDirectory(targetDir);

    var fileName = $"{Guid.NewGuid()}{ext}";
    var fullPath = Path.Combine(targetDir, fileName);

    await using (var stream = System.IO.File.Create(fullPath))
    {
        await file.CopyToAsync(stream, ct);
    }

    // URL relative servie par StaticFiles
    var relativeUrl = $"/uploads/media/{modelId}/{fileName}";

    var req = new CreateModelMediaRequest
    {
        ModelId   = modelId,
        Url       = relativeUrl,
        MediaType = isVideo ? MediaType.Video : MediaType.Photo, // enum de ton modèle
        Legende   = legende,
        Position  = position,
        IsPrimary = isPrimary
    };

    try
    {
        var id = await _svc.CreateAsync(req, ct);
        return CreatedAtAction(nameof(Get), new { mediaId = id }, new { id, url = relativeUrl });
    }
    catch (KeyNotFoundException)
    {
        return BadRequest(new { error = "model_not_found", modelId });
    }
}

    // UPDATE (Professor)
    // PUT /api/media/{mediaId}
    [HttpPut("api/media/{mediaId:guid}")]
    [Authorize(Roles = "Professor")]
    [ProducesResponseType(typeof(ModelMediaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModelMediaResponse>> UpdateMeta(
        Guid mediaId,
        [FromBody] UpdateModelMediaMetaRequest req,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var updated = await _svc.UpdateMetaAsync(mediaId, req, ct);
        return updated is null
            ? NotFound(new { error = "media_not_found", mediaId })
            : Ok(updated);
    }


    // DELETE (Professor)
    // DELETE /api/media/{mediaId}
    [HttpDelete("api/media/{mediaId:guid}")]
    [Authorize(Roles = "Student,Professor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid mediaId, CancellationToken ct)
    {
        var ok = await _svc.DeleteAsync(mediaId, ct);
        return ok ? NoContent() : NotFound(new { error = "media_not_found", mediaId });
    }
}