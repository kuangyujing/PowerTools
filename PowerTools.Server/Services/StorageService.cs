using System.Collections.Concurrent;
using System.Security.Cryptography;
using PowerTools.Server.Models;

namespace PowerTools.Server.Services;

/// <summary>
/// Service for managing temporary file storage with automatic expiration
/// </summary>
public class StorageService : IDisposable
{
    private readonly ConcurrentDictionary<string, StoredFile> _storage = new();
    private readonly ILogger<StorageService> _logger;
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
    private bool _disposed;

    /// <summary>
    /// Maximum file size allowed (10MB)
    /// </summary>
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Maximum number of files stored at once
    /// </summary>
    public const int MaxStoredFiles = 1000;

    public StorageService(ILogger<StorageService> logger)
    {
        _logger = logger;
        _cleanupTimer = new Timer(CleanupExpiredFiles, null, _cleanupInterval, _cleanupInterval);
        _logger.LogInformation("StorageService initialized with cleanup interval of {Interval} minutes", _cleanupInterval.TotalMinutes);
    }

    /// <summary>
    /// Store a file in temporary storage
    /// </summary>
    /// <param name="fileContent">File content as byte array</param>
    /// <param name="fileName">Optional file name</param>
    /// <param name="ttlMinutes">Time-to-live in minutes</param>
    /// <returns>Stored file metadata</returns>
    public StoredFileMetadata StoreFile(byte[] fileContent, string? fileName, int ttlMinutes)
    {
        if (fileContent == null || fileContent.Length == 0)
        {
            throw new ArgumentException("File content cannot be empty");
        }

        if (fileContent.Length > MaxFileSizeBytes)
        {
            throw new ArgumentException($"File size exceeds maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)}MB");
        }

        if (_storage.Count >= MaxStoredFiles)
        {
            // Try cleanup first
            CleanupExpiredFilesSync();

            if (_storage.Count >= MaxStoredFiles)
            {
                throw new InvalidOperationException("Storage capacity exceeded. Please try again later.");
            }
        }

        var accessKey = GenerateAccessKey();
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(ttlMinutes);

        var storedFile = new StoredFile
        {
            Content = fileContent,
            Metadata = new StoredFileMetadata
            {
                AccessKey = accessKey,
                FileName = fileName,
                FileSizeBytes = fileContent.Length,
                CreatedAtUtc = now,
                ExpiresAtUtc = expiresAt
            }
        };

        if (!_storage.TryAdd(accessKey, storedFile))
        {
            throw new InvalidOperationException("Failed to store file. Please try again.");
        }

        _logger.LogInformation("File stored: AccessKey={AccessKey}, Size={Size} bytes, ExpiresAt={ExpiresAt}",
            accessKey, fileContent.Length, expiresAt);

        return storedFile.Metadata;
    }

    /// <summary>
    /// Retrieve a file from storage
    /// </summary>
    /// <param name="accessKey">Access key</param>
    /// <returns>File content and metadata, or null if not found or expired</returns>
    public (byte[] Content, StoredFileMetadata Metadata)? GetFile(string accessKey)
    {
        if (string.IsNullOrWhiteSpace(accessKey))
        {
            throw new ArgumentException("Access key cannot be empty");
        }

        if (!_storage.TryGetValue(accessKey, out var storedFile))
        {
            _logger.LogWarning("File not found: AccessKey={AccessKey}", accessKey);
            return null;
        }

        if (DateTime.UtcNow > storedFile.Metadata.ExpiresAtUtc)
        {
            _logger.LogInformation("File expired: AccessKey={AccessKey}", accessKey);
            _storage.TryRemove(accessKey, out _);
            return null;
        }

        _logger.LogInformation("File retrieved: AccessKey={AccessKey}", accessKey);
        return (storedFile.Content, storedFile.Metadata);
    }

    /// <summary>
    /// Delete a file from storage
    /// </summary>
    /// <param name="accessKey">Access key</param>
    /// <returns>True if file was deleted, false if not found</returns>
    public bool DeleteFile(string accessKey)
    {
        if (string.IsNullOrWhiteSpace(accessKey))
        {
            throw new ArgumentException("Access key cannot be empty");
        }

        if (_storage.TryRemove(accessKey, out _))
        {
            _logger.LogInformation("File deleted: AccessKey={AccessKey}", accessKey);
            return true;
        }

        _logger.LogWarning("File not found for deletion: AccessKey={AccessKey}", accessKey);
        return false;
    }

    /// <summary>
    /// Get the current number of stored files
    /// </summary>
    public int StoredFileCount => _storage.Count;

    private static string GenerateAccessKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(24);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private void CleanupExpiredFiles(object? state)
    {
        CleanupExpiredFilesSync();
    }

    private void CleanupExpiredFilesSync()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _storage
            .Where(kvp => now > kvp.Value.Metadata.ExpiresAtUtc)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            if (_storage.TryRemove(key, out _))
            {
                _logger.LogDebug("Expired file cleaned up: AccessKey={AccessKey}", key);
            }
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired files. Current storage count: {Current}",
                expiredKeys.Count, _storage.Count);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cleanupTimer.Dispose();
        _storage.Clear();
    }

    private class StoredFile
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public StoredFileMetadata Metadata { get; set; } = new();
    }
}
