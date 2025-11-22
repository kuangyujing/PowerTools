using System.ComponentModel.DataAnnotations;

namespace PowerTools.Server.Models;

/// <summary>
/// Request to resize an image
/// </summary>
public class ImageResizeRequest
{
    /// <summary>
    /// Image file content in Base64 format (JPEG or PNG)
    /// </summary>
    [Required]
    public string FileContentBase64 { get; set; } = string.Empty;

    /// <summary>
    /// Target width in pixels. If only width is specified, height will be calculated to maintain aspect ratio.
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Target height in pixels. If only height is specified, width will be calculated to maintain aspect ratio.
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Whether to maintain aspect ratio when both width and height are specified.
    /// If true, the image will be scaled to fit within the specified dimensions.
    /// Default is true.
    /// </summary>
    public bool MaintainAspectRatio { get; set; } = true;

    /// <summary>
    /// Output format: "jpeg" or "png". If not specified, uses the same format as input.
    /// </summary>
    public string? OutputFormat { get; set; }

    /// <summary>
    /// JPEG quality (1-100). Only applies when output format is JPEG. Default is 85.
    /// </summary>
    [Range(1, 100)]
    public int Quality { get; set; } = 85;
}

/// <summary>
/// Response containing the resized image
/// </summary>
public class ImageResizeResponse
{
    /// <summary>
    /// Resized image content in Base64 format
    /// </summary>
    public string FileContentBase64 { get; set; } = string.Empty;

    /// <summary>
    /// Output image format ("jpeg" or "png")
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Width of the resized image in pixels
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Height of the resized image in pixels
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }
}

/// <summary>
/// Response containing image metadata
/// </summary>
public class ImageInfoResponse
{
    /// <summary>
    /// Image format ("jpeg" or "png")
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Width in pixels
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Height in pixels
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }
}

/// <summary>
/// Request containing only the image file
/// </summary>
public class ImageFileRequest
{
    /// <summary>
    /// Image file content in Base64 format (JPEG or PNG)
    /// </summary>
    [Required]
    public string FileContentBase64 { get; set; } = string.Empty;
}
