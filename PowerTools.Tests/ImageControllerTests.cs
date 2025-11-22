using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PowerTools.Server.Controllers;
using PowerTools.Server.Models;
using SkiaSharp;
using Xunit;

namespace PowerTools.Tests;

public class ImageControllerTests
{
    private readonly ImageController _controller;

    public ImageControllerTests()
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<ImageController>();
        _controller = new ImageController(logger);
    }

    private string CreateTestImageBase64(int width, int height, SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);

        // Draw a simple gradient for testing
        using var paint = new SKPaint { Color = SKColors.Blue };
        canvas.Clear(SKColors.White);
        canvas.DrawRect(0, 0, width / 2, height / 2, paint);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 85);

        return Convert.ToBase64String(data.ToArray());
    }

    #region Resize Tests

    [Fact]
    public void Resize_ResizesJpegImage_WhenWidthSpecified()
    {
        // Arrange
        var base64 = CreateTestImageBase64(800, 600, SKEncodedImageFormat.Jpeg);
        var request = new ImageResizeRequest
        {
            FileContentBase64 = base64,
            Width = 400
        };

        // Act
        var result = _controller.Resize(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);

        var response = result.Value as ImageResizeResponse;
        Assert.NotNull(response);
        Assert.Equal(400, response.Width);
        Assert.Equal(300, response.Height); // Maintains aspect ratio
        Assert.Equal("jpeg", response.Format);
        Assert.True(response.FileSizeBytes > 0);
    }

    [Fact]
    public void Resize_ResizesPngImage_WhenHeightSpecified()
    {
        // Arrange
        var base64 = CreateTestImageBase64(800, 600, SKEncodedImageFormat.Png);
        var request = new ImageResizeRequest
        {
            FileContentBase64 = base64,
            Height = 300
        };

        // Act
        var result = _controller.Resize(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as ImageResizeResponse;
        Assert.NotNull(response);
        Assert.Equal(400, response.Width); // Maintains aspect ratio
        Assert.Equal(300, response.Height);
        Assert.Equal("png", response.Format);
    }

    [Fact]
    public void Resize_MaintainsAspectRatio_WhenBothDimensionsSpecified()
    {
        // Arrange
        var base64 = CreateTestImageBase64(800, 600, SKEncodedImageFormat.Jpeg);
        var request = new ImageResizeRequest
        {
            FileContentBase64 = base64,
            Width = 400,
            Height = 400,
            MaintainAspectRatio = true
        };

        // Act
        var result = _controller.Resize(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as ImageResizeResponse;
        Assert.NotNull(response);
        // Should fit within 400x400 while maintaining 4:3 ratio
        Assert.Equal(400, response.Width);
        Assert.Equal(300, response.Height);
    }

    [Fact]
    public void Resize_StretchesImage_WhenMaintainAspectRatioIsFalse()
    {
        // Arrange
        var base64 = CreateTestImageBase64(800, 600, SKEncodedImageFormat.Jpeg);
        var request = new ImageResizeRequest
        {
            FileContentBase64 = base64,
            Width = 400,
            Height = 400,
            MaintainAspectRatio = false
        };

        // Act
        var result = _controller.Resize(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as ImageResizeResponse;
        Assert.NotNull(response);
        Assert.Equal(400, response.Width);
        Assert.Equal(400, response.Height); // Stretched to exact dimensions
    }

    [Fact]
    public void Resize_ConvertsFormat_WhenOutputFormatSpecified()
    {
        // Arrange
        var base64 = CreateTestImageBase64(400, 300, SKEncodedImageFormat.Jpeg);
        var request = new ImageResizeRequest
        {
            FileContentBase64 = base64,
            Width = 200,
            OutputFormat = "png"
        };

        // Act
        var result = _controller.Resize(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as ImageResizeResponse;
        Assert.NotNull(response);
        Assert.Equal("png", response.Format);
    }

    [Fact]
    public void Resize_ReturnsBadRequest_WhenNoDimensionsSpecified()
    {
        // Arrange
        var base64 = CreateTestImageBase64(400, 300, SKEncodedImageFormat.Jpeg);
        var request = new ImageResizeRequest
        {
            FileContentBase64 = base64
        };

        // Act
        var result = _controller.Resize(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Resize_ReturnsBadRequest_WhenWidthIsZero()
    {
        // Arrange
        var base64 = CreateTestImageBase64(400, 300, SKEncodedImageFormat.Jpeg);
        var request = new ImageResizeRequest
        {
            FileContentBase64 = base64,
            Width = 0
        };

        // Act
        var result = _controller.Resize(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Resize_ReturnsBadRequest_WhenFileContentIsEmpty()
    {
        // Arrange
        var request = new ImageResizeRequest
        {
            FileContentBase64 = "",
            Width = 400
        };

        // Act
        var result = _controller.Resize(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Resize_ReturnsBadRequest_WhenInvalidBase64()
    {
        // Arrange
        var request = new ImageResizeRequest
        {
            FileContentBase64 = "not-valid-base64!!!",
            Width = 400
        };

        // Act
        var result = _controller.Resize(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Resize_ReturnsBadRequest_WhenInvalidImageFormat()
    {
        // Arrange - Create a text file encoded as base64
        var textBytes = System.Text.Encoding.UTF8.GetBytes("This is not an image");
        var request = new ImageResizeRequest
        {
            FileContentBase64 = Convert.ToBase64String(textBytes),
            Width = 400
        };

        // Act
        var result = _controller.Resize(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Resize_ReturnsBadRequest_WhenInvalidOutputFormat()
    {
        // Arrange
        var base64 = CreateTestImageBase64(400, 300, SKEncodedImageFormat.Jpeg);
        var request = new ImageResizeRequest
        {
            FileContentBase64 = base64,
            Width = 200,
            OutputFormat = "gif" // Not supported
        };

        // Act
        var result = _controller.Resize(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Resize_AcceptsJpgAsOutputFormat()
    {
        // Arrange
        var base64 = CreateTestImageBase64(400, 300, SKEncodedImageFormat.Png);
        var request = new ImageResizeRequest
        {
            FileContentBase64 = base64,
            Width = 200,
            OutputFormat = "jpg" // Should be treated as jpeg
        };

        // Act
        var result = _controller.Resize(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as ImageResizeResponse;
        Assert.NotNull(response);
        Assert.Equal("jpeg", response.Format);
    }

    #endregion

    #region GetInfo Tests

    [Fact]
    public void GetInfo_ReturnsImageMetadata_ForJpeg()
    {
        // Arrange
        var base64 = CreateTestImageBase64(800, 600, SKEncodedImageFormat.Jpeg);
        var request = new ImageFileRequest
        {
            FileContentBase64 = base64
        };

        // Act
        var result = _controller.GetInfo(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as ImageInfoResponse;
        Assert.NotNull(response);
        Assert.Equal("jpeg", response.Format);
        Assert.Equal(800, response.Width);
        Assert.Equal(600, response.Height);
        Assert.True(response.FileSizeBytes > 0);
    }

    [Fact]
    public void GetInfo_ReturnsImageMetadata_ForPng()
    {
        // Arrange
        var base64 = CreateTestImageBase64(1024, 768, SKEncodedImageFormat.Png);
        var request = new ImageFileRequest
        {
            FileContentBase64 = base64
        };

        // Act
        var result = _controller.GetInfo(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as ImageInfoResponse;
        Assert.NotNull(response);
        Assert.Equal("png", response.Format);
        Assert.Equal(1024, response.Width);
        Assert.Equal(768, response.Height);
    }

    [Fact]
    public void GetInfo_ReturnsBadRequest_WhenFileContentIsEmpty()
    {
        // Arrange
        var request = new ImageFileRequest
        {
            FileContentBase64 = ""
        };

        // Act
        var result = _controller.GetInfo(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void GetInfo_ReturnsBadRequest_WhenInvalidImageFormat()
    {
        // Arrange
        var textBytes = System.Text.Encoding.UTF8.GetBytes("Not an image file content");
        var request = new ImageFileRequest
        {
            FileContentBase64 = Convert.ToBase64String(textBytes)
        };

        // Act
        var result = _controller.GetInfo(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    #endregion
}
