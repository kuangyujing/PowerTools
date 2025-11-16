namespace PowerTools.Server.Models;

/// <summary>
/// Response model for file encoding conversion
/// </summary>
public class EncodingConversionResponse
{
    /// <summary>
    /// Base64 encoded converted file content
    /// </summary>
    public string FileContentBase64 { get; set; } = string.Empty;

    /// <summary>
    /// File name (same as input)
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Detected or specified input encoding
    /// </summary>
    public string DetectedEncoding { get; set; } = string.Empty;

    /// <summary>
    /// Output encoding used for conversion
    /// </summary>
    public string OutputEncoding { get; set; } = string.Empty;

    /// <summary>
    /// Size of the converted file in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Confidence level of encoding detection (0.0 to 1.0). Only applicable when auto-detecting.
    /// </summary>
    public double? DetectionConfidence { get; set; }
}
