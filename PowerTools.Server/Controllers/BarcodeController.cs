using Microsoft.AspNetCore.Mvc;
using PowerTools.Server.Models;
using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;
using ZXing.SkiaSharp.Rendering;

namespace PowerTools.Server.Controllers;

/// <summary>
/// Controller for barcode and QR code generation
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BarcodeController : ControllerBase
{
    private readonly ILogger<BarcodeController> _logger;

    private static readonly Dictionary<string, BarcodeFormat> SupportedBarcodeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Code128", BarcodeFormat.CODE_128 },
        { "Code39", BarcodeFormat.CODE_39 },
        { "EAN13", BarcodeFormat.EAN_13 },
        { "EAN8", BarcodeFormat.EAN_8 },
        { "UPC_A", BarcodeFormat.UPC_A },
        { "ITF", BarcodeFormat.ITF },
        { "Codabar", BarcodeFormat.CODABAR }
    };

    public BarcodeController(ILogger<BarcodeController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generate a QR code image
    /// </summary>
    /// <param name="request">QR code generation request</param>
    /// <returns>QR code image in Base64 format</returns>
    [HttpPost("qrcode")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(QrCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GenerateQrCode([FromBody] QrCodeRequest request)
    {
        try
        {
            _logger.LogInformation("Generating QR code: Size={Size}, Format={Format}, ErrorCorrectionLevel={ErrorCorrectionLevel}",
                request.Size, request.Format, request.ErrorCorrectionLevel);

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { error = "Content cannot be empty" });
            }

            var format = request.Format?.ToLowerInvariant() ?? "png";
            if (format != "png" && format != "svg")
            {
                return BadRequest(new { error = "Format must be 'png' or 'svg'" });
            }

            var ecLevel = ParseErrorCorrectionLevel(request.ErrorCorrectionLevel);
            if (ecLevel == null)
            {
                return BadRequest(new { error = "ErrorCorrectionLevel must be 'L', 'M', 'Q', or 'H'" });
            }

            // Generate QR code
            var writer = new BarcodeWriter<SKBitmap>
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Width = request.Size,
                    Height = request.Size,
                    Margin = 1,
                    Hints =
                    {
                        { EncodeHintType.ERROR_CORRECTION, ecLevel }
                    }
                },
                Renderer = new SKBitmapRenderer()
            };

            using var bitmap = writer.Write(request.Content);

            byte[] imageBytes;
            if (format == "svg")
            {
                var svgContent = GenerateSvgFromBitmap(bitmap);
                imageBytes = System.Text.Encoding.UTF8.GetBytes(svgContent);
            }
            else
            {
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                imageBytes = data.ToArray();
            }

            var response = new QrCodeResponse
            {
                ImageBase64 = Convert.ToBase64String(imageBytes),
                Format = format,
                Width = bitmap.Width,
                Height = bitmap.Height,
                FileSizeBytes = imageBytes.Length
            };

            _logger.LogInformation("QR code generated successfully: {Width}x{Height}, {Format}, {Size} bytes",
                response.Width, response.Height, response.Format, response.FileSizeBytes);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while generating the QR code", details = ex.Message });
        }
    }

    /// <summary>
    /// Generate a barcode image
    /// </summary>
    /// <param name="request">Barcode generation request</param>
    /// <returns>Barcode image in Base64 format</returns>
    [HttpPost("barcode")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(BarcodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GenerateBarcode([FromBody] BarcodeRequest request)
    {
        try
        {
            _logger.LogInformation("Generating barcode: Type={BarcodeType}, Width={Width}, Height={Height}, Format={Format}",
                request.BarcodeType, request.Width, request.Height, request.Format);

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { error = "Content cannot be empty" });
            }

            if (string.IsNullOrWhiteSpace(request.BarcodeType))
            {
                return BadRequest(new { error = "BarcodeType is required" });
            }

            if (!SupportedBarcodeTypes.TryGetValue(request.BarcodeType, out var barcodeFormat))
            {
                var supportedTypes = string.Join(", ", SupportedBarcodeTypes.Keys);
                return BadRequest(new { error = $"Unsupported barcode type. Supported types: {supportedTypes}" });
            }

            var format = request.Format?.ToLowerInvariant() ?? "png";
            if (format != "png" && format != "svg")
            {
                return BadRequest(new { error = "Format must be 'png' or 'svg'" });
            }

            // Validate content for specific barcode types
            var validationError = ValidateBarcodeContent(request.Content, barcodeFormat);
            if (validationError != null)
            {
                return BadRequest(new { error = validationError });
            }

            // Generate barcode
            var writer = new BarcodeWriter<SKBitmap>
            {
                Format = barcodeFormat,
                Options = new EncodingOptions
                {
                    Width = request.Width,
                    Height = request.Height,
                    Margin = 5,
                    PureBarcode = !request.ShowText
                },
                Renderer = new SKBitmapRenderer()
            };

            using var bitmap = writer.Write(request.Content);

            byte[] imageBytes;
            if (format == "svg")
            {
                var svgContent = GenerateSvgFromBitmap(bitmap);
                imageBytes = System.Text.Encoding.UTF8.GetBytes(svgContent);
            }
            else
            {
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                imageBytes = data.ToArray();
            }

            var response = new BarcodeResponse
            {
                ImageBase64 = Convert.ToBase64String(imageBytes),
                Format = format,
                Width = bitmap.Width,
                Height = bitmap.Height,
                BarcodeType = request.BarcodeType,
                FileSizeBytes = imageBytes.Length
            };

            _logger.LogInformation("Barcode generated successfully: {Width}x{Height}, {BarcodeType}, {Format}, {Size} bytes",
                response.Width, response.Height, response.BarcodeType, response.Format, response.FileSizeBytes);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating barcode");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while generating the barcode", details = ex.Message });
        }
    }

    /// <summary>
    /// Get list of supported barcode types
    /// </summary>
    /// <returns>List of supported barcode type names</returns>
    [HttpGet("types")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public IActionResult GetSupportedTypes()
    {
        return Ok(SupportedBarcodeTypes.Keys.ToList());
    }

    private static ZXing.QrCode.Internal.ErrorCorrectionLevel? ParseErrorCorrectionLevel(string? level)
    {
        return level?.ToUpperInvariant() switch
        {
            "L" => ZXing.QrCode.Internal.ErrorCorrectionLevel.L,
            "M" => ZXing.QrCode.Internal.ErrorCorrectionLevel.M,
            "Q" => ZXing.QrCode.Internal.ErrorCorrectionLevel.Q,
            "H" => ZXing.QrCode.Internal.ErrorCorrectionLevel.H,
            null => ZXing.QrCode.Internal.ErrorCorrectionLevel.M,
            "" => ZXing.QrCode.Internal.ErrorCorrectionLevel.M,
            _ => null
        };
    }

    private static string? ValidateBarcodeContent(string content, BarcodeFormat format)
    {
        return format switch
        {
            BarcodeFormat.EAN_13 => ValidateEan13(content),
            BarcodeFormat.EAN_8 => ValidateEan8(content),
            BarcodeFormat.UPC_A => ValidateUpcA(content),
            BarcodeFormat.ITF => ValidateItf(content),
            _ => null
        };
    }

    private static string? ValidateEan13(string content)
    {
        // EAN-13 requires exactly 12 or 13 digits (13th is check digit, can be auto-calculated)
        if (content.Length != 12 && content.Length != 13)
        {
            return "EAN-13 requires exactly 12 or 13 digits";
        }
        if (!content.All(char.IsDigit))
        {
            return "EAN-13 content must contain only digits";
        }
        return null;
    }

    private static string? ValidateEan8(string content)
    {
        // EAN-8 requires exactly 7 or 8 digits
        if (content.Length != 7 && content.Length != 8)
        {
            return "EAN-8 requires exactly 7 or 8 digits";
        }
        if (!content.All(char.IsDigit))
        {
            return "EAN-8 content must contain only digits";
        }
        return null;
    }

    private static string? ValidateUpcA(string content)
    {
        // UPC-A requires exactly 11 or 12 digits
        if (content.Length != 11 && content.Length != 12)
        {
            return "UPC-A requires exactly 11 or 12 digits";
        }
        if (!content.All(char.IsDigit))
        {
            return "UPC-A content must contain only digits";
        }
        return null;
    }

    private static string? ValidateItf(string content)
    {
        // ITF requires even number of digits
        if (content.Length % 2 != 0)
        {
            return "ITF requires an even number of digits";
        }
        if (!content.All(char.IsDigit))
        {
            return "ITF content must contain only digits";
        }
        return null;
    }

    private static string GenerateSvgFromBitmap(SKBitmap bitmap)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{bitmap.Width}\" height=\"{bitmap.Height}\" viewBox=\"0 0 {bitmap.Width} {bitmap.Height}\">");
        sb.AppendLine($"<rect width=\"{bitmap.Width}\" height=\"{bitmap.Height}\" fill=\"white\"/>");

        // Convert bitmap pixels to SVG rectangles (for black pixels only)
        for (int y = 0; y < bitmap.Height; y++)
        {
            int x = 0;
            while (x < bitmap.Width)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.Red < 128) // Black pixel
                {
                    // Find the run length of black pixels
                    int startX = x;
                    while (x < bitmap.Width && bitmap.GetPixel(x, y).Red < 128)
                    {
                        x++;
                    }
                    sb.AppendLine($"<rect x=\"{startX}\" y=\"{y}\" width=\"{x - startX}\" height=\"1\" fill=\"black\"/>");
                }
                else
                {
                    x++;
                }
            }
        }

        sb.AppendLine("</svg>");
        return sb.ToString();
    }
}
