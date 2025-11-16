using System.ComponentModel.DataAnnotations;

namespace PowerTools.Server.Models;

/// <summary>
/// Request model for file encoding conversion
/// </summary>
public class EncodingConversionRequest
{
    /// <summary>
    /// Base64 encoded file content
    /// </summary>
    [Required(ErrorMessage = "File content is required")]
    public string FileContentBase64 { get; set; } = string.Empty;

    /// <summary>
    /// Original file name with extension (e.g., "data.csv")
    /// </summary>
    [Required(ErrorMessage = "File name is required")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Target output encoding (e.g., "UTF-8", "Shift_JIS", "EUC-JP", "ISO-2022-JP")
    /// </summary>
    [Required(ErrorMessage = "Output encoding is required")]
    public string OutputEncoding { get; set; } = string.Empty;

    /// <summary>
    /// Optional input encoding. If not specified, the service will auto-detect the encoding.
    /// </summary>
    public string? InputEncoding { get; set; }
}
