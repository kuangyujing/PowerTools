using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PowerTools.Server.Controllers;
using PowerTools.Server.Models;
using Xunit;

namespace PowerTools.Tests;

public class BarcodeControllerTests
{
    private readonly BarcodeController _controller;

    public BarcodeControllerTests()
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<BarcodeController>();
        _controller = new BarcodeController(logger);
    }

    #region QR Code Tests

    [Fact]
    public void GenerateQrCode_ReturnsQrCodeImage_WithDefaultSettings()
    {
        // Arrange
        var request = new QrCodeRequest
        {
            Content = "https://example.com"
        };

        // Act
        var result = _controller.GenerateQrCode(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);

        var response = result.Value as QrCodeResponse;
        Assert.NotNull(response);
        Assert.False(string.IsNullOrEmpty(response.ImageBase64));
        Assert.False(string.IsNullOrEmpty(response.DataUri));
        Assert.StartsWith("data:image/png;base64,", response.DataUri);
        Assert.Equal("png", response.Format);
        Assert.Equal(200, response.Width);
        Assert.Equal(200, response.Height);
        Assert.True(response.FileSizeBytes > 0);
    }

    [Fact]
    public void GenerateQrCode_ReturnsQrCodeImage_WithCustomSize()
    {
        // Arrange
        var request = new QrCodeRequest
        {
            Content = "Test content",
            Size = 400
        };

        // Act
        var result = _controller.GenerateQrCode(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as QrCodeResponse;
        Assert.NotNull(response);
        Assert.Equal(400, response.Width);
        Assert.Equal(400, response.Height);
    }

    [Fact]
    public void GenerateQrCode_ReturnsSvg_WhenFormatIsSvg()
    {
        // Arrange
        var request = new QrCodeRequest
        {
            Content = "SVG test",
            Format = "svg"
        };

        // Act
        var result = _controller.GenerateQrCode(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as QrCodeResponse;
        Assert.NotNull(response);
        Assert.Equal("svg", response.Format);

        // Verify it's actually SVG content
        var svgContent = System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(response.ImageBase64));
        Assert.Contains("<svg", svgContent);

        // Verify DataUri format for SVG
        Assert.StartsWith("data:image/svg+xml;base64,", response.DataUri);
    }

    [Theory]
    [InlineData("L")]
    [InlineData("M")]
    [InlineData("Q")]
    [InlineData("H")]
    public void GenerateQrCode_AcceptsAllErrorCorrectionLevels(string level)
    {
        // Arrange
        var request = new QrCodeRequest
        {
            Content = "Test",
            ErrorCorrectionLevel = level
        };

        // Act
        var result = _controller.GenerateQrCode(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public void GenerateQrCode_ReturnsBadRequest_WhenContentIsEmpty()
    {
        // Arrange
        var request = new QrCodeRequest
        {
            Content = ""
        };

        // Act
        var result = _controller.GenerateQrCode(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void GenerateQrCode_ReturnsBadRequest_WhenFormatIsInvalid()
    {
        // Arrange
        var request = new QrCodeRequest
        {
            Content = "Test",
            Format = "gif"
        };

        // Act
        var result = _controller.GenerateQrCode(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void GenerateQrCode_ReturnsBadRequest_WhenErrorCorrectionLevelIsInvalid()
    {
        // Arrange
        var request = new QrCodeRequest
        {
            Content = "Test",
            ErrorCorrectionLevel = "X"
        };

        // Act
        var result = _controller.GenerateQrCode(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    #endregion

    #region Barcode Tests

    [Fact]
    public void GenerateBarcode_ReturnsCode128Barcode()
    {
        // Arrange
        var request = new BarcodeRequest
        {
            Content = "ABC-12345",
            BarcodeType = "Code128"
        };

        // Act
        var result = _controller.GenerateBarcode(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);

        var response = result.Value as BarcodeResponse;
        Assert.NotNull(response);
        Assert.False(string.IsNullOrEmpty(response.ImageBase64));
        Assert.False(string.IsNullOrEmpty(response.DataUri));
        Assert.StartsWith("data:image/png;base64,", response.DataUri);
        Assert.Equal("png", response.Format);
        Assert.Equal("Code128", response.BarcodeType);
        Assert.True(response.FileSizeBytes > 0);
    }

    [Fact]
    public void GenerateBarcode_ReturnsCode39Barcode()
    {
        // Arrange
        var request = new BarcodeRequest
        {
            Content = "HELLO123",
            BarcodeType = "Code39"
        };

        // Act
        var result = _controller.GenerateBarcode(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as BarcodeResponse;
        Assert.NotNull(response);
        Assert.Equal("Code39", response.BarcodeType);
    }

    [Fact]
    public void GenerateBarcode_ReturnsEAN13Barcode()
    {
        // Arrange
        var request = new BarcodeRequest
        {
            Content = "4901234567894", // Valid EAN-13 (checksum digit: 4)
            BarcodeType = "EAN13"
        };

        // Act
        var result = _controller.GenerateBarcode(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as BarcodeResponse;
        Assert.NotNull(response);
        Assert.Equal("EAN13", response.BarcodeType);
    }

    [Fact]
    public void GenerateBarcode_ReturnsEAN8Barcode()
    {
        // Arrange
        var request = new BarcodeRequest
        {
            Content = "12345670", // Valid EAN-8
            BarcodeType = "EAN8"
        };

        // Act
        var result = _controller.GenerateBarcode(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as BarcodeResponse;
        Assert.NotNull(response);
        Assert.Equal("EAN8", response.BarcodeType);
    }

    [Fact]
    public void GenerateBarcode_ReturnsUpcABarcode()
    {
        // Arrange
        var request = new BarcodeRequest
        {
            Content = "012345678905", // Valid UPC-A
            BarcodeType = "UPC_A"
        };

        // Act
        var result = _controller.GenerateBarcode(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as BarcodeResponse;
        Assert.NotNull(response);
        Assert.Equal("UPC_A", response.BarcodeType);
    }

    [Fact]
    public void GenerateBarcode_ReturnsItfBarcode()
    {
        // Arrange
        var request = new BarcodeRequest
        {
            Content = "1234567890", // Even number of digits
            BarcodeType = "ITF"
        };

        // Act
        var result = _controller.GenerateBarcode(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as BarcodeResponse;
        Assert.NotNull(response);
        Assert.Equal("ITF", response.BarcodeType);
    }

    [Fact]
    public void GenerateBarcode_ReturnsCodabarBarcode()
    {
        // Arrange
        var request = new BarcodeRequest
        {
            Content = "A12345B",
            BarcodeType = "Codabar"
        };

        // Act
        var result = _controller.GenerateBarcode(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as BarcodeResponse;
        Assert.NotNull(response);
        Assert.Equal("Codabar", response.BarcodeType);
    }

    [Fact]
    public void GenerateBarcode_ReturnsCustomDimensions()
    {
        // Arrange
        var request = new BarcodeRequest
        {
            Content = "TEST123",
            BarcodeType = "Code128",
            Width = 400,
            Height = 150
        };

        // Act
        var result = _controller.GenerateBarcode(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as BarcodeResponse;
        Assert.NotNull(response);
        Assert.Equal(400, response.Width);
        Assert.Equal(150, response.Height);
    }

    [Fact]
    public void GenerateBarcode_ReturnsSvg_WhenFormatIsSvg()
    {
        // Arrange
        var request = new BarcodeRequest
        {
            Content = "SVG-TEST",
            BarcodeType = "Code128",
            Format = "svg"
        };

        // Act
        var result = _controller.GenerateBarcode(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as BarcodeResponse;
        Assert.NotNull(response);
        Assert.Equal("svg", response.Format);

        var svgContent = System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(response.ImageBase64));
        Assert.Contains("<svg", svgContent);

        // Verify DataUri format for SVG
        Assert.StartsWith("data:image/svg+xml;base64,", response.DataUri);
    }

    [Fact]
    public void GenerateBarcode_ReturnsBadRequest_WhenContentIsEmpty()
    {
        // Arrange
        var request = new BarcodeRequest
        {
            Content = "",
            BarcodeType = "Code128"
        };

        // Act
        var result = _controller.GenerateBarcode(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void GenerateBarcode_ReturnsBadRequest_WhenBarcodeTypeIsEmpty()
    {
        // Arrange
        var request = new BarcodeRequest
        {
            Content = "Test",
            BarcodeType = ""
        };

        // Act
        var result = _controller.GenerateBarcode(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void GenerateBarcode_ReturnsBadRequest_WhenBarcodeTypeIsUnsupported()
    {
        // Arrange
        var request = new BarcodeRequest
        {
            Content = "Test",
            BarcodeType = "InvalidType"
        };

        // Act
        var result = _controller.GenerateBarcode(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void GenerateBarcode_ReturnsBadRequest_WhenEAN13HasInvalidLength()
    {
        // Arrange
        var request = new BarcodeRequest
        {
            Content = "12345", // Wrong length for EAN-13
            BarcodeType = "EAN13"
        };

        // Act
        var result = _controller.GenerateBarcode(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void GenerateBarcode_ReturnsBadRequest_WhenEAN8HasNonDigits()
    {
        // Arrange
        var request = new BarcodeRequest
        {
            Content = "1234567A", // Contains non-digit
            BarcodeType = "EAN8"
        };

        // Act
        var result = _controller.GenerateBarcode(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void GenerateBarcode_ReturnsBadRequest_WhenITFHasOddDigits()
    {
        // Arrange
        var request = new BarcodeRequest
        {
            Content = "12345", // Odd number of digits
            BarcodeType = "ITF"
        };

        // Act
        var result = _controller.GenerateBarcode(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void GenerateBarcode_ReturnsBadRequest_WhenFormatIsInvalid()
    {
        // Arrange
        var request = new BarcodeRequest
        {
            Content = "Test",
            BarcodeType = "Code128",
            Format = "gif"
        };

        // Act
        var result = _controller.GenerateBarcode(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void GenerateBarcode_IsCaseInsensitiveForBarcodeType()
    {
        // Arrange
        var request = new BarcodeRequest
        {
            Content = "TEST",
            BarcodeType = "code128" // lowercase
        };

        // Act
        var result = _controller.GenerateBarcode(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    #endregion

    #region GetSupportedTypes Tests

    [Fact]
    public void GetSupportedTypes_ReturnsListOfBarcodeTypes()
    {
        // Act
        var result = _controller.GetSupportedTypes() as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var types = result.Value as List<string>;
        Assert.NotNull(types);
        Assert.Contains("Code128", types);
        Assert.Contains("Code39", types);
        Assert.Contains("EAN13", types);
        Assert.Contains("EAN8", types);
        Assert.Contains("UPC_A", types);
        Assert.Contains("ITF", types);
        Assert.Contains("Codabar", types);
    }

    #endregion
}
