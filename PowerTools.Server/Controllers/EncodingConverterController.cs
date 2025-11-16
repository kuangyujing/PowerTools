using Microsoft.AspNetCore.Mvc;
using PowerTools.Server.Models;
using PowerTools.Server.Services;

namespace PowerTools.Server.Controllers;

/// <summary>
/// Controller for file encoding conversion operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EncodingConverterController : ControllerBase
{
    private readonly EncodingDetectionService _encodingService;
    private readonly ILogger<EncodingConverterController> _logger;

    public EncodingConverterController(
        EncodingDetectionService encodingService,
        ILogger<EncodingConverterController> logger)
    {
        _encodingService = encodingService;
        _logger = logger;
    }

    /// <summary>
    /// Convert file encoding from one encoding to another
    /// </summary>
    /// <param name="request">Encoding conversion request with Base64 file content</param>
    /// <returns>Converted file content in Base64 format</returns>
    /// <response code="200">Returns the converted file content</response>
    /// <response code="400">If the request is invalid or encoding is not supported</response>
    /// <response code="500">If an error occurs during conversion</response>
    [HttpPost("convert")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(EncodingConversionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult ConvertEncoding([FromBody] EncodingConversionRequest request)
    {
        try
        {
            _logger.LogInformation("Converting file: {FileName}, Output: {OutputEncoding}, Input: {InputEncoding}",
                request.FileName, request.OutputEncoding, request.InputEncoding ?? "auto-detect");

            // Validate Base64 and decode
            byte[] fileBytes;
            try
            {
                fileBytes = Convert.FromBase64String(request.FileContentBase64);
            }
            catch (FormatException)
            {
                return BadRequest(new { error = "Invalid Base64 file content" });
            }

            if (fileBytes.Length == 0)
            {
                return BadRequest(new { error = "File content cannot be empty" });
            }

            // Check for binary files (contains too many null bytes)
            if (IsBinaryFile(fileBytes))
            {
                return BadRequest(new { error = "Binary files are not supported. Only text files can be converted." });
            }

            // Perform conversion
            var result = _encodingService.Convert(fileBytes, request.InputEncoding, request.OutputEncoding);

            var response = new EncodingConversionResponse
            {
                FileContentBase64 = Convert.ToBase64String(result.ConvertedBytes),
                FileName = request.FileName,
                DetectedEncoding = result.DetectedEncoding,
                OutputEncoding = result.OutputEncoding,
                FileSizeBytes = result.ConvertedBytes.Length,
                DetectionConfidence = result.DetectionConfidence
            };

            _logger.LogInformation("Conversion successful: {InputEncoding} -> {OutputEncoding}, Size: {Size} bytes",
                result.DetectedEncoding, result.OutputEncoding, result.ConvertedBytes.Length);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in conversion request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting file encoding");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred during file conversion", details = ex.Message });
        }
    }

    /// <summary>
    /// Get list of supported encodings
    /// </summary>
    /// <returns>List of supported encoding names</returns>
    [HttpGet("encodings")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(EncodingListResponse), StatusCodes.Status200OK)]
    public IActionResult GetSupportedEncodings()
    {
        var commonEncodings = new[]
        {
            new { name = "UTF-8", displayName = "UTF-8" },
            new { name = "UTF-16", displayName = "UTF-16 (Little Endian)" },
            new { name = "UTF-16BE", displayName = "UTF-16 (Big Endian)" },
            new { name = "UTF-32", displayName = "UTF-32" },
            new { name = "Shift_JIS", displayName = "Shift_JIS (Japanese)" },
            new { name = "EUC-JP", displayName = "EUC-JP (Japanese)" },
            new { name = "ISO-2022-JP", displayName = "ISO-2022-JP (Japanese)" },
            new { name = "GB2312", displayName = "GB2312 (Simplified Chinese)" },
            new { name = "Big5", displayName = "Big5 (Traditional Chinese)" },
            new { name = "EUC-KR", displayName = "EUC-KR (Korean)" },
            new { name = "ISO-8859-1", displayName = "ISO-8859-1 (Latin-1)" },
            new { name = "Windows-1252", displayName = "Windows-1252 (Western European)" }
        };

        var response = new EncodingListResponse
        {
            Encodings = commonEncodings
        };

        return Ok(response);
    }

    /// <summary>
    /// Check if file contains too many null bytes (likely binary)
    /// </summary>
    private bool IsBinaryFile(byte[] bytes)
    {
        int sampleSize = Math.Min(8192, bytes.Length);
        int nullCount = 0;

        for (int i = 0; i < sampleSize; i++)
        {
            if (bytes[i] == 0)
                nullCount++;
        }

        // If more than 5% are null bytes, consider it binary
        return (double)nullCount / sampleSize > 0.05;
    }
}

/// <summary>
/// Response containing list of supported encodings
/// </summary>
public class EncodingListResponse
{
    public object[] Encodings { get; set; } = Array.Empty<object>();
}
