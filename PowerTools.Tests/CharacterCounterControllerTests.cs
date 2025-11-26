using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PowerTools.Server.Controllers;
using PowerTools.Server.Models;
using Xunit;

namespace PowerTools.Tests;

public class CharacterCounterControllerTests
{
    private readonly CharacterCounterController _controller;

    public CharacterCounterControllerTests()
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<CharacterCounterController>();
        _controller = new CharacterCounterController(logger);
    }

    #region Count - Success Cases

    [Fact]
    public void Count_AlphabetOnly_ReturnsCorrectCounts()
    {
        // Arrange
        var request = new CharacterCountRequest
        {
            Input = "HelloWorld"
        };

        // Act
        var result = _controller.Count(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);

        var response = result.Value as CharacterCountResponse;
        Assert.NotNull(response);
        Assert.Equal(10, response.TotalCount);
        Assert.Equal(10, response.AlphabetCount);
        Assert.Equal(0, response.DigitCount);
        Assert.Equal(0, response.SpaceCount);
        Assert.Equal(0, response.TabCount);
        Assert.Equal(0, response.KanjiCount);
        Assert.Equal(0, response.HiraganaCount);
        Assert.Equal(0, response.KatakanaCount);
        Assert.Equal(0, response.OtherCount);
    }

    [Fact]
    public void Count_DigitsOnly_ReturnsCorrectCounts()
    {
        // Arrange
        var request = new CharacterCountRequest
        {
            Input = "1234567890"
        };

        // Act
        var result = _controller.Count(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CharacterCountResponse;
        Assert.NotNull(response);
        Assert.Equal(10, response.TotalCount);
        Assert.Equal(0, response.AlphabetCount);
        Assert.Equal(10, response.DigitCount);
    }

    [Fact]
    public void Count_AlphabetAndDigits_ReturnsCorrectCounts()
    {
        // Arrange
        var request = new CharacterCountRequest
        {
            Input = "abc123XYZ789"
        };

        // Act
        var result = _controller.Count(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CharacterCountResponse;
        Assert.NotNull(response);
        Assert.Equal(12, response.TotalCount);
        Assert.Equal(6, response.AlphabetCount);
        Assert.Equal(6, response.DigitCount);
    }

    [Fact]
    public void Count_SpacesAndTabs_ReturnsCorrectCounts()
    {
        // Arrange
        var request = new CharacterCountRequest
        {
            Input = "Hello World\tTest"
        };

        // Act
        var result = _controller.Count(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CharacterCountResponse;
        Assert.NotNull(response);
        Assert.Equal(16, response.TotalCount);
        Assert.Equal(14, response.AlphabetCount);  // HelloWorldTest
        Assert.Equal(1, response.SpaceCount);
        Assert.Equal(1, response.TabCount);
    }

    [Fact]
    public void Count_HiraganaOnly_ReturnsCorrectCounts()
    {
        // Arrange
        var request = new CharacterCountRequest
        {
            Input = "こんにちは"
        };

        // Act
        var result = _controller.Count(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CharacterCountResponse;
        Assert.NotNull(response);
        Assert.Equal(5, response.TotalCount);
        Assert.Equal(5, response.HiraganaCount);
        Assert.Equal(0, response.KatakanaCount);
        Assert.Equal(0, response.KanjiCount);
    }

    [Fact]
    public void Count_KatakanaOnly_ReturnsCorrectCounts()
    {
        // Arrange
        var request = new CharacterCountRequest
        {
            Input = "カタカナ"
        };

        // Act
        var result = _controller.Count(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CharacterCountResponse;
        Assert.NotNull(response);
        Assert.Equal(4, response.TotalCount);
        Assert.Equal(4, response.KatakanaCount);
        Assert.Equal(0, response.HiraganaCount);
        Assert.Equal(0, response.KanjiCount);
    }

    [Fact]
    public void Count_KanjiOnly_ReturnsCorrectCounts()
    {
        // Arrange
        var request = new CharacterCountRequest
        {
            Input = "日本語漢字"
        };

        // Act
        var result = _controller.Count(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CharacterCountResponse;
        Assert.NotNull(response);
        Assert.Equal(5, response.TotalCount);
        Assert.Equal(5, response.KanjiCount);
        Assert.Equal(0, response.HiraganaCount);
        Assert.Equal(0, response.KatakanaCount);
    }

    [Fact]
    public void Count_MixedJapanese_ReturnsCorrectCounts()
    {
        // Arrange
        var request = new CharacterCountRequest
        {
            Input = "日本語テストです"  // Kanji: 日本語 (3), Katakana: テスト (3), Hiragana: です (2)
        };

        // Act
        var result = _controller.Count(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CharacterCountResponse;
        Assert.NotNull(response);
        Assert.Equal(8, response.TotalCount);
        Assert.Equal(3, response.KanjiCount);
        Assert.Equal(3, response.KatakanaCount);
        Assert.Equal(2, response.HiraganaCount);
    }

    [Fact]
    public void Count_MixedEnglishAndJapanese_ReturnsCorrectCounts()
    {
        // Arrange
        var request = new CharacterCountRequest
        {
            Input = "Hello こんにちは World"  // Total: 5 + 5 + 5 + 2 spaces = 17
        };

        // Act
        var result = _controller.Count(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CharacterCountResponse;
        Assert.NotNull(response);
        Assert.Equal(17, response.TotalCount);
        Assert.Equal(10, response.AlphabetCount);  // HelloWorld
        Assert.Equal(5, response.HiraganaCount);   // こんにちは
        Assert.Equal(2, response.SpaceCount);
    }

    [Fact]
    public void Count_ComplexMixedString_ReturnsCorrectCounts()
    {
        // Arrange
        var request = new CharacterCountRequest
        {
            Input = "Test123 日本語\tカタカナ ひらがな!"  // 4+3+1+3+1+4+1+4+1 = 22
        };

        // Act
        var result = _controller.Count(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CharacterCountResponse;
        Assert.NotNull(response);
        Assert.Equal(22, response.TotalCount);
        Assert.Equal(4, response.AlphabetCount);   // Test
        Assert.Equal(3, response.DigitCount);      // 123
        Assert.Equal(2, response.SpaceCount);
        Assert.Equal(1, response.TabCount);
        Assert.Equal(3, response.KanjiCount);      // 日本語
        Assert.Equal(4, response.KatakanaCount);   // カタカナ
        Assert.Equal(4, response.HiraganaCount);   // ひらがな
        Assert.Equal(1, response.OtherCount);      // !
    }

    #endregion

    #region Count - Edge Cases

    [Fact]
    public void Count_EmptyString_ReturnsZeroCounts()
    {
        // Arrange
        var request = new CharacterCountRequest
        {
            Input = ""
        };

        // Act
        var result = _controller.Count(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CharacterCountResponse;
        Assert.NotNull(response);
        Assert.Equal(0, response.TotalCount);
        Assert.Equal(0, response.AlphabetCount);
        Assert.Equal(0, response.DigitCount);
        Assert.Equal(0, response.SpaceCount);
        Assert.Equal(0, response.TabCount);
        Assert.Equal(0, response.KanjiCount);
        Assert.Equal(0, response.HiraganaCount);
        Assert.Equal(0, response.KatakanaCount);
        Assert.Equal(0, response.OtherCount);
    }

    [Fact]
    public void Count_SpecialCharacters_CountsAsOther()
    {
        // Arrange
        var request = new CharacterCountRequest
        {
            Input = "!@#$%^&*()"
        };

        // Act
        var result = _controller.Count(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CharacterCountResponse;
        Assert.NotNull(response);
        Assert.Equal(10, response.TotalCount);
        Assert.Equal(10, response.OtherCount);
    }

    [Fact]
    public void Count_Newlines_CountsAsOther()
    {
        // Arrange
        var request = new CharacterCountRequest
        {
            Input = "Line1\nLine2\r\nLine3"
        };

        // Act
        var result = _controller.Count(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CharacterCountResponse;
        Assert.NotNull(response);
        Assert.Equal(18, response.TotalCount);
        Assert.Equal(12, response.AlphabetCount);  // Line1Line2Line3
        Assert.Equal(3, response.DigitCount);      // 1, 2, 3
        Assert.Equal(3, response.OtherCount);      // \n, \r, \n
    }

    [Fact]
    public void Count_OnlySpaces_ReturnsCorrectCounts()
    {
        // Arrange
        var request = new CharacterCountRequest
        {
            Input = "     "  // 5 spaces
        };

        // Act
        var result = _controller.Count(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CharacterCountResponse;
        Assert.NotNull(response);
        Assert.Equal(5, response.TotalCount);
        Assert.Equal(5, response.SpaceCount);
    }

    [Fact]
    public void Count_OnlyTabs_ReturnsCorrectCounts()
    {
        // Arrange
        var request = new CharacterCountRequest
        {
            Input = "\t\t\t"  // 3 tabs
        };

        // Act
        var result = _controller.Count(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CharacterCountResponse;
        Assert.NotNull(response);
        Assert.Equal(3, response.TotalCount);
        Assert.Equal(3, response.TabCount);
    }

    [Fact]
    public void Count_UnicodeSymbols_CountsAsOther()
    {
        // Arrange
        var request = new CharacterCountRequest
        {
            Input = "♠♣♥♦"  // Card suits
        };

        // Act
        var result = _controller.Count(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CharacterCountResponse;
        Assert.NotNull(response);
        Assert.Equal(4, response.TotalCount);
        Assert.Equal(4, response.OtherCount);
    }

    #endregion

    #region Count - Validation Errors

    [Fact]
    public void Count_NullInput_ReturnsBadRequest()
    {
        // Arrange
        var request = new CharacterCountRequest
        {
            Input = null!
        };

        // Act
        var result = _controller.Count(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Count_InputExceedsMaxLength_ReturnsBadRequest()
    {
        // Arrange - Create a string longer than 1MB (1024 * 1024 characters)
        var longString = new string('a', 1024 * 1024 + 1);
        var request = new CharacterCountRequest
        {
            Input = longString
        };

        // Act
        var result = _controller.Count(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Count_InputAtMaxLength_Success()
    {
        // Arrange - Create a string at exactly 1MB (1024 * 1024 characters)
        var maxLengthString = new string('a', 1024 * 1024);
        var request = new CharacterCountRequest
        {
            Input = maxLengthString
        };

        // Act
        var result = _controller.Count(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);

        var response = result.Value as CharacterCountResponse;
        Assert.NotNull(response);
        Assert.Equal(1024 * 1024, response.TotalCount);
        Assert.Equal(1024 * 1024, response.AlphabetCount);
    }

    #endregion

    #region Count - Real-World Scenarios

    [Fact]
    public void Count_EmailAddress_ReturnsCorrectCounts()
    {
        // Arrange
        var request = new CharacterCountRequest
        {
            Input = "user@example.com"
        };

        // Act
        var result = _controller.Count(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CharacterCountResponse;
        Assert.NotNull(response);
        Assert.Equal(16, response.TotalCount);
        Assert.Equal(14, response.AlphabetCount);  // userexamplecom
        Assert.Equal(2, response.OtherCount);      // @.
    }

    [Fact]
    public void Count_JapaneseAddress_ReturnsCorrectCounts()
    {
        // Arrange
        var request = new CharacterCountRequest
        {
            Input = "東京都渋谷区123-45"  // 6 kanji + 5 digits + 1 hyphen = 12
        };

        // Act
        var result = _controller.Count(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CharacterCountResponse;
        Assert.NotNull(response);
        Assert.Equal(12, response.TotalCount);
        Assert.Equal(6, response.KanjiCount);      // 東京都渋谷区
        Assert.Equal(5, response.DigitCount);      // 12345
        Assert.Equal(1, response.OtherCount);      // -
    }

    [Fact]
    public void Count_ProgrammingCode_ReturnsCorrectCounts()
    {
        // Arrange
        var request = new CharacterCountRequest
        {
            Input = "int x = 10;"  // intx(4) + 10(2) + spaces(3) + =;(2) = 11
        };

        // Act
        var result = _controller.Count(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as CharacterCountResponse;
        Assert.NotNull(response);
        Assert.Equal(11, response.TotalCount);
        Assert.Equal(4, response.AlphabetCount);   // intx
        Assert.Equal(2, response.DigitCount);      // 10
        Assert.Equal(3, response.SpaceCount);
        Assert.Equal(2, response.OtherCount);      // =;
    }

    #endregion
}
