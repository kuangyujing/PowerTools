using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PowerTools.Server.Controllers;
using PowerTools.Server.Models;
using Xunit;

namespace PowerTools.Tests;

public class RegexControllerTests
{
    private readonly RegexController _controller;

    public RegexControllerTests()
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<RegexController>();
        _controller = new RegexController(logger);
    }

    #region IsMatch Tests

    [Fact]
    public void IsMatch_ValidPattern_ReturnsTrue()
    {
        // Arrange
        var request = new RegexIsMatchRequest
        {
            Input = "Hello World",
            Pattern = @"Hello"
        };

        // Act
        var result = _controller.IsMatch(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        var response = result.Value as RegexIsMatchResponse;
        Assert.NotNull(response);
        Assert.True(response.IsMatch);
    }

    [Fact]
    public void IsMatch_NoMatch_ReturnsFalse()
    {
        // Arrange
        var request = new RegexIsMatchRequest
        {
            Input = "Hello World",
            Pattern = @"Goodbye"
        };

        // Act
        var result = _controller.IsMatch(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexIsMatchResponse;
        Assert.NotNull(response);
        Assert.False(response.IsMatch);
    }

    [Fact]
    public void IsMatch_IgnoreCase_Works()
    {
        // Arrange
        var request = new RegexIsMatchRequest
        {
            Input = "Hello World",
            Pattern = @"hello",
            Options = new RegexOptionsDto { IgnoreCase = true }
        };

        // Act
        var result = _controller.IsMatch(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexIsMatchResponse;
        Assert.NotNull(response);
        Assert.True(response.IsMatch);
    }

    [Fact]
    public void IsMatch_CaseSensitive_NoMatch()
    {
        // Arrange
        var request = new RegexIsMatchRequest
        {
            Input = "Hello World",
            Pattern = @"hello",
            Options = new RegexOptionsDto { IgnoreCase = false }
        };

        // Act
        var result = _controller.IsMatch(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexIsMatchResponse;
        Assert.NotNull(response);
        Assert.False(response.IsMatch);
    }

    [Fact]
    public void IsMatch_EmailPattern_Works()
    {
        // Arrange
        var request = new RegexIsMatchRequest
        {
            Input = "Contact us at support@example.com",
            Pattern = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}"
        };

        // Act
        var result = _controller.IsMatch(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexIsMatchResponse;
        Assert.NotNull(response);
        Assert.True(response.IsMatch);
    }

    [Fact]
    public void IsMatch_InvalidPattern_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegexIsMatchRequest
        {
            Input = "Hello World",
            Pattern = @"[invalid(regex"  // Unclosed bracket
        };

        // Act
        var result = _controller.IsMatch(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void IsMatch_EmptyPattern_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegexIsMatchRequest
        {
            Input = "Hello World",
            Pattern = ""
        };

        // Act
        var result = _controller.IsMatch(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    #endregion

    #region Match Tests

    [Fact]
    public void Match_ValidPattern_ReturnsFirstMatch()
    {
        // Arrange
        var request = new RegexMatchRequest
        {
            Input = "The quick brown fox jumps over the lazy dog",
            Pattern = @"\b\w{5}\b"  // Match 5-letter words
        };

        // Act
        var result = _controller.Match(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexMatchResponse;
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("quick", response.Value);
        Assert.Equal(4, response.Index);
        Assert.Equal(5, response.Length);
    }

    [Fact]
    public void Match_WithGroups_ReturnsGroups()
    {
        // Arrange
        var request = new RegexMatchRequest
        {
            Input = "John Smith, john.smith@example.com",
            Pattern = @"(\w+)\.(\w+)@(\w+)\.(\w+)"  // Capture email parts
        };

        // Act
        var result = _controller.Match(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexMatchResponse;
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("john.smith@example.com", response.Value);
        Assert.Equal(5, response.Groups.Count);  // Full match + 4 groups
        Assert.Equal("john", response.Groups[1].Value);
        Assert.Equal("smith", response.Groups[2].Value);
        Assert.Equal("example", response.Groups[3].Value);
        Assert.Equal("com", response.Groups[4].Value);
    }

    [Fact]
    public void Match_WithNamedGroups_ReturnsNamedGroups()
    {
        // Arrange
        var request = new RegexMatchRequest
        {
            Input = "2024-01-15",
            Pattern = @"(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})"
        };

        // Act
        var result = _controller.Match(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexMatchResponse;
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("2024-01-15", response.Value);
        Assert.Contains(response.Groups, g => g.Name == "year" && g.Value == "2024");
        Assert.Contains(response.Groups, g => g.Name == "month" && g.Value == "01");
        Assert.Contains(response.Groups, g => g.Name == "day" && g.Value == "15");
    }

    [Fact]
    public void Match_NoMatch_ReturnsEmptyResult()
    {
        // Arrange
        var request = new RegexMatchRequest
        {
            Input = "Hello World",
            Pattern = @"\d+"  // No digits in input
        };

        // Act
        var result = _controller.Match(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexMatchResponse;
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Null(response.Value);
    }

    #endregion

    #region Matches Tests

    [Fact]
    public void Matches_ValidPattern_ReturnsAllMatches()
    {
        // Arrange
        var request = new RegexMatchRequest
        {
            Input = "The quick brown fox jumps over the lazy dog",
            Pattern = @"\b\w{4}\b"  // Match 4-letter words
        };

        // Act
        var result = _controller.Matches(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexMatchesResponse;
        Assert.NotNull(response);
        Assert.Equal(2, response.Count);  // over, lazy
        Assert.Contains(response.Matches, m => m.Value == "over");
        Assert.Contains(response.Matches, m => m.Value == "lazy");
    }

    [Fact]
    public void Matches_MultipleEmails_ExtractsAll()
    {
        // Arrange
        var request = new RegexMatchRequest
        {
            Input = "Contact: alice@example.com, bob@test.org, charlie@company.net",
            Pattern = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}"
        };

        // Act
        var result = _controller.Matches(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexMatchesResponse;
        Assert.NotNull(response);
        Assert.Equal(3, response.Count);
        Assert.Contains(response.Matches, m => m.Value == "alice@example.com");
        Assert.Contains(response.Matches, m => m.Value == "bob@test.org");
        Assert.Contains(response.Matches, m => m.Value == "charlie@company.net");
    }

    [Fact]
    public void Matches_NoMatches_ReturnsEmptyList()
    {
        // Arrange
        var request = new RegexMatchRequest
        {
            Input = "Hello World",
            Pattern = @"\d+"
        };

        // Act
        var result = _controller.Matches(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexMatchesResponse;
        Assert.NotNull(response);
        Assert.Equal(0, response.Count);
        Assert.Empty(response.Matches);
    }

    [Fact]
    public void Matches_JapaneseText_Works()
    {
        // Arrange
        var request = new RegexMatchRequest
        {
            Input = "電話番号: 03-1234-5678, 090-9876-5432",
            Pattern = @"\d{2,3}-\d{4}-\d{4}"
        };

        // Act
        var result = _controller.Matches(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexMatchesResponse;
        Assert.NotNull(response);
        Assert.Equal(2, response.Count);
        Assert.Contains(response.Matches, m => m.Value == "03-1234-5678");
        Assert.Contains(response.Matches, m => m.Value == "090-9876-5432");
    }

    #endregion

    #region Replace Tests

    [Fact]
    public void Replace_ValidPattern_ReplacesAll()
    {
        // Arrange
        var request = new RegexReplaceRequest
        {
            Input = "Hello World, Hello Universe",
            Pattern = @"Hello",
            Replacement = "Hi"
        };

        // Act
        var result = _controller.Replace(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexReplaceResponse;
        Assert.NotNull(response);
        Assert.Equal("Hi World, Hi Universe", response.Result);
        Assert.Equal(2, response.ReplacementCount);
    }

    [Fact]
    public void Replace_WithBackreference_Works()
    {
        // Arrange
        var request = new RegexReplaceRequest
        {
            Input = "John Smith",
            Pattern = @"(\w+)\s+(\w+)",
            Replacement = "$2, $1"  // Swap first and last name
        };

        // Act
        var result = _controller.Replace(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexReplaceResponse;
        Assert.NotNull(response);
        Assert.Equal("Smith, John", response.Result);
        Assert.Equal(1, response.ReplacementCount);
    }

    [Fact]
    public void Replace_NoMatch_ReturnsOriginal()
    {
        // Arrange
        var request = new RegexReplaceRequest
        {
            Input = "Hello World",
            Pattern = @"\d+",
            Replacement = "NUMBER"
        };

        // Act
        var result = _controller.Replace(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexReplaceResponse;
        Assert.NotNull(response);
        Assert.Equal("Hello World", response.Result);
        Assert.Equal(0, response.ReplacementCount);
    }

    [Fact]
    public void Replace_EmptyReplacement_RemovesMatches()
    {
        // Arrange
        var request = new RegexReplaceRequest
        {
            Input = "Hello123World456",
            Pattern = @"\d+",
            Replacement = ""
        };

        // Act
        var result = _controller.Replace(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexReplaceResponse;
        Assert.NotNull(response);
        Assert.Equal("HelloWorld", response.Result);
        Assert.Equal(2, response.ReplacementCount);
    }

    [Fact]
    public void Replace_DateFormat_Converts()
    {
        // Arrange - Convert YYYY-MM-DD to DD/MM/YYYY
        var request = new RegexReplaceRequest
        {
            Input = "Date: 2024-01-15",
            Pattern = @"(\d{4})-(\d{2})-(\d{2})",
            Replacement = "$3/$2/$1"
        };

        // Act
        var result = _controller.Replace(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexReplaceResponse;
        Assert.NotNull(response);
        Assert.Equal("Date: 15/01/2024", response.Result);
    }

    #endregion

    #region Split Tests

    [Fact]
    public void Split_ValidPattern_SplitsParts()
    {
        // Arrange
        var request = new RegexSplitRequest
        {
            Input = "apple,banana;cherry:date",
            Pattern = @"[,;:]"  // Split by comma, semicolon, or colon
        };

        // Act
        var result = _controller.Split(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexSplitResponse;
        Assert.NotNull(response);
        Assert.Equal(4, response.Count);
        Assert.Equal(new List<string> { "apple", "banana", "cherry", "date" }, response.Parts);
    }

    [Fact]
    public void Split_MultipleSpaces_Normalizes()
    {
        // Arrange
        var request = new RegexSplitRequest
        {
            Input = "Hello    World  Test",
            Pattern = @"\s+"  // Split by one or more whitespace
        };

        // Act
        var result = _controller.Split(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexSplitResponse;
        Assert.NotNull(response);
        Assert.Equal(3, response.Count);
        Assert.Equal(new List<string> { "Hello", "World", "Test" }, response.Parts);
    }

    [Fact]
    public void Split_NoMatch_ReturnsSingleElement()
    {
        // Arrange
        var request = new RegexSplitRequest
        {
            Input = "Hello World",
            Pattern = @"\d+"  // No digits to split on
        };

        // Act
        var result = _controller.Split(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexSplitResponse;
        Assert.NotNull(response);
        Assert.Equal(1, response.Count);
        Assert.Single(response.Parts);
        Assert.Equal("Hello World", response.Parts[0]);
    }

    [Fact]
    public void Split_CsvLine_ParsesFields()
    {
        // Arrange
        var request = new RegexSplitRequest
        {
            Input = "Name,Age,City,Country",
            Pattern = @","
        };

        // Act
        var result = _controller.Split(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexSplitResponse;
        Assert.NotNull(response);
        Assert.Equal(4, response.Count);
        Assert.Equal(new List<string> { "Name", "Age", "City", "Country" }, response.Parts);
    }

    #endregion

    #region Multiline Option Tests

    [Fact]
    public void Match_Multiline_MatchesLineStart()
    {
        // Arrange
        var request = new RegexMatchRequest
        {
            Input = "Line 1\nLine 2\nLine 3",
            Pattern = @"^Line \d",
            Options = new RegexOptionsDto { Multiline = true }
        };

        // Act
        var result = _controller.Matches(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexMatchesResponse;
        Assert.NotNull(response);
        Assert.Equal(3, response.Count);  // Matches at start of each line
    }

    [Fact]
    public void Match_WithoutMultiline_MatchesOnlyStringStart()
    {
        // Arrange
        var request = new RegexMatchRequest
        {
            Input = "Line 1\nLine 2\nLine 3",
            Pattern = @"^Line \d",
            Options = new RegexOptionsDto { Multiline = false }
        };

        // Act
        var result = _controller.Matches(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexMatchesResponse;
        Assert.NotNull(response);
        Assert.Equal(1, response.Count);  // Only matches at start of string
    }

    #endregion

    #region Singleline Option Tests

    [Fact]
    public void Match_Singleline_DotMatchesNewline()
    {
        // Arrange
        var request = new RegexMatchRequest
        {
            Input = "Start\nMiddle\nEnd",
            Pattern = @"Start.+End",
            Options = new RegexOptionsDto { Singleline = true }
        };

        // Act
        var result = _controller.Match(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexMatchResponse;
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("Start\nMiddle\nEnd", response.Value);
    }

    [Fact]
    public void Match_WithoutSingleline_DotDoesNotMatchNewline()
    {
        // Arrange
        var request = new RegexMatchRequest
        {
            Input = "Start\nMiddle\nEnd",
            Pattern = @"Start.+End",
            Options = new RegexOptionsDto { Singleline = false }
        };

        // Act
        var result = _controller.Match(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexMatchResponse;
        Assert.NotNull(response);
        Assert.False(response.Success);  // No match because . doesn't match \n
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void IsMatch_EmptyInput_Works()
    {
        // Arrange
        var request = new RegexIsMatchRequest
        {
            Input = "",
            Pattern = @"^$"  // Match empty string
        };

        // Act
        var result = _controller.IsMatch(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexIsMatchResponse;
        Assert.NotNull(response);
        Assert.True(response.IsMatch);
    }

    [Fact]
    public void Match_SpecialCharactersInInput_Works()
    {
        // Arrange
        var request = new RegexMatchRequest
        {
            Input = "Price: $100.50 (USD)",
            Pattern = @"\$(\d+\.\d{2})"
        };

        // Act
        var result = _controller.Match(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexMatchResponse;
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("$100.50", response.Value);
        Assert.Equal("100.50", response.Groups[1].Value);
    }

    [Fact]
    public void Replace_SpecialCharactersInReplacement_Works()
    {
        // Arrange
        var request = new RegexReplaceRequest
        {
            Input = "Hello World",
            Pattern = @"World",
            Replacement = "World!!!"
        };

        // Act
        var result = _controller.Replace(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as RegexReplaceResponse;
        Assert.NotNull(response);
        Assert.Equal("Hello World!!!", response.Result);
    }

    #endregion

    #region Input Validation Tests

    [Fact]
    public void IsMatch_InputExceedsMaxLength_ReturnsBadRequest()
    {
        // Arrange - Create a string longer than 1MB (1024 * 1024 characters)
        var longString = new string('a', 1024 * 1024 + 1);
        var request = new RegexIsMatchRequest
        {
            Input = longString,
            Pattern = @"test"
        };

        // Act
        var result = _controller.IsMatch(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Match_InputExceedsMaxLength_ReturnsBadRequest()
    {
        // Arrange - Create a string longer than 1MB
        var longString = new string('a', 1024 * 1024 + 1);
        var request = new RegexMatchRequest
        {
            Input = longString,
            Pattern = @"test"
        };

        // Act
        var result = _controller.Match(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Matches_InputExceedsMaxLength_ReturnsBadRequest()
    {
        // Arrange - Create a string longer than 1MB
        var longString = new string('a', 1024 * 1024 + 1);
        var request = new RegexMatchRequest
        {
            Input = longString,
            Pattern = @"test"
        };

        // Act
        var result = _controller.Matches(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Replace_InputExceedsMaxLength_ReturnsBadRequest()
    {
        // Arrange - Create a string longer than 1MB
        var longString = new string('a', 1024 * 1024 + 1);
        var request = new RegexReplaceRequest
        {
            Input = longString,
            Pattern = @"test",
            Replacement = "TEST"
        };

        // Act
        var result = _controller.Replace(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Split_InputExceedsMaxLength_ReturnsBadRequest()
    {
        // Arrange - Create a string longer than 1MB
        var longString = new string('a', 1024 * 1024 + 1);
        var request = new RegexSplitRequest
        {
            Input = longString,
            Pattern = @","
        };

        // Act
        var result = _controller.Split(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void IsMatch_NullInput_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegexIsMatchRequest
        {
            Input = null!,
            Pattern = @"test"
        };

        // Act
        var result = _controller.IsMatch(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    #endregion

    #region ReDoS Protection Tests

    [Fact]
    public void IsMatch_ComplexPatternWithTimeout_ReturnsBadRequest()
    {
        // Arrange - Pattern that could cause catastrophic backtracking
        var request = new RegexIsMatchRequest
        {
            Input = new string('a', 50) + "b",  // String designed to trigger backtracking
            Pattern = @"(a+)+$"  // Vulnerable pattern
        };

        // Act
        var result = _controller.IsMatch(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
        // Should timeout and return error message about timeout
    }

    [Fact]
    public void Match_ComplexPatternWithTimeout_ReturnsBadRequest()
    {
        // Arrange - Pattern that could cause catastrophic backtracking
        var request = new RegexMatchRequest
        {
            Input = new string('a', 50) + "b",
            Pattern = @"(a+)+$"  // Vulnerable pattern
        };

        // Act
        var result = _controller.Match(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Matches_ComplexPatternWithTimeout_ReturnsBadRequest()
    {
        // Arrange - Pattern that could cause catastrophic backtracking
        var request = new RegexMatchRequest
        {
            Input = new string('a', 50) + "b",
            Pattern = @"(a+)+$"  // Vulnerable pattern
        };

        // Act
        var result = _controller.Matches(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Replace_ComplexPatternWithTimeout_ReturnsBadRequest()
    {
        // Arrange - Pattern that could cause catastrophic backtracking
        var request = new RegexReplaceRequest
        {
            Input = new string('a', 50) + "b",
            Pattern = @"(a+)+$",  // Vulnerable pattern
            Replacement = "X"
        };

        // Act
        var result = _controller.Replace(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Split_ComplexPatternWithTimeout_ReturnsBadRequest()
    {
        // Arrange - Pattern that could cause catastrophic backtracking
        var request = new RegexSplitRequest
        {
            Input = new string('a', 50) + "b",
            Pattern = @"(a+)+$"  // Vulnerable pattern
        };

        // Act
        var result = _controller.Split(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    #endregion
}
