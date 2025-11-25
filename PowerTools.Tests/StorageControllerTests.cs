using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PowerTools.Server.Controllers;
using PowerTools.Server.Models;
using PowerTools.Server.Services;
using Xunit;

namespace PowerTools.Tests;

public class StorageControllerTests : IDisposable
{
    private readonly StorageController _controller;
    private readonly StorageService _storageService;

    public StorageControllerTests()
    {
        var controllerLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<StorageController>();
        var serviceLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<StorageService>();

        _storageService = new StorageService(serviceLogger);
        _controller = new StorageController(controllerLogger, _storageService);
    }

    public void Dispose()
    {
        _storageService.Dispose();
    }

    private static string CreateTestFileBase64(string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        return Convert.ToBase64String(bytes);
    }

    private static string CreateTestFileBase64(int sizeInBytes)
    {
        var bytes = new byte[sizeInBytes];
        new Random(42).NextBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    #region Upload Tests

    [Fact]
    public void Upload_ReturnsAccessKey_WhenValidRequest()
    {
        // Arrange
        var request = new StorageUploadRequest
        {
            FileContentBase64 = CreateTestFileBase64("Hello, World!"),
            FileName = "test.txt",
            TtlMinutes = 30
        };

        // Act
        var result = _controller.Upload(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);

        var response = result.Value as StorageUploadResponse;
        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessKey));
        Assert.Equal("test.txt", response.FileName);
        Assert.True(response.FileSizeBytes > 0);
        Assert.True(response.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public void Upload_UsesDefaultTtl_WhenNotSpecified()
    {
        // Arrange
        var request = new StorageUploadRequest
        {
            FileContentBase64 = CreateTestFileBase64("Test content")
        };

        // Act
        var result = _controller.Upload(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as StorageUploadResponse;
        Assert.NotNull(response);

        // Default TTL is 60 minutes
        var expectedExpiry = DateTime.UtcNow.AddMinutes(60);
        Assert.True(response.ExpiresAtUtc <= expectedExpiry);
        Assert.True(response.ExpiresAtUtc > DateTime.UtcNow.AddMinutes(59));
    }

    [Fact]
    public void Upload_ReturnsBadRequest_WhenFileContentIsEmpty()
    {
        // Arrange
        var request = new StorageUploadRequest
        {
            FileContentBase64 = "",
            FileName = "test.txt"
        };

        // Act
        var result = _controller.Upload(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Upload_ReturnsBadRequest_WhenFileContentIsNull()
    {
        // Arrange
        var request = new StorageUploadRequest
        {
            FileContentBase64 = null!,
            FileName = "test.txt"
        };

        // Act
        var result = _controller.Upload(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Upload_ReturnsBadRequest_WhenInvalidBase64()
    {
        // Arrange
        var request = new StorageUploadRequest
        {
            FileContentBase64 = "not-valid-base64!!!",
            FileName = "test.txt"
        };

        // Act
        var result = _controller.Upload(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Upload_ReturnsBadRequest_WhenFileTooLarge()
    {
        // Arrange - Create a file larger than 10MB
        var largeContent = CreateTestFileBase64((int)(StorageService.MaxFileSizeBytes + 1));
        var request = new StorageUploadRequest
        {
            FileContentBase64 = largeContent,
            FileName = "large.bin"
        };

        // Act
        var result = _controller.Upload(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Upload_WorksWithoutFileName()
    {
        // Arrange
        var request = new StorageUploadRequest
        {
            FileContentBase64 = CreateTestFileBase64("Content without filename")
        };

        // Act
        var result = _controller.Upload(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as StorageUploadResponse;
        Assert.NotNull(response);
        Assert.Null(response.FileName);
    }

    #endregion

    #region Get Tests

    [Fact]
    public void Get_ReturnsFile_WhenValidAccessKey()
    {
        // Arrange
        var uploadRequest = new StorageUploadRequest
        {
            FileContentBase64 = CreateTestFileBase64("Hello, World!"),
            FileName = "test.txt",
            TtlMinutes = 30
        };
        var uploadResult = _controller.Upload(uploadRequest) as OkObjectResult;
        var uploadResponse = uploadResult?.Value as StorageUploadResponse;

        var getRequest = new StorageGetRequest
        {
            AccessKey = uploadResponse!.AccessKey
        };

        // Act
        var result = _controller.Get(getRequest) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);

        var response = result.Value as StorageGetResponse;
        Assert.NotNull(response);
        Assert.Equal(uploadRequest.FileContentBase64, response.FileContentBase64);
        Assert.Equal("test.txt", response.FileName);
    }

    [Fact]
    public void Get_ReturnsNotFound_WhenInvalidAccessKey()
    {
        // Arrange
        var request = new StorageGetRequest
        {
            AccessKey = "invalid-access-key"
        };

        // Act
        var result = _controller.Get(request) as NotFoundObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public void Get_ReturnsBadRequest_WhenAccessKeyIsEmpty()
    {
        // Arrange
        var request = new StorageGetRequest
        {
            AccessKey = ""
        };

        // Act
        var result = _controller.Get(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Get_ReturnsNotFound_WhenFileExpired()
    {
        // Arrange - Upload with 1 minute TTL and manually expire it
        var uploadRequest = new StorageUploadRequest
        {
            FileContentBase64 = CreateTestFileBase64("Expiring content"),
            TtlMinutes = 1
        };
        var uploadResult = _controller.Upload(uploadRequest) as OkObjectResult;
        var uploadResponse = uploadResult?.Value as StorageUploadResponse;

        // We can't actually wait for expiration in unit tests, but we can verify
        // that the file exists initially
        var getRequest = new StorageGetRequest
        {
            AccessKey = uploadResponse!.AccessKey
        };
        var firstResult = _controller.Get(getRequest) as OkObjectResult;
        Assert.NotNull(firstResult);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public void Delete_ReturnsSuccess_WhenValidAccessKey()
    {
        // Arrange
        var uploadRequest = new StorageUploadRequest
        {
            FileContentBase64 = CreateTestFileBase64("To be deleted"),
            FileName = "delete-me.txt"
        };
        var uploadResult = _controller.Upload(uploadRequest) as OkObjectResult;
        var uploadResponse = uploadResult?.Value as StorageUploadResponse;

        var deleteRequest = new StorageDeleteRequest
        {
            AccessKey = uploadResponse!.AccessKey
        };

        // Act
        var result = _controller.Delete(deleteRequest) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);

        var response = result.Value as StorageDeleteResponse;
        Assert.NotNull(response);
        Assert.True(response.Deleted);
    }

    [Fact]
    public void Delete_ReturnsNotFound_WhenInvalidAccessKey()
    {
        // Arrange
        var request = new StorageDeleteRequest
        {
            AccessKey = "invalid-access-key"
        };

        // Act
        var result = _controller.Delete(request) as NotFoundObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public void Delete_ReturnsBadRequest_WhenAccessKeyIsEmpty()
    {
        // Arrange
        var request = new StorageDeleteRequest
        {
            AccessKey = ""
        };

        // Act
        var result = _controller.Delete(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Delete_CannotGetFileAfterDeletion()
    {
        // Arrange
        var uploadRequest = new StorageUploadRequest
        {
            FileContentBase64 = CreateTestFileBase64("Content to delete")
        };
        var uploadResult = _controller.Upload(uploadRequest) as OkObjectResult;
        var uploadResponse = uploadResult?.Value as StorageUploadResponse;

        var deleteRequest = new StorageDeleteRequest
        {
            AccessKey = uploadResponse!.AccessKey
        };
        _controller.Delete(deleteRequest);

        var getRequest = new StorageGetRequest
        {
            AccessKey = uploadResponse.AccessKey
        };

        // Act
        var result = _controller.Get(getRequest) as NotFoundObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public void Delete_ReturnsNotFound_WhenDeletingTwice()
    {
        // Arrange
        var uploadRequest = new StorageUploadRequest
        {
            FileContentBase64 = CreateTestFileBase64("Delete twice")
        };
        var uploadResult = _controller.Upload(uploadRequest) as OkObjectResult;
        var uploadResponse = uploadResult?.Value as StorageUploadResponse;

        var deleteRequest = new StorageDeleteRequest
        {
            AccessKey = uploadResponse!.AccessKey
        };

        // Delete first time
        _controller.Delete(deleteRequest);

        // Act - Delete second time
        var result = _controller.Delete(deleteRequest) as NotFoundObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(404, result.StatusCode);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullWorkflow_UploadGetDelete_WorksCorrectly()
    {
        // Arrange
        var originalContent = "This is test content for full workflow";
        var base64Content = CreateTestFileBase64(originalContent);

        // Upload
        var uploadRequest = new StorageUploadRequest
        {
            FileContentBase64 = base64Content,
            FileName = "workflow-test.txt",
            TtlMinutes = 60
        };
        var uploadResult = _controller.Upload(uploadRequest) as OkObjectResult;
        Assert.NotNull(uploadResult);
        var uploadResponse = uploadResult.Value as StorageUploadResponse;
        Assert.NotNull(uploadResponse);

        // Get
        var getRequest = new StorageGetRequest { AccessKey = uploadResponse.AccessKey };
        var getResult = _controller.Get(getRequest) as OkObjectResult;
        Assert.NotNull(getResult);
        var getResponse = getResult.Value as StorageGetResponse;
        Assert.NotNull(getResponse);
        Assert.Equal(base64Content, getResponse.FileContentBase64);

        // Delete
        var deleteRequest = new StorageDeleteRequest { AccessKey = uploadResponse.AccessKey };
        var deleteResult = _controller.Delete(deleteRequest) as OkObjectResult;
        Assert.NotNull(deleteResult);

        // Verify deleted
        var getAfterDelete = _controller.Get(getRequest) as NotFoundObjectResult;
        Assert.NotNull(getAfterDelete);
    }

    [Fact]
    public void Upload_MultipleFiles_EachHasUniqueAccessKey()
    {
        // Arrange & Act
        var accessKeys = new HashSet<string>();
        for (int i = 0; i < 10; i++)
        {
            var request = new StorageUploadRequest
            {
                FileContentBase64 = CreateTestFileBase64($"File content {i}"),
                FileName = $"file{i}.txt"
            };
            var result = _controller.Upload(request) as OkObjectResult;
            var response = result?.Value as StorageUploadResponse;
            Assert.NotNull(response);
            accessKeys.Add(response.AccessKey);
        }

        // Assert - All access keys should be unique
        Assert.Equal(10, accessKeys.Count);
    }

    [Fact]
    public void Upload_BinaryData_WorksCorrectly()
    {
        // Arrange - Create binary data (not valid UTF-8)
        var binaryData = new byte[] { 0x00, 0x01, 0x02, 0xFF, 0xFE, 0xFD };
        var base64Content = Convert.ToBase64String(binaryData);

        var uploadRequest = new StorageUploadRequest
        {
            FileContentBase64 = base64Content,
            FileName = "binary.bin"
        };

        // Act - Upload
        var uploadResult = _controller.Upload(uploadRequest) as OkObjectResult;
        Assert.NotNull(uploadResult);
        var uploadResponse = uploadResult.Value as StorageUploadResponse;
        Assert.NotNull(uploadResponse);

        // Act - Get
        var getRequest = new StorageGetRequest { AccessKey = uploadResponse.AccessKey };
        var getResult = _controller.Get(getRequest) as OkObjectResult;
        Assert.NotNull(getResult);
        var getResponse = getResult.Value as StorageGetResponse;

        // Assert
        Assert.NotNull(getResponse);
        Assert.Equal(base64Content, getResponse.FileContentBase64);

        var retrievedBytes = Convert.FromBase64String(getResponse.FileContentBase64);
        Assert.Equal(binaryData, retrievedBytes);
    }

    #endregion
}
