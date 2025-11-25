using System.ComponentModel.DataAnnotations;

namespace PowerTools.Server.Models;

/// <summary>
/// Request to upload a file to temporary storage
/// </summary>
public class StorageUploadRequest
{
    /// <summary>
    /// File content encoded in Base64
    /// </summary>
    [Required]
    public string FileContentBase64 { get; set; } = string.Empty;

    /// <summary>
    /// Original file name (optional, for reference)
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Time-to-live in minutes (default: 60, max: 1440 = 24 hours)
    /// </summary>
    [Range(1, 1440)]
    public int TtlMinutes { get; set; } = 60;
}

/// <summary>
/// Response after uploading a file
/// </summary>
public class StorageUploadResponse
{
    /// <summary>
    /// Unique access key to retrieve or delete the file
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Original file name if provided
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Expiration time in UTC
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }
}

/// <summary>
/// Request to retrieve a file from temporary storage
/// </summary>
public class StorageGetRequest
{
    /// <summary>
    /// Access key received from upload
    /// </summary>
    [Required]
    public string AccessKey { get; set; } = string.Empty;
}

/// <summary>
/// Response containing the retrieved file
/// </summary>
public class StorageGetResponse
{
    /// <summary>
    /// File content encoded in Base64
    /// </summary>
    public string FileContentBase64 { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Original file name if provided during upload
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Expiration time in UTC
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }
}

/// <summary>
/// Request to delete a file from temporary storage
/// </summary>
public class StorageDeleteRequest
{
    /// <summary>
    /// Access key received from upload
    /// </summary>
    [Required]
    public string AccessKey { get; set; } = string.Empty;
}

/// <summary>
/// Response after deleting a file
/// </summary>
public class StorageDeleteResponse
{
    /// <summary>
    /// Whether the file was successfully deleted
    /// </summary>
    public bool Deleted { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Internal model for stored file metadata
/// </summary>
public class StoredFileMetadata
{
    /// <summary>
    /// Unique access key
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Original file name
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Creation time in UTC
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Expiration time in UTC
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }
}
