using System.Security.Claims;
using ContosoDashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ContosoDashboard.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentController : ControllerBase
{
    private const long MaximumFileSize = 25L * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string[]> SupportedFileTypes =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ["application/pdf"],
            [".doc"] = ["application/msword"],
            [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
            [".xls"] = ["application/vnd.ms-excel"],
            [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
            [".ppt"] = ["application/vnd.ms-powerpoint"],
            [".pptx"] = ["application/vnd.openxmlformats-officedocument.presentationml.presentation"],
            [".txt"] = ["text/plain"],
            [".jpg"] = ["image/jpeg"],
            [".jpeg"] = ["image/jpeg"],
            [".png"] = ["image/png"]
        };

    private static readonly HashSet<string> SupportedCategories = new(StringComparer.Ordinal)
    {
        "Project Documents",
        "Team Resources",
        "Personal Files",
        "Reports",
        "Presentations",
        "Other"
    };

    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentController> _logger;

    public DocumentController(IDocumentService documentService, ILogger<DocumentController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumFileSize)]
    [ProducesResponseType(typeof(DocumentUploadResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<IActionResult> Upload([FromForm] DocumentUploadForm form, CancellationToken cancellationToken)
    {
        if (!TryGetRequestingUserId(out var requestingUserId))
        {
            return Unauthorized();
        }

        if (form.File is null || form.File.Length == 0)
        {
            return BadRequest(new { error = "A non-empty file is required." });
        }

        if (form.File.Length > MaximumFileSize)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new
            {
                error = "The file exceeds the 25 MB limit."
            });
        }

        if (string.IsNullOrWhiteSpace(form.Title))
        {
            return BadRequest(new { error = "A document title is required." });
        }

        if (!SupportedCategories.Contains(form.Category ?? string.Empty))
        {
            return BadRequest(new { error = "A valid document category is required." });
        }

        if (!IsSupportedFileType(form.File, out var normalizedContentType))
        {
            return StatusCode(StatusCodes.Status415UnsupportedMediaType, new
            {
                error = "The file type is not supported. Use PDF, Word, Excel, PowerPoint, text, JPEG, or PNG."
            });
        }

        var metadata = new DocumentUploadMetadata(
            form.Title.Trim(),
            string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim(),
            form.Category!,
            form.ProjectId,
            ParseTags(form.Tags),
            Path.GetFileName(form.File.FileName),
            normalizedContentType,
            form.File.Length,
            form.TaskId);

        try
        {
            await using var content = form.File.OpenReadStream();
            var result = await _documentService.UploadAsync(
                requestingUserId,
                metadata,
                content,
                cancellationToken);

            return Created($"/api/documents/{result.DocumentId}", result);
        }
        catch (DocumentUploadException exception) when (exception.Code == DocumentUploadErrorCode.FileTooLarge)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new { error = exception.Message });
        }
        catch (DocumentUploadException exception) when (exception.Code == DocumentUploadErrorCode.UnsupportedFileType)
        {
            return StatusCode(StatusCodes.Status415UnsupportedMediaType, new { error = exception.Message });
        }
        catch (DocumentUploadException exception) when (exception.Code is DocumentUploadErrorCode.MalwareDetected or DocumentUploadErrorCode.ScanUnavailable)
        {
            return UnprocessableEntity(new { error = exception.Message });
        }
        catch (DocumentUploadException exception) when (exception.Code == DocumentUploadErrorCode.Unauthorized)
        {
            return Forbid();
        }
        catch (DocumentUploadException exception) when (exception.Code == DocumentUploadErrorCode.InvalidRequest)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (DocumentUploadException exception)
        {
            _logger.LogError(exception, "Document upload failed for user {UserId}.", requestingUserId);
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Document upload failed.");
        }
    }

    private bool TryGetRequestingUserId(out int userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out userId);
    }

    private static bool IsSupportedFileType(IFormFile file, out string contentType)
    {
        var extension = Path.GetExtension(file.FileName);
        if (!SupportedFileTypes.TryGetValue(extension, out var allowedContentTypes))
        {
            contentType = string.Empty;
            return false;
        }

        contentType = file.ContentType.Trim();
        return allowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyCollection<string> ParseTags(string? tags)
    {
        return (tags ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

public sealed class DocumentUploadForm
{
    [FromForm(Name = "file")]
    public IFormFile? File { get; set; }

    [FromForm(Name = "title")]
    public string? Title { get; set; }

    [FromForm(Name = "description")]
    public string? Description { get; set; }

    [FromForm(Name = "category")]
    public string? Category { get; set; }

    [FromForm(Name = "projectId")]
    public int? ProjectId { get; set; }

    [FromForm(Name = "taskId")]
    public int? TaskId { get; set; }

    [FromForm(Name = "tags")]
    public string? Tags { get; set; }
}
