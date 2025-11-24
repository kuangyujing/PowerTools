using System.ComponentModel.DataAnnotations;

namespace PowerTools.Server.Models;

#region Request Models

/// <summary>
/// Request model for character type counting operation
/// </summary>
public class CharacterCountRequest
{
    /// <summary>
    /// The input string to analyze
    /// </summary>
    [Required(ErrorMessage = "Input string is required")]
    public string Input { get; set; } = string.Empty;
}

#endregion

#region Response Models

/// <summary>
/// Response model for character type counting operation
/// </summary>
public class CharacterCountResponse
{
    /// <summary>
    /// Total number of characters in the input string
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Count of alphabet characters (a-z, A-Z)
    /// </summary>
    public int AlphabetCount { get; set; }

    /// <summary>
    /// Count of half-width digit characters (0-9)
    /// </summary>
    public int DigitCount { get; set; }

    /// <summary>
    /// Count of half-width space characters
    /// </summary>
    public int SpaceCount { get; set; }

    /// <summary>
    /// Count of tab characters
    /// </summary>
    public int TabCount { get; set; }

    /// <summary>
    /// Count of Kanji (CJK Unified Ideographs) characters
    /// </summary>
    public int KanjiCount { get; set; }

    /// <summary>
    /// Count of Hiragana characters
    /// </summary>
    public int HiraganaCount { get; set; }

    /// <summary>
    /// Count of Katakana characters
    /// </summary>
    public int KatakanaCount { get; set; }

    /// <summary>
    /// Count of other characters not in the above categories
    /// </summary>
    public int OtherCount { get; set; }
}

#endregion
