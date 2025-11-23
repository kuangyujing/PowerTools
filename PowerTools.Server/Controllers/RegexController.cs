using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using PowerTools.Server.Models;

namespace PowerTools.Server.Controllers;

/// <summary>
/// Controller for regular expression operations.
/// Provides pattern matching, extraction, replacement, and splitting capabilities
/// that are not natively available in Power Platform Cloud Flows.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RegexController : ControllerBase
{
    private readonly ILogger<RegexController> _logger;

    // Maximum input string length to prevent ReDoS attacks (1MB)
    private const int MaxInputLength = 1024 * 1024;

    // Timeout for regex operations (3 seconds to stay within Custom Connector's 5-second limit)
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(3);

    public RegexController(ILogger<RegexController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Check if the input string matches the regular expression pattern
    /// </summary>
    /// <param name="request">The IsMatch request containing input and pattern</param>
    /// <returns>Whether the pattern matches the input</returns>
    /// <response code="200">Returns the match result</response>
    /// <response code="400">If the request is invalid or pattern is malformed</response>
    [HttpPost("ismatch")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(RegexIsMatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult IsMatch([FromBody] RegexIsMatchRequest request)
    {
        try
        {
            var validationError = ValidateInput(request.Input, request.Pattern);
            if (validationError != null)
            {
                return BadRequest(new { error = validationError });
            }

            _logger.LogInformation("IsMatch: Pattern={Pattern}, InputLength={Length}",
                request.Pattern, request.Input.Length);

            var options = ConvertOptions(request.Options);
            var regex = new Regex(request.Pattern, options, RegexTimeout);

            var isMatch = regex.IsMatch(request.Input);

            return Ok(new RegexIsMatchResponse { IsMatch = isMatch });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid regex pattern: {Pattern}", request.Pattern);
            return BadRequest(new { error = $"Invalid regex pattern: {ex.Message}" });
        }
        catch (RegexMatchTimeoutException ex)
        {
            _logger.LogWarning(ex, "Regex operation timed out");
            return BadRequest(new { error = "Regex operation timed out. The pattern may be too complex." });
        }
    }

    /// <summary>
    /// Find the first match of the pattern in the input string
    /// </summary>
    /// <param name="request">The Match request containing input and pattern</param>
    /// <returns>The first match with captured groups</returns>
    /// <response code="200">Returns the first match or empty result if no match</response>
    /// <response code="400">If the request is invalid or pattern is malformed</response>
    [HttpPost("match")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(RegexMatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Match([FromBody] RegexMatchRequest request)
    {
        try
        {
            var validationError = ValidateInput(request.Input, request.Pattern);
            if (validationError != null)
            {
                return BadRequest(new { error = validationError });
            }

            _logger.LogInformation("Match: Pattern={Pattern}, InputLength={Length}",
                request.Pattern, request.Input.Length);

            var options = ConvertOptions(request.Options);
            var regex = new Regex(request.Pattern, options, RegexTimeout);

            var match = regex.Match(request.Input);

            var response = new RegexMatchResponse
            {
                Success = match.Success,
                Value = match.Success ? match.Value : null,
                Index = match.Index,
                Length = match.Length,
                Groups = ConvertGroups(match.Groups, regex.GetGroupNames())
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid regex pattern: {Pattern}", request.Pattern);
            return BadRequest(new { error = $"Invalid regex pattern: {ex.Message}" });
        }
        catch (RegexMatchTimeoutException ex)
        {
            _logger.LogWarning(ex, "Regex operation timed out");
            return BadRequest(new { error = "Regex operation timed out. The pattern may be too complex." });
        }
    }

    /// <summary>
    /// Find all matches of the pattern in the input string
    /// </summary>
    /// <param name="request">The Match request containing input and pattern</param>
    /// <returns>All matches with captured groups</returns>
    /// <response code="200">Returns all matches or empty list if no matches</response>
    /// <response code="400">If the request is invalid or pattern is malformed</response>
    [HttpPost("matches")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(RegexMatchesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Matches([FromBody] RegexMatchRequest request)
    {
        try
        {
            var validationError = ValidateInput(request.Input, request.Pattern);
            if (validationError != null)
            {
                return BadRequest(new { error = validationError });
            }

            _logger.LogInformation("Matches: Pattern={Pattern}, InputLength={Length}",
                request.Pattern, request.Input.Length);

            var options = ConvertOptions(request.Options);
            var regex = new Regex(request.Pattern, options, RegexTimeout);

            var matches = regex.Matches(request.Input);
            var groupNames = regex.GetGroupNames();

            var matchList = new List<RegexMatchDto>();
            foreach (Match match in matches)
            {
                matchList.Add(new RegexMatchDto
                {
                    Value = match.Value,
                    Index = match.Index,
                    Length = match.Length,
                    Groups = ConvertGroups(match.Groups, groupNames)
                });
            }

            var response = new RegexMatchesResponse
            {
                Count = matchList.Count,
                Matches = matchList
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid regex pattern: {Pattern}", request.Pattern);
            return BadRequest(new { error = $"Invalid regex pattern: {ex.Message}" });
        }
        catch (RegexMatchTimeoutException ex)
        {
            _logger.LogWarning(ex, "Regex operation timed out");
            return BadRequest(new { error = "Regex operation timed out. The pattern may be too complex." });
        }
    }

    /// <summary>
    /// Replace all occurrences of the pattern with the replacement string
    /// </summary>
    /// <param name="request">The Replace request containing input, pattern, and replacement</param>
    /// <returns>The result string after replacement</returns>
    /// <response code="200">Returns the replaced string</response>
    /// <response code="400">If the request is invalid or pattern is malformed</response>
    [HttpPost("replace")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(RegexReplaceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Replace([FromBody] RegexReplaceRequest request)
    {
        try
        {
            var validationError = ValidateInput(request.Input, request.Pattern);
            if (validationError != null)
            {
                return BadRequest(new { error = validationError });
            }

            _logger.LogInformation("Replace: Pattern={Pattern}, InputLength={Length}",
                request.Pattern, request.Input.Length);

            var options = ConvertOptions(request.Options);
            var regex = new Regex(request.Pattern, options, RegexTimeout);

            // Count replacements
            var matches = regex.Matches(request.Input);
            var replacementCount = matches.Count;

            // Perform replacement
            var result = regex.Replace(request.Input, request.Replacement ?? string.Empty);

            var response = new RegexReplaceResponse
            {
                Result = result,
                ReplacementCount = replacementCount
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid regex pattern: {Pattern}", request.Pattern);
            return BadRequest(new { error = $"Invalid regex pattern: {ex.Message}" });
        }
        catch (RegexMatchTimeoutException ex)
        {
            _logger.LogWarning(ex, "Regex operation timed out");
            return BadRequest(new { error = "Regex operation timed out. The pattern may be too complex." });
        }
    }

    /// <summary>
    /// Split the input string by the pattern
    /// </summary>
    /// <param name="request">The Split request containing input and pattern</param>
    /// <returns>Array of strings split by the pattern</returns>
    /// <response code="200">Returns the split strings</response>
    /// <response code="400">If the request is invalid or pattern is malformed</response>
    [HttpPost("split")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(RegexSplitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Split([FromBody] RegexSplitRequest request)
    {
        try
        {
            var validationError = ValidateInput(request.Input, request.Pattern);
            if (validationError != null)
            {
                return BadRequest(new { error = validationError });
            }

            _logger.LogInformation("Split: Pattern={Pattern}, InputLength={Length}",
                request.Pattern, request.Input.Length);

            var options = ConvertOptions(request.Options);
            var regex = new Regex(request.Pattern, options, RegexTimeout);

            var parts = regex.Split(request.Input);

            var response = new RegexSplitResponse
            {
                Count = parts.Length,
                Parts = parts.ToList()
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid regex pattern: {Pattern}", request.Pattern);
            return BadRequest(new { error = $"Invalid regex pattern: {ex.Message}" });
        }
        catch (RegexMatchTimeoutException ex)
        {
            _logger.LogWarning(ex, "Regex operation timed out");
            return BadRequest(new { error = "Regex operation timed out. The pattern may be too complex." });
        }
    }

    #region Helper Methods

    /// <summary>
    /// Validate input parameters
    /// </summary>
    private string? ValidateInput(string input, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return "Pattern cannot be empty";
        }

        if (input == null)
        {
            return "Input cannot be null";
        }

        if (input.Length > MaxInputLength)
        {
            return $"Input string exceeds maximum length of {MaxInputLength} characters";
        }

        return null;
    }

    /// <summary>
    /// Convert DTO options to RegexOptions enum
    /// </summary>
    private RegexOptions ConvertOptions(RegexOptionsDto? options)
    {
        if (options == null)
        {
            return RegexOptions.None;
        }

        var result = RegexOptions.None;

        if (options.IgnoreCase)
        {
            result |= RegexOptions.IgnoreCase;
        }

        if (options.Multiline)
        {
            result |= RegexOptions.Multiline;
        }

        if (options.Singleline)
        {
            result |= RegexOptions.Singleline;
        }

        return result;
    }

    /// <summary>
    /// Convert Match.Groups to list of DTOs
    /// </summary>
    private List<RegexGroupDto> ConvertGroups(GroupCollection groups, string[] groupNames)
    {
        var result = new List<RegexGroupDto>();

        for (int i = 0; i < groups.Count; i++)
        {
            var group = groups[i];
            var name = i < groupNames.Length ? groupNames[i] : i.ToString();

            result.Add(new RegexGroupDto
            {
                Name = name,
                Value = group.Value,
                Success = group.Success,
                Index = group.Index,
                Length = group.Length
            });
        }

        return result;
    }

    #endregion
}
