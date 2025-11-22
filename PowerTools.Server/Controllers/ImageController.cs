using Microsoft.AspNetCore.Mvc;
using PowerTools.Server.Models;
using SkiaSharp;

namespace PowerTools.Server.Controllers;

/// <summary>
/// Controller for image operations - resize JPEG and PNG images
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ImageController : ControllerBase
{
    private readonly ILogger<ImageController> _logger;

    public ImageController(ILogger<ImageController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Resize a JPEG or PNG image
    /// </summary>
    /// <param name="request">Request containing image and resize parameters</param>
    /// <returns>Resized image in Base64 format</returns>
    [HttpPost("resize")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ImageResizeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Resize([FromBody] ImageResizeRequest request)
    {
        try
        {
            _logger.LogInformation("Resizing image: Width={Width}, Height={Height}, MaintainAspectRatio={MaintainAspectRatio}",
                request.Width, request.Height, request.MaintainAspectRatio);

            // Validate request
            if (!request.Width.HasValue && !request.Height.HasValue)
            {
                return BadRequest(new { error = "At least one of Width or Height must be specified" });
            }

            if (request.Width.HasValue && request.Width.Value <= 0)
            {
                return BadRequest(new { error = "Width must be a positive integer" });
            }

            if (request.Height.HasValue && request.Height.Value <= 0)
            {
                return BadRequest(new { error = "Height must be a positive integer" });
            }

            // Decode the image
            var (bitmap, inputFormat) = DecodeImage(request.FileContentBase64);
            using (bitmap)
            {
                // Determine output format
                var outputFormat = DetermineOutputFormat(request.OutputFormat, inputFormat);

                // Calculate target dimensions
                var (targetWidth, targetHeight) = CalculateTargetDimensions(
                    bitmap.Width, bitmap.Height,
                    request.Width, request.Height,
                    request.MaintainAspectRatio);

                // Resize the image
                using var resizedBitmap = ResizeImage(bitmap, targetWidth, targetHeight);

                // Encode the result
                var (outputBytes, actualFormat) = EncodeImage(resizedBitmap, outputFormat, request.Quality);

                var response = new ImageResizeResponse
                {
                    FileContentBase64 = Convert.ToBase64String(outputBytes),
                    Format = actualFormat,
                    Width = resizedBitmap.Width,
                    Height = resizedBitmap.Height,
                    FileSizeBytes = outputBytes.Length
                };

                _logger.LogInformation("Image resized successfully: {Width}x{Height}, {Format}, {Size} bytes",
                    response.Width, response.Height, response.Format, response.FileSizeBytes);

                return Ok(response);
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in resize request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resizing image");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while resizing the image", details = ex.Message });
        }
    }

    /// <summary>
    /// Get information about an image without modifying it
    /// </summary>
    /// <param name="request">Request containing image file</param>
    /// <returns>Image metadata</returns>
    [HttpPost("info")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ImageInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetInfo([FromBody] ImageFileRequest request)
    {
        try
        {
            _logger.LogInformation("Getting image info");

            var imageBytes = DecodeBase64(request.FileContentBase64);
            var format = DetectImageFormat(imageBytes);

            using var bitmap = SKBitmap.Decode(imageBytes);
            if (bitmap == null)
            {
                return BadRequest(new { error = "Unable to decode image. Only JPEG and PNG formats are supported." });
            }

            var response = new ImageInfoResponse
            {
                Format = format,
                Width = bitmap.Width,
                Height = bitmap.Height,
                FileSizeBytes = imageBytes.Length
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in info request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image info");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while reading the image", details = ex.Message });
        }
    }

    private (SKBitmap bitmap, string format) DecodeImage(string fileContentBase64)
    {
        var imageBytes = DecodeBase64(fileContentBase64);
        var format = DetectImageFormat(imageBytes);

        var bitmap = SKBitmap.Decode(imageBytes);
        if (bitmap == null)
        {
            throw new ArgumentException("Unable to decode image. Only JPEG and PNG formats are supported.");
        }

        return (bitmap, format);
    }

    private byte[] DecodeBase64(string fileContentBase64)
    {
        if (string.IsNullOrWhiteSpace(fileContentBase64))
        {
            throw new ArgumentException("File content cannot be empty");
        }

        byte[] fileBytes;
        try
        {
            fileBytes = Convert.FromBase64String(fileContentBase64);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Invalid Base64 file content");
        }

        if (fileBytes.Length == 0)
        {
            throw new ArgumentException("File content cannot be empty");
        }

        return fileBytes;
    }

    private string DetectImageFormat(byte[] imageBytes)
    {
        // Check for JPEG magic bytes (FF D8 FF)
        if (imageBytes.Length >= 3 &&
            imageBytes[0] == 0xFF &&
            imageBytes[1] == 0xD8 &&
            imageBytes[2] == 0xFF)
        {
            return "jpeg";
        }

        // Check for PNG magic bytes (89 50 4E 47 0D 0A 1A 0A)
        if (imageBytes.Length >= 8 &&
            imageBytes[0] == 0x89 &&
            imageBytes[1] == 0x50 &&
            imageBytes[2] == 0x4E &&
            imageBytes[3] == 0x47 &&
            imageBytes[4] == 0x0D &&
            imageBytes[5] == 0x0A &&
            imageBytes[6] == 0x1A &&
            imageBytes[7] == 0x0A)
        {
            return "png";
        }

        throw new ArgumentException("Unsupported image format. Only JPEG and PNG are supported.");
    }

    private string DetermineOutputFormat(string? requestedFormat, string inputFormat)
    {
        if (string.IsNullOrWhiteSpace(requestedFormat))
        {
            return inputFormat;
        }

        var format = requestedFormat.ToLowerInvariant().Trim();
        if (format == "jpg")
        {
            format = "jpeg";
        }

        if (format != "jpeg" && format != "png")
        {
            throw new ArgumentException("Output format must be 'jpeg' or 'png'");
        }

        return format;
    }

    private (int width, int height) CalculateTargetDimensions(
        int originalWidth, int originalHeight,
        int? targetWidth, int? targetHeight,
        bool maintainAspectRatio)
    {
        // If only width is specified
        if (targetWidth.HasValue && !targetHeight.HasValue)
        {
            var ratio = (double)targetWidth.Value / originalWidth;
            return (targetWidth.Value, (int)Math.Round(originalHeight * ratio));
        }

        // If only height is specified
        if (!targetWidth.HasValue && targetHeight.HasValue)
        {
            var ratio = (double)targetHeight.Value / originalHeight;
            return ((int)Math.Round(originalWidth * ratio), targetHeight.Value);
        }

        // Both dimensions specified
        if (targetWidth.HasValue && targetHeight.HasValue)
        {
            if (maintainAspectRatio)
            {
                // Scale to fit within the specified dimensions
                var widthRatio = (double)targetWidth.Value / originalWidth;
                var heightRatio = (double)targetHeight.Value / originalHeight;
                var ratio = Math.Min(widthRatio, heightRatio);

                return ((int)Math.Round(originalWidth * ratio), (int)Math.Round(originalHeight * ratio));
            }
            else
            {
                // Use exact dimensions (may distort image)
                return (targetWidth.Value, targetHeight.Value);
            }
        }

        // Should not reach here due to validation
        return (originalWidth, originalHeight);
    }

    private SKBitmap ResizeImage(SKBitmap original, int targetWidth, int targetHeight)
    {
        var resized = new SKBitmap(targetWidth, targetHeight, original.ColorType, original.AlphaType);

        using var canvas = new SKCanvas(resized);
        using var paint = new SKPaint
        {
            FilterQuality = SKFilterQuality.High,
            IsAntialias = true
        };

        canvas.DrawBitmap(original,
            new SKRect(0, 0, original.Width, original.Height),
            new SKRect(0, 0, targetWidth, targetHeight),
            paint);

        return resized;
    }

    private (byte[] bytes, string format) EncodeImage(SKBitmap bitmap, string format, int quality)
    {
        SKEncodedImageFormat skFormat;
        string outputFormat;

        if (format == "png")
        {
            skFormat = SKEncodedImageFormat.Png;
            outputFormat = "png";
        }
        else
        {
            skFormat = SKEncodedImageFormat.Jpeg;
            outputFormat = "jpeg";
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(skFormat, quality);

        return (data.ToArray(), outputFormat);
    }
}
