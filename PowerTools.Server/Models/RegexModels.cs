using System.ComponentModel.DataAnnotations;

namespace PowerTools.Server.Models;

#region Request Models

/// <summary>
/// Options for regex operations
/// </summary>
public class RegexOptionsDto
{
    /// <summary>
    /// Specifies case-insensitive matching (equivalent to RegexOptions.IgnoreCase)
    /// </summary>
    public bool IgnoreCase { get; set; }

    /// <summary>
    /// Multiline mode. Changes the meaning of ^ and $ so they match at the beginning
    /// and end of any line, not just the beginning and end of the entire string.
    /// </summary>
    public bool Multiline { get; set; }

    /// <summary>
    /// Singleline mode. Changes the meaning of . so it matches every character
    /// (instead of every character except \n).
    /// </summary>
    public bool Singleline { get; set; }
}

/// <summary>
/// Request model for regex IsMatch operation
/// </summary>
public class RegexIsMatchRequest
{
    /// <summary>
    /// The input string to search for a match
    /// </summary>
    [Required(ErrorMessage = "Input string is required")]
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// The regular expression pattern to match
    /// </summary>
    [Required(ErrorMessage = "Pattern is required")]
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Optional regex options
    /// </summary>
    public RegexOptionsDto? Options { get; set; }
}

/// <summary>
/// Request model for regex Match operation
/// </summary>
public class RegexMatchRequest
{
    /// <summary>
    /// The input string to search for a match
    /// </summary>
    [Required(ErrorMessage = "Input string is required")]
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// The regular expression pattern to match
    /// </summary>
    [Required(ErrorMessage = "Pattern is required")]
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Optional regex options
    /// </summary>
    public RegexOptionsDto? Options { get; set; }
}

/// <summary>
/// Request model for regex Replace operation
/// </summary>
public class RegexReplaceRequest
{
    /// <summary>
    /// The input string to perform replacement on
    /// </summary>
    [Required(ErrorMessage = "Input string is required")]
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// The regular expression pattern to match
    /// </summary>
    [Required(ErrorMessage = "Pattern is required")]
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// The replacement string. Can include backreferences like $1, $2, etc.
    /// </summary>
    [Required(ErrorMessage = "Replacement string is required")]
    public string Replacement { get; set; } = string.Empty;

    /// <summary>
    /// Optional regex options
    /// </summary>
    public RegexOptionsDto? Options { get; set; }
}

/// <summary>
/// Request model for regex Split operation
/// </summary>
public class RegexSplitRequest
{
    /// <summary>
    /// The input string to split
    /// </summary>
    [Required(ErrorMessage = "Input string is required")]
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// The regular expression pattern to use as delimiter
    /// </summary>
    [Required(ErrorMessage = "Pattern is required")]
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Optional regex options
    /// </summary>
    public RegexOptionsDto? Options { get; set; }
}

#endregion

#region Response Models

/// <summary>
/// Response model for regex IsMatch operation
/// </summary>
public class RegexIsMatchResponse
{
    /// <summary>
    /// true if the regular expression finds a match; otherwise, false
    /// </summary>
    public bool IsMatch { get; set; }
}

/// <summary>
/// Represents a captured group in a regex match
/// </summary>
public class RegexGroupDto
{
    /// <summary>
    /// The name of the group (empty string for numbered groups)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The captured value
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Whether the group was successfully matched
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The position in the original string where the captured substring begins
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// The length of the captured substring
    /// </summary>
    public int Length { get; set; }
}

/// <summary>
/// Represents a single match in regex operations
/// </summary>
public class RegexMatchDto
{
    /// <summary>
    /// The matched value
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// The position in the original string where the match begins
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// The length of the matched substring
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// List of captured groups (including the entire match as group 0)
    /// </summary>
    public List<RegexGroupDto> Groups { get; set; } = new();
}

/// <summary>
/// Response model for regex Match operation (returns first match)
/// </summary>
public class RegexMatchResponse
{
    /// <summary>
    /// Whether the regex found a match
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The matched value (null if no match)
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// The position in the original string where the match begins
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// The length of the matched substring
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// List of captured groups (including the entire match as group 0)
    /// </summary>
    public List<RegexGroupDto> Groups { get; set; } = new();
}

/// <summary>
/// Response model for regex Matches operation (returns all matches)
/// </summary>
public class RegexMatchesResponse
{
    /// <summary>
    /// The number of matches found
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// List of all matches
    /// </summary>
    public List<RegexMatchDto> Matches { get; set; } = new();
}

/// <summary>
/// Response model for regex Replace operation
/// </summary>
public class RegexReplaceResponse
{
    /// <summary>
    /// The resulting string after replacement
    /// </summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// The number of replacements made
    /// </summary>
    public int ReplacementCount { get; set; }
}

/// <summary>
/// Response model for regex Split operation
/// </summary>
public class RegexSplitResponse
{
    /// <summary>
    /// The number of parts after splitting
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// The array of strings split by the pattern
    /// </summary>
    public List<string> Parts { get; set; } = new();
}

#endregion
