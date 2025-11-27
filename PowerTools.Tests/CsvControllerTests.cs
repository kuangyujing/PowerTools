using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PowerTools.Server.Controllers;
using PowerTools.Server.Models;
using Xunit;

namespace PowerTools.Tests;

public class CsvControllerTests
{
    private readonly CsvController _controller;

    public CsvControllerTests()
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<CsvController>();
        _controller = new CsvController(logger);
    }

    #region Parse Tests

    [Fact]
    public void Parse_BasicCsv_ReturnsJsonData()
    {
        // Arrange
        var csv = "Name,Age,City\nAlice,30,Tokyo\nBob,25,Osaka";
        var request = new CsvParseRequest
        {
            Content = csv,
            HasHeader = true
        };

        // Act
        var result = _controller.Parse(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);

        var response = result.Value as CsvParseResponse;
        Assert.NotNull(response);
        Assert.Equal(3, response.Headers.Count);
        Assert.Contains("Name", response.Headers);
        Assert.Contains("Age", response.Headers);
        Assert.Contains("City", response.Headers);
        Assert.Equal(2, response.RowCount);
        Assert.Equal("Alice", response.Rows[0]["Name"]);
        Assert.Equal("30", response.Rows[0]["Age"]);
        Assert.Equal("Tokyo", response.Rows[0]["City"]);
        Assert.Equal("Bob", response.Rows[1]["Name"]);
    }

    [Fact]
    public void Parse_NoHeader_GeneratesColumnNames()
    {
        // Arrange
        var csv = "Alice,30,Tokyo\nBob,25,Osaka";
        var request = new CsvParseRequest
        {
            Content = csv,
            HasHeader = false
        };

        // Act
        var result = _controller.Parse(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CsvParseResponse;
        Assert.NotNull(response);
        Assert.Equal(3, response.Headers.Count);
        Assert.Equal("Column1", response.Headers[0]);
        Assert.Equal("Column2", response.Headers[1]);
        Assert.Equal("Column3", response.Headers[2]);
        Assert.Equal(2, response.RowCount);
        Assert.Equal("Alice", response.Rows[0]["Column1"]);
    }

    [Fact]
    public void Parse_CustomDelimiter_ParsesCorrectly()
    {
        // Arrange
        var csv = "Name;Age;City\nAlice;30;Tokyo";
        var request = new CsvParseRequest
        {
            Content = csv,
            Delimiter = ";",
            HasHeader = true
        };

        // Act
        var result = _controller.Parse(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CsvParseResponse;
        Assert.NotNull(response);
        Assert.Equal(3, response.Headers.Count);
        Assert.Equal("Alice", response.Rows[0]["Name"]);
        Assert.Equal("Tokyo", response.Rows[0]["City"]);
    }

    [Fact]
    public void Parse_TabDelimiter_ParsesCorrectly()
    {
        // Arrange
        var csv = "Name\tAge\tCity\nAlice\t30\tTokyo";
        var request = new CsvParseRequest
        {
            Content = csv,
            Delimiter = "\t",
            HasHeader = true
        };

        // Act
        var result = _controller.Parse(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CsvParseResponse;
        Assert.NotNull(response);
        Assert.Equal("Alice", response.Rows[0]["Name"]);
        Assert.Equal("Tokyo", response.Rows[0]["City"]);
    }

    [Fact]
    public void Parse_QuotedFields_HandlesCorrectly()
    {
        // Arrange
        var csv = "Name,Description\nAlice,\"Hello, World\"\nBob,\"Line1\nLine2\"";
        var request = new CsvParseRequest
        {
            Content = csv,
            HasHeader = true
        };

        // Act
        var result = _controller.Parse(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CsvParseResponse;
        Assert.NotNull(response);
        Assert.Equal(2, response.RowCount);
        Assert.Equal("Hello, World", response.Rows[0]["Description"]);
        Assert.Equal("Line1\nLine2", response.Rows[1]["Description"]);
    }

    [Fact]
    public void Parse_EscapedQuotes_HandlesCorrectly()
    {
        // Arrange
        var csv = "Name,Quote\nAlice,\"She said \"\"Hello\"\"\"";
        var request = new CsvParseRequest
        {
            Content = csv,
            HasHeader = true
        };

        // Act
        var result = _controller.Parse(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CsvParseResponse;
        Assert.NotNull(response);
        Assert.Equal("She said \"Hello\"", response.Rows[0]["Quote"]);
    }

    [Fact]
    public void Parse_Base64Content_DecodesAndParses()
    {
        // Arrange
        var csv = "Name,Age\nAlice,30";
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(csv));
        var request = new CsvParseRequest
        {
            Content = base64,
            IsBase64 = true,
            HasHeader = true
        };

        // Act
        var result = _controller.Parse(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CsvParseResponse;
        Assert.NotNull(response);
        Assert.Equal("Alice", response.Rows[0]["Name"]);
        Assert.Equal("30", response.Rows[0]["Age"]);
    }

    [Fact]
    public void Parse_JapaneseContent_HandlesCorrectly()
    {
        // Arrange
        var csv = "名前,年齢,都市\n田中太郎,30,東京\n山田花子,25,大阪";
        var request = new CsvParseRequest
        {
            Content = csv,
            HasHeader = true
        };

        // Act
        var result = _controller.Parse(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CsvParseResponse;
        Assert.NotNull(response);
        Assert.Contains("名前", response.Headers);
        Assert.Equal("田中太郎", response.Rows[0]["名前"]);
        Assert.Equal("東京", response.Rows[0]["都市"]);
    }

    [Fact]
    public void Parse_EmptyRows_SkipsWhenEnabled()
    {
        // Arrange
        var csv = "Name,Age\nAlice,30\n\nBob,25";
        var request = new CsvParseRequest
        {
            Content = csv,
            HasHeader = true,
            SkipEmptyRows = true
        };

        // Act
        var result = _controller.Parse(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CsvParseResponse;
        Assert.NotNull(response);
        Assert.Equal(2, response.RowCount);
    }

    [Fact]
    public void Parse_TrimValues_TrimsWhitespace()
    {
        // Arrange
        var csv = "Name,Age\n  Alice  ,  30  ";
        var request = new CsvParseRequest
        {
            Content = csv,
            HasHeader = true,
            TrimValues = true
        };

        // Act
        var result = _controller.Parse(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CsvParseResponse;
        Assert.NotNull(response);
        Assert.Equal("Alice", response.Rows[0]["Name"]);
        Assert.Equal("30", response.Rows[0]["Age"]);
    }

    [Fact]
    public void Parse_UnequalColumns_HandlesGracefully()
    {
        // Arrange
        var csv = "A,B,C\n1,2\n1,2,3,4";
        var request = new CsvParseRequest
        {
            Content = csv,
            HasHeader = true
        };

        // Act
        var result = _controller.Parse(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CsvParseResponse;
        Assert.NotNull(response);
        Assert.Equal(4, response.ColumnCount);  // Maximum columns found
        Assert.Null(response.Rows[0]["C"]);      // Missing value is null
    }

    [Fact]
    public void Parse_WindowsLineEndings_ParsesCorrectly()
    {
        // Arrange
        var csv = "Name,Age\r\nAlice,30\r\nBob,25";
        var request = new CsvParseRequest
        {
            Content = csv,
            HasHeader = true
        };

        // Act
        var result = _controller.Parse(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CsvParseResponse;
        Assert.NotNull(response);
        Assert.Equal(2, response.RowCount);
        Assert.Equal("Alice", response.Rows[0]["Name"]);
    }

    [Fact]
    public void Parse_EmptyContent_ReturnsBadRequest()
    {
        // Arrange
        var request = new CsvParseRequest
        {
            Content = "",
            HasHeader = true
        };

        // Act
        var result = _controller.Parse(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Parse_InvalidBase64_ReturnsBadRequest()
    {
        // Arrange
        var request = new CsvParseRequest
        {
            Content = "not-valid-base64!!!",
            IsBase64 = true,
            HasHeader = true
        };

        // Act
        var result = _controller.Parse(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Parse_EmptyDelimiter_ReturnsBadRequest()
    {
        // Arrange
        var request = new CsvParseRequest
        {
            Content = "Name,Age\nAlice,30",
            Delimiter = "",
            HasHeader = true
        };

        // Act
        var result = _controller.Parse(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Parse_MultiCharDelimiter_ReturnsBadRequest()
    {
        // Arrange
        var request = new CsvParseRequest
        {
            Content = "Name,Age\nAlice,30",
            Delimiter = ",,",
            HasHeader = true
        };

        // Act
        var result = _controller.Parse(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    #endregion

    #region Generate Tests

    [Fact]
    public void Generate_BasicData_CreatesCsv()
    {
        // Arrange
        var request = new CsvGenerateRequest
        {
            Headers = new List<string> { "Name", "Age", "City" },
            Rows = new List<Dictionary<string, object?>>
            {
                new() { { "Name", "Alice" }, { "Age", 30 }, { "City", "Tokyo" } },
                new() { { "Name", "Bob" }, { "Age", 25 }, { "City", "Osaka" } }
            },
            IncludeHeader = true
        };

        // Act
        var result = _controller.Generate(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);

        var response = result.Value as CsvGenerateResponse;
        Assert.NotNull(response);
        Assert.Equal(2, response.RowCount);
        Assert.Equal(3, response.ColumnCount);
        Assert.Contains("Name,Age,City", response.Content);
        Assert.Contains("Alice,30,Tokyo", response.Content);
        Assert.Contains("Bob,25,Osaka", response.Content);
    }

    [Fact]
    public void Generate_NoHeader_ExcludesHeaderRow()
    {
        // Arrange
        var request = new CsvGenerateRequest
        {
            Headers = new List<string> { "Name", "Age" },
            Rows = new List<Dictionary<string, object?>>
            {
                new() { { "Name", "Alice" }, { "Age", 30 } }
            },
            IncludeHeader = false
        };

        // Act
        var result = _controller.Generate(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CsvGenerateResponse;
        Assert.NotNull(response);
        Assert.DoesNotContain("Name,Age\r\n", response.Content.Split("Alice")[0]);
        Assert.Contains("Alice,30", response.Content);
    }

    [Fact]
    public void Generate_CustomDelimiter_UsesDelimiter()
    {
        // Arrange
        var request = new CsvGenerateRequest
        {
            Headers = new List<string> { "Name", "Age" },
            Rows = new List<Dictionary<string, object?>>
            {
                new() { { "Name", "Alice" }, { "Age", 30 } }
            },
            Delimiter = ";"
        };

        // Act
        var result = _controller.Generate(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CsvGenerateResponse;
        Assert.NotNull(response);
        Assert.Contains("Name;Age", response.Content);
        Assert.Contains("Alice;30", response.Content);
    }

    [Fact]
    public void Generate_FieldsWithDelimiter_QuotesFields()
    {
        // Arrange
        var request = new CsvGenerateRequest
        {
            Headers = new List<string> { "Name", "Description" },
            Rows = new List<Dictionary<string, object?>>
            {
                new() { { "Name", "Alice" }, { "Description", "Hello, World" } }
            }
        };

        // Act
        var result = _controller.Generate(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CsvGenerateResponse;
        Assert.NotNull(response);
        Assert.Contains("\"Hello, World\"", response.Content);
    }

    [Fact]
    public void Generate_FieldsWithQuotes_EscapesQuotes()
    {
        // Arrange
        var request = new CsvGenerateRequest
        {
            Headers = new List<string> { "Name", "Quote" },
            Rows = new List<Dictionary<string, object?>>
            {
                new() { { "Name", "Alice" }, { "Quote", "She said \"Hello\"" } }
            }
        };

        // Act
        var result = _controller.Generate(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CsvGenerateResponse;
        Assert.NotNull(response);
        Assert.Contains("\"She said \"\"Hello\"\"\"", response.Content);
    }

    [Fact]
    public void Generate_FieldsWithNewlines_QuotesFields()
    {
        // Arrange
        var request = new CsvGenerateRequest
        {
            Headers = new List<string> { "Name", "Address" },
            Rows = new List<Dictionary<string, object?>>
            {
                new() { { "Name", "Alice" }, { "Address", "Line1\nLine2" } }
            }
        };

        // Act
        var result = _controller.Generate(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CsvGenerateResponse;
        Assert.NotNull(response);
        Assert.Contains("\"Line1\nLine2\"", response.Content);
    }

    [Fact]
    public void Generate_UnixLineEndings_UsesLF()
    {
        // Arrange
        var request = new CsvGenerateRequest
        {
            Headers = new List<string> { "Name" },
            Rows = new List<Dictionary<string, object?>>
            {
                new() { { "Name", "Alice" } },
                new() { { "Name", "Bob" } }
            },
            LineEnding = "lf"
        };

        // Act
        var result = _controller.Generate(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CsvGenerateResponse;
        Assert.NotNull(response);
        Assert.DoesNotContain("\r\n", response.Content);
        Assert.Contains("\n", response.Content);
    }

    [Fact]
    public void Generate_WindowsLineEndings_UsesCRLF()
    {
        // Arrange
        var request = new CsvGenerateRequest
        {
            Headers = new List<string> { "Name" },
            Rows = new List<Dictionary<string, object?>>
            {
                new() { { "Name", "Alice" } },
                new() { { "Name", "Bob" } }
            },
            LineEnding = "crlf"
        };

        // Act
        var result = _controller.Generate(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CsvGenerateResponse;
        Assert.NotNull(response);
        Assert.Contains("\r\n", response.Content);
    }

    [Fact]
    public void Generate_OutputBase64_EncodesContent()
    {
        // Arrange
        var request = new CsvGenerateRequest
        {
            Headers = new List<string> { "Name", "Age" },
            Rows = new List<Dictionary<string, object?>>
            {
                new() { { "Name", "Alice" }, { "Age", 30 } }
            },
            OutputBase64 = true
        };

        // Act
        var result = _controller.Generate(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CsvGenerateResponse;
        Assert.NotNull(response);
        Assert.True(response.IsBase64);

        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(response.Content));
        Assert.Contains("Name,Age", decoded);
        Assert.Contains("Alice,30", decoded);
    }

    [Fact]
    public void Generate_JapaneseContent_HandlesCorrectly()
    {
        // Arrange
        var request = new CsvGenerateRequest
        {
            Headers = new List<string> { "名前", "都市" },
            Rows = new List<Dictionary<string, object?>>
            {
                new() { { "名前", "田中太郎" }, { "都市", "東京" } }
            }
        };

        // Act
        var result = _controller.Generate(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CsvGenerateResponse;
        Assert.NotNull(response);
        Assert.Contains("名前,都市", response.Content);
        Assert.Contains("田中太郎,東京", response.Content);
    }

    [Fact]
    public void Generate_NoHeaders_UsesKeysFromFirstRow()
    {
        // Arrange
        var request = new CsvGenerateRequest
        {
            Headers = null,
            Rows = new List<Dictionary<string, object?>>
            {
                new() { { "Name", "Alice" }, { "Age", 30 } },
                new() { { "Name", "Bob" }, { "Age", 25 } }
            }
        };

        // Act
        var result = _controller.Generate(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CsvGenerateResponse;
        Assert.NotNull(response);
        Assert.Equal(2, response.ColumnCount);
    }

    [Fact]
    public void Generate_MissingValues_OutputsEmptyField()
    {
        // Arrange
        var request = new CsvGenerateRequest
        {
            Headers = new List<string> { "Name", "Age", "City" },
            Rows = new List<Dictionary<string, object?>>
            {
                new() { { "Name", "Alice" }, { "City", "Tokyo" } }  // Age is missing
            }
        };

        // Act
        var result = _controller.Generate(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CsvGenerateResponse;
        Assert.NotNull(response);
        Assert.Contains("Alice,,Tokyo", response.Content);
    }

    [Fact]
    public void Generate_NullValues_OutputsEmptyField()
    {
        // Arrange
        var request = new CsvGenerateRequest
        {
            Headers = new List<string> { "Name", "Age" },
            Rows = new List<Dictionary<string, object?>>
            {
                new() { { "Name", "Alice" }, { "Age", null } }
            }
        };

        // Act
        var result = _controller.Generate(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CsvGenerateResponse;
        Assert.NotNull(response);
        Assert.Contains("Alice,", response.Content);
    }

    [Fact]
    public void Generate_EmptyRows_ReturnsBadRequest()
    {
        // Arrange
        var request = new CsvGenerateRequest
        {
            Headers = new List<string> { "Name", "Age" },
            Rows = new List<Dictionary<string, object?>>()
        };

        // Act
        var result = _controller.Generate(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Generate_EmptyDelimiter_ReturnsBadRequest()
    {
        // Arrange
        var request = new CsvGenerateRequest
        {
            Headers = new List<string> { "Name", "Age" },
            Rows = new List<Dictionary<string, object?>>
            {
                new() { { "Name", "Alice" }, { "Age", 30 } }
            },
            Delimiter = ""
        };

        // Act
        var result = _controller.Generate(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public void ParseAndGenerate_RoundTrip_PreservesData()
    {
        // Arrange
        var originalCsv = "Name,Age,City\r\nAlice,30,Tokyo\r\nBob,25,Osaka\r\n";
        var parseRequest = new CsvParseRequest
        {
            Content = originalCsv,
            HasHeader = true
        };

        // Act - Parse
        var parseResult = _controller.Parse(parseRequest) as OkObjectResult;
        Assert.NotNull(parseResult);
        var parseResponse = parseResult.Value as CsvParseResponse;
        Assert.NotNull(parseResponse);

        // Convert to generate request
        var generateRequest = new CsvGenerateRequest
        {
            Headers = parseResponse.Headers,
            Rows = parseResponse.Rows.Select(r =>
                r.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value)).ToList(),
            IncludeHeader = true,
            LineEnding = "crlf"
        };

        // Act - Generate
        var generateResult = _controller.Generate(generateRequest) as OkObjectResult;
        Assert.NotNull(generateResult);
        var generateResponse = generateResult.Value as CsvGenerateResponse;
        Assert.NotNull(generateResponse);

        // Assert - Content should match
        Assert.Equal(originalCsv, generateResponse.Content);
    }

    #endregion
}
