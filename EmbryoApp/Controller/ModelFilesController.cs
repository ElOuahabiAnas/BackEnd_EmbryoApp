using EmbryoApp.DTOs;
using EmbryoApp.DTOs.ModelFiles;
using EmbryoApp.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmbryoApp.Controller;

[ApiController]
public sealed class ModelFilesController : ControllerBase
{
    private readonly IModelFileService _svc;
    public ModelFilesController(IModelFileService svc) => _svc = svc;

    // LIST: fichiers d'un Model3D
    // e.g. GET /api/models/{modelId}/files?page=1&pageSize=20
    [HttpGet("api/models/{modelId:guid}/files")]
    [Authorize(Roles = "Student,Professor")]
    [ProducesResponseType(typeof(PagedResult<ModelFileResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ModelFileResponse>>> ListByModel(
        Guid modelId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _svc.ListByModelAsync(
            new ModelFileListQuery { ModelId = modelId, Page = page, PageSize = pageSize }, ct);
        return Ok(result);
    }

    // GET by id
    // e.g. GET /api/files/{fileId}
    [HttpGet("api/files/{fileId:guid}")]
    [Authorize(Roles = "Student,Professor")]
    [ProducesResponseType(typeof(ModelFileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModelFileResponse>> Get(Guid fileId, CancellationToken ct)
    {
        var item = await _svc.GetByIdAsync(fileId, ct);
        return item is null ? NotFound(new { error = "file_not_found", fileId }) : Ok(item);
    }

    // CREATE (Professor)
    // e.g. POST /api/models/{modelId}/files  (ModelId dans body doit matcher la route — on le force)
    [HttpPost("api/models/{modelId:guid}/files")]
    [Authorize(Roles = "Student,Professor")]
    [DisableRequestSizeLimit]
[ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<ActionResult> Upload(
    Guid modelId,
    IFormFile file,                              // <— le fichier dans multipart/form-data, clé "file"
    [FromForm] string? fileRole,
    [FromForm] bool isPrimary = false,
    [FromForm] int? position = null,
    [FromServices] IWebHostEnvironment env = null!,
    CancellationToken ct = default)
{
    if (file is null || file.Length == 0)
        return BadRequest(new { error = "file_required" });

    // Vérif extension
    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (ext is not (".glb" or ".fbx"))
        return BadRequest(new { error = "unsupported_extension", allowed = new[] { ".glb", ".fbx" } });

    // Racine webroot (wwwroot)
    var webRoot = env.WebRootPath;
    if (string.IsNullOrWhiteSpace(webRoot))
    {
        webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        Directory.CreateDirectory(webRoot);
    }

    // Dossier cible: wwwroot/uploads/models/{modelId}
    var targetDir = Path.Combine(webRoot, "uploads", "models", modelId.ToString());
    Directory.CreateDirectory(targetDir);

    // Nom de fichier sûr
    var safeFileName = $"{Guid.NewGuid()}{ext}";
    var fullPath = Path.Combine(targetDir, safeFileName);

    // Sauvegarde sur disque
    await using (var stream = System.IO.File.Create(fullPath))
    {
        await file.CopyToAsync(stream, ct);
    }

    // Chemin relatif à exposer par l’API (servi par StaticFiles)
    var relativePath = $"/uploads/models/{modelId}/{safeFileName}";

    // Appel du service pour persister la ligne ModelFiles
    var req = new CreateModelFileRequest
    {
        ModelId   = modelId,
        Path      = relativePath,                // <— stocké en base
        FileType  = ext.Trim('.'),               // "glb"|"fbx"
        FileRole  = fileRole,
        IsPrimary = isPrimary,
        Position  = position
    };

    try
    {
        var id = await _svc.CreateAsync(req, ct);
        return CreatedAtAction(nameof(Get), new { fileId = id }, new { id, path = relativePath });
    }
    catch (KeyNotFoundException)
    {
        // parent Model3D inexistant
        return BadRequest(new { error = "model_not_found", modelId });
    }
}

    // UPDATE (Professor)
    // e.g. PUT /api/files/{fileId}
    [HttpPut("api/files/{fileId:guid}")]
    [Authorize(Roles = "Student,Professor")]
    [ProducesResponseType(typeof(ModelFileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModelFileResponse>> UpdateMeta(
        Guid fileId,
        [FromBody] UpdateModelFileMetaRequest req,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var updated = await _svc.UpdateMetaAsync(fileId, req, ct);
        return updated is null
            ? NotFound(new { error = "file_not_found", fileId })
            : Ok(updated);
    }

    // DELETE (Professor)
    // e.g. DELETE /api/files/{fileId}
    [HttpDelete("api/files/{fileId:guid}")]
    [Authorize(Roles = "Student,Professor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid fileId, CancellationToken ct)
    {
        var ok = await _svc.DeleteAsync(fileId, ct);
        return ok ? NoContent() : NotFound(new { error = "file_not_found", fileId });
    }
}