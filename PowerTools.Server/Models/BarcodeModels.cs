using System.ComponentModel.DataAnnotations;

namespace PowerTools.Server.Models;

/// <summary>
/// Request model for QR code generation
/// </summary>
public class QrCodeRequest
{
    /// <summary>
    /// The content to encode in the QR code (URL, text, etc.)
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Size of the QR code image in pixels (width and height are equal)
    /// </summary>
    [Range(50, 2000)]
    public int Size { get; set; } = 200;

    /// <summary>
    /// Output format: "png" or "svg"
    /// </summary>
    public string Format { get; set; } = "png";

    /// <summary>
    /// Error correction level: "L" (7%), "M" (15%), "Q" (25%), "H" (30%)
    /// </summary>
    public string ErrorCorrectionLevel { get; set; } = "M";
}

/// <summary>
/// Response model for QR code generation
/// </summary>
public class QrCodeResponse
{
    /// <summary>
    /// Base64-encoded image data
    /// </summary>
    public string ImageBase64 { get; set; } = string.Empty;

    /// <summary>
    /// Data URI for direct image embedding (e.g., data:image/png;base64,xxxxx)
    /// </summary>
    public string DataUri { get; set; } = string.Empty;

    /// <summary>
    /// Output format (png or svg)
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Image width in pixels
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Image height in pixels
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }
}

/// <summary>
/// Request model for barcode generation
/// </summary>
public class BarcodeRequest
{
    /// <summary>
    /// The content to encode in the barcode
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Barcode type: Code128, Code39, EAN13, EAN8, UPC_A, ITF, Codabar
    /// </summary>
    [Required]
    public string BarcodeType { get; set; } = string.Empty;

    /// <summary>
    /// Image width in pixels
    /// </summary>
    [Range(50, 2000)]
    public int Width { get; set; } = 300;

    /// <summary>
    /// Image height in pixels
    /// </summary>
    [Range(30, 500)]
    public int Height { get; set; } = 100;

    /// <summary>
    /// Output format: "png" or "svg"
    /// </summary>
    public string Format { get; set; } = "png";

    /// <summary>
    /// Whether to show the content text below the barcode
    /// </summary>
    public bool ShowText { get; set; } = true;
}

/// <summary>
/// Response model for barcode generation
/// </summary>
public class BarcodeResponse
{
    /// <summary>
    /// Base64-encoded image data
    /// </summary>
    public string ImageBase64 { get; set; } = string.Empty;

    /// <summary>
    /// Data URI for direct image embedding (e.g., data:image/png;base64,xxxxx)
    /// </summary>
    public string DataUri { get; set; } = string.Empty;

    /// <summary>
    /// Output format (png or svg)
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Image width in pixels
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Image height in pixels
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// The barcode type used
    /// </summary>
    public string BarcodeType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }
}
