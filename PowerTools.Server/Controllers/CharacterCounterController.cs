using Microsoft.AspNetCore.Mvc;
using PowerTools.Server.Models;

namespace PowerTools.Server.Controllers;

/// <summary>
/// Controller for character type counting operations.
/// Provides functionality to count different character types in a string
/// including alphabets, digits, spaces, tabs, kanji, hiragana, katakana, and others.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CharacterCounterController : ControllerBase
{
    private readonly ILogger<CharacterCounterController> _logger;

    // Maximum input string length (1MB)
    private const int MaxInputLength = 1024 * 1024;

    public CharacterCounterController(ILogger<CharacterCounterController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Count the number of each character type in the input string
    /// </summary>
    /// <param name="request">The request containing the input string to analyze</param>
    /// <returns>Counts of each character type</returns>
    /// <response code="200">Returns the character type counts</response>
    /// <response code="400">If the request is invalid</response>
    [HttpPost("count")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(CharacterCountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Count([FromBody] CharacterCountRequest request)
    {
        if (request.Input == null)
        {
            return BadRequest(new { error = "Input cannot be null" });
        }

        if (request.Input.Length > MaxInputLength)
        {
            return BadRequest(new { error = $"Input string exceeds maximum length of {MaxInputLength} characters" });
        }

        _logger.LogInformation("CharacterCounter: InputLength={Length}", request.Input.Length);

        var response = CountCharacterTypes(request.Input);

        return Ok(response);
    }

    /// <summary>
    /// Count each character type in the input string
    /// </summary>
    private CharacterCountResponse CountCharacterTypes(string input)
    {
        var response = new CharacterCountResponse
        {
            TotalCount = input.Length
        };

        foreach (char c in input)
        {
            if (IsAlphabet(c))
            {
                response.AlphabetCount++;
            }
            else if (IsDigit(c))
            {
                response.DigitCount++;
            }
            else if (c == ' ')
            {
                response.SpaceCount++;
            }
            else if (c == '\t')
            {
                response.TabCount++;
            }
            else if (IsKanji(c))
            {
                response.KanjiCount++;
            }
            else if (IsHiragana(c))
            {
                response.HiraganaCount++;
            }
            else if (IsKatakana(c))
            {
                response.KatakanaCount++;
            }
            else
            {
                response.OtherCount++;
            }
        }

        return response;
    }

    /// <summary>
    /// Check if the character is an alphabet (a-z, A-Z)
    /// </summary>
    private static bool IsAlphabet(char c)
    {
        return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
    }

    /// <summary>
    /// Check if the character is a half-width digit (0-9)
    /// </summary>
    private static bool IsDigit(char c)
    {
        return c >= '0' && c <= '9';
    }

    /// <summary>
    /// Check if the character is a Kanji (CJK Unified Ideographs)
    /// Covers the main CJK Unified Ideographs block (U+4E00-U+9FFF)
    /// and CJK Unified Ideographs Extension A (U+3400-U+4DBF)
    /// </summary>
    private static bool IsKanji(char c)
    {
        // CJK Unified Ideographs (U+4E00-U+9FFF)
        // CJK Unified Ideographs Extension A (U+3400-U+4DBF)
        return (c >= '\u4E00' && c <= '\u9FFF') || (c >= '\u3400' && c <= '\u4DBF');
    }

    /// <summary>
    /// Check if the character is Hiragana (U+3040-U+309F)
    /// </summary>
    private static bool IsHiragana(char c)
    {
        return c >= '\u3040' && c <= '\u309F';
    }

    /// <summary>
    /// Check if the character is Katakana (U+30A0-U+30FF)
    /// </summary>
    private static bool IsKatakana(char c)
    {
        return c >= '\u30A0' && c <= '\u30FF';
    }
}
