using System;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PowerTools.Server.Controllers;
using PowerTools.Server.Models;
using PowerTools.Server.Services;
using Xunit;

namespace PowerTools.Tests;

public class EncodingConverterControllerTests
{
    private readonly EncodingConverterController _controller;
    private readonly EncodingDetectionService _service;

    public EncodingConverterControllerTests()
    {
        // Register code pages encoding provider for Shift_JIS, EUC-JP, etc.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var serviceLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<EncodingDetectionService>();
        var controllerLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<EncodingConverterController>();

        _service = new EncodingDetectionService(serviceLogger);
        _controller = new EncodingConverterController(_service, controllerLogger);
    }

    [Fact]
    public void ConvertEncoding_UTF8ToShiftJIS_Success()
    {
        // Arrange
        var testText = "こんにちは、世界！Hello World!";
        var utf8Bytes = Encoding.UTF8.GetBytes(testText);
        var request = new EncodingConversionRequest
        {
            FileContentBase64 = Convert.ToBase64String(utf8Bytes),
            FileName = "test.txt",
            InputEncoding = "UTF-8",
            OutputEncoding = "Shift_JIS"
        };

        // Act
        var result = _controller.ConvertEncoding(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);

        var response = result.Value as EncodingConversionResponse;
        Assert.NotNull(response);
        Assert.Equal("test.txt", response.FileName);
        Assert.Equal("utf-8", response.DetectedEncoding);
        Assert.Equal("shift_jis", response.OutputEncoding);
        Assert.True(response.FileSizeBytes > 0);

        // Verify the converted content
        var convertedBytes = Convert.FromBase64String(response.FileContentBase64);
        var convertedText = Encoding.GetEncoding("Shift_JIS").GetString(convertedBytes);
        Assert.Equal(testText, convertedText);
    }

    [Fact]
    public void ConvertEncoding_AutoDetectUTF8_Success()
    {
        // Arrange
        var testText = "This is a UTF-8 test file with some content.";
        var utf8Bytes = Encoding.UTF8.GetBytes(testText);
        var request = new EncodingConversionRequest
        {
            FileContentBase64 = Convert.ToBase64String(utf8Bytes),
            FileName = "test.txt",
            InputEncoding = null, // Auto-detect
            OutputEncoding = "UTF-16"
        };

        // Act
        var result = _controller.ConvertEncoding(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as EncodingConversionResponse;
        Assert.NotNull(response);
        Assert.Equal("utf-16", response.OutputEncoding);
        Assert.NotNull(response.DetectionConfidence);
    }

    [Fact]
    public void ConvertEncoding_UTF8BOM_Success()
    {
        // Arrange
        var testText = "Test with BOM";
        var utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

        // Manually add BOM (preamble) to the bytes
        var preamble = utf8WithBom.GetPreamble();
        var textBytes = utf8WithBom.GetBytes(testText);
        var utf8Bytes = new byte[preamble.Length + textBytes.Length];
        Array.Copy(preamble, 0, utf8Bytes, 0, preamble.Length);
        Array.Copy(textBytes, 0, utf8Bytes, preamble.Length, textBytes.Length);

        var request = new EncodingConversionRequest
        {
            FileContentBase64 = Convert.ToBase64String(utf8Bytes),
            FileName = "test_bom.txt",
            InputEncoding = null, // Should detect BOM
            OutputEncoding = "UTF-8"
        };

        // Act
        var result = _controller.ConvertEncoding(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as EncodingConversionResponse;
        Assert.NotNull(response);
        Assert.Equal("utf-8", response.DetectedEncoding);
        Assert.Equal(1.0, response.DetectionConfidence); // BOM detection has 100% confidence
    }

    [Fact]
    public void ConvertEncoding_InvalidBase64_ReturnsBadRequest()
    {
        // Arrange
        var request = new EncodingConversionRequest
        {
            FileContentBase64 = "This is not valid base64!!!",
            FileName = "test.txt",
            OutputEncoding = "UTF-8"
        };

        // Act
        var result = _controller.ConvertEncoding(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void ConvertEncoding_EmptyFile_ReturnsBadRequest()
    {
        // Arrange
        var request = new EncodingConversionRequest
        {
            FileContentBase64 = Convert.ToBase64String(Array.Empty<byte>()),
            FileName = "empty.txt",
            OutputEncoding = "UTF-8"
        };

        // Act
        var result = _controller.ConvertEncoding(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void ConvertEncoding_UnsupportedEncoding_ReturnsBadRequest()
    {
        // Arrange
        var testText = "Test content";
        var utf8Bytes = Encoding.UTF8.GetBytes(testText);
        var request = new EncodingConversionRequest
        {
            FileContentBase64 = Convert.ToBase64String(utf8Bytes),
            FileName = "test.txt",
            OutputEncoding = "INVALID-ENCODING"
        };

        // Act
        var result = _controller.ConvertEncoding(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void ConvertEncoding_BinaryFile_ReturnsBadRequest()
    {
        // Arrange - Create binary file with many null bytes
        var binaryBytes = new byte[1000];
        for (int i = 0; i < 600; i++)
        {
            binaryBytes[i] = 0x00; // Null bytes
        }

        var request = new EncodingConversionRequest
        {
            FileContentBase64 = Convert.ToBase64String(binaryBytes),
            FileName = "binary.bin",
            OutputEncoding = "UTF-8"
        };

        // Act
        var result = _controller.ConvertEncoding(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void ConvertEncoding_JapaneseText_ShiftJISToUTF8_Success()
    {
        // Arrange
        var testText = "日本語のテストファイルです。\nエンコーディング変換のテスト。";
        var shiftJisBytes = Encoding.GetEncoding("Shift_JIS").GetBytes(testText);
        var request = new EncodingConversionRequest
        {
            FileContentBase64 = Convert.ToBase64String(shiftJisBytes),
            FileName = "japanese.txt",
            InputEncoding = "Shift_JIS",
            OutputEncoding = "UTF-8"
        };

        // Act
        var result = _controller.ConvertEncoding(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as EncodingConversionResponse;
        Assert.NotNull(response);

        var convertedBytes = Convert.FromBase64String(response.FileContentBase64);
        var convertedText = Encoding.UTF8.GetString(convertedBytes);
        Assert.Equal(testText, convertedText);
    }

    [Fact]
    public void ConvertEncoding_CSVFile_Success()
    {
        // Arrange
        var csvContent = "名前,年齢,住所\n山田太郎,30,東京都\n佐藤花子,25,大阪府";
        var utf8Bytes = Encoding.UTF8.GetBytes(csvContent);
        var request = new EncodingConversionRequest
        {
            FileContentBase64 = Convert.ToBase64String(utf8Bytes),
            FileName = "data.csv",
            InputEncoding = "UTF-8",
            OutputEncoding = "Shift_JIS"
        };

        // Act
        var result = _controller.ConvertEncoding(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as EncodingConversionResponse;
        Assert.NotNull(response);
        Assert.Equal("data.csv", response.FileName);

        var convertedBytes = Convert.FromBase64String(response.FileContentBase64);
        var convertedText = Encoding.GetEncoding("Shift_JIS").GetString(convertedBytes);
        Assert.Equal(csvContent, convertedText);
    }

    [Fact]
    public void GetSupportedEncodings_ReturnsEncodingList()
    {
        // Act
        var result = _controller.GetSupportedEncodings() as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);

        var response = result.Value as EncodingListResponse;
        Assert.NotNull(response);
        Assert.NotEmpty(response.Encodings);
    }
}
