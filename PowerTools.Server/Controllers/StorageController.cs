using Microsoft.AspNetCore.Mvc;
using PowerTools.Server.Models;
using PowerTools.Server.Services;

namespace PowerTools.Server.Controllers;

/// <summary>
/// Controller for temporary file storage operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StorageController : ControllerBase
{
    private readonly ILogger<StorageController> _logger;
    private readonly StorageService _storageService;

    public StorageController(ILogger<StorageController> logger, StorageService storageService)
    {
        _logger = logger;
        _storageService = storageService;
    }

    /// <summary>
    /// Upload a file to temporary storage
    /// </summary>
    /// <param name="request">Request containing file content in Base64</param>
    /// <returns>Access key and file metadata</returns>
    [HttpPost("upload")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(StorageUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Upload([FromBody] StorageUploadRequest request)
    {
        try
        {
            _logger.LogInformation("Uploading file: FileName={FileName}, TtlMinutes={TtlMinutes}",
                request.FileName, request.TtlMinutes);

            if (string.IsNullOrWhiteSpace(request.FileContentBase64))
            {
                return BadRequest(new { error = "File content cannot be empty" });
            }

            byte[] fileContent;
            try
            {
                fileContent = Convert.FromBase64String(request.FileContentBase64);
            }
            catch (FormatException)
            {
                return BadRequest(new { error = "Invalid Base64 file content" });
            }

            if (fileContent.Length == 0)
            {
                return BadRequest(new { error = "File content cannot be empty" });
            }

            var metadata = _storageService.StoreFile(fileContent, request.FileName, request.TtlMinutes);

            var response = new StorageUploadResponse
            {
                AccessKey = metadata.AccessKey,
                FileSizeBytes = metadata.FileSizeBytes,
                FileName = metadata.FileName,
                ExpiresAtUtc = metadata.ExpiresAtUtc
            };

            _logger.LogInformation("File uploaded successfully: AccessKey={AccessKey}, Size={Size} bytes",
                metadata.AccessKey, metadata.FileSizeBytes);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in upload request");
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operation failed in upload request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while uploading the file", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieve a file from temporary storage
    /// </summary>
    /// <param name="request">Request containing the access key</param>
    /// <returns>File content and metadata</returns>
    [HttpPost("get")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(StorageGetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Get([FromBody] StorageGetRequest request)
    {
        try
        {
            _logger.LogInformation("Retrieving file: AccessKey={AccessKey}", request.AccessKey);

            if (string.IsNullOrWhiteSpace(request.AccessKey))
            {
                return BadRequest(new { error = "Access key cannot be empty" });
            }

            var result = _storageService.GetFile(request.AccessKey);

            if (result == null)
            {
                return NotFound(new { error = "File not found or has expired" });
            }

            var (content, metadata) = result.Value;

            var response = new StorageGetResponse
            {
                FileContentBase64 = Convert.ToBase64String(content),
                FileSizeBytes = metadata.FileSizeBytes,
                FileName = metadata.FileName,
                ExpiresAtUtc = metadata.ExpiresAtUtc
            };

            _logger.LogInformation("File retrieved successfully: AccessKey={AccessKey}", request.AccessKey);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in get request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving the file", details = ex.Message });
        }
    }

    /// <summary>
    /// Delete a file from temporary storage
    /// </summary>
    /// <param name="request">Request containing the access key</param>
    /// <returns>Deletion result</returns>
    [HttpPost("delete")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(StorageDeleteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Delete([FromBody] StorageDeleteRequest request)
    {
        try
        {
            _logger.LogInformation("Deleting file: AccessKey={AccessKey}", request.AccessKey);

            if (string.IsNullOrWhiteSpace(request.AccessKey))
            {
                return BadRequest(new { error = "Access key cannot be empty" });
            }

            var deleted = _storageService.DeleteFile(request.AccessKey);

            if (!deleted)
            {
                return NotFound(new { error = "File not found or has already been deleted" });
            }

            var response = new StorageDeleteResponse
            {
                Deleted = true,
                Message = "File successfully deleted"
            };

            _logger.LogInformation("File deleted successfully: AccessKey={AccessKey}", request.AccessKey);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in delete request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while deleting the file", details = ex.Message });
        }
    }
}
