using System.Text;
using Microsoft.AspNetCore.Mvc;
using PowerTools.Server.Models;

namespace PowerTools.Server.Controllers;

/// <summary>
/// Controller for CSV parsing and generation operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CsvController : ControllerBase
{
    private readonly ILogger<CsvController> _logger;

    public CsvController(ILogger<CsvController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parse CSV content and convert to JSON
    /// </summary>
    /// <param name="request">Request containing CSV content and parsing options</param>
    /// <returns>Parsed data as JSON</returns>
    [HttpPost("parse")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(CsvParseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Parse([FromBody] CsvParseRequest request)
    {
        try
        {
            _logger.LogInformation("Parsing CSV: HasHeader={HasHeader}, Delimiter={Delimiter}",
                request.HasHeader, request.Delimiter);

            if (string.IsNullOrEmpty(request.Content))
            {
                return BadRequest(new { error = "Content cannot be empty" });
            }

            if (string.IsNullOrEmpty(request.Delimiter))
            {
                return BadRequest(new { error = "Delimiter cannot be empty" });
            }

            if (request.Delimiter.Length > 1)
            {
                return BadRequest(new { error = "Delimiter must be a single character" });
            }

            var content = request.IsBase64
                ? DecodeBase64(request.Content)
                : request.Content;

            var response = ParseCsv(content, request);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in CSV parse request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing CSV");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while parsing the CSV", details = ex.Message });
        }
    }

    /// <summary>
    /// Generate CSV from JSON data
    /// </summary>
    /// <param name="request">Request containing JSON data and generation options</param>
    /// <returns>Generated CSV content</returns>
    [HttpPost("generate")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(CsvGenerateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Generate([FromBody] CsvGenerateRequest request)
    {
        try
        {
            _logger.LogInformation("Generating CSV: RowCount={RowCount}, Delimiter={Delimiter}",
                request.Rows.Count, request.Delimiter);

            if (request.Rows == null || request.Rows.Count == 0)
            {
                return BadRequest(new { error = "Rows cannot be empty" });
            }

            if (string.IsNullOrEmpty(request.Delimiter))
            {
                return BadRequest(new { error = "Delimiter cannot be empty" });
            }

            if (request.Delimiter.Length > 1)
            {
                return BadRequest(new { error = "Delimiter must be a single character" });
            }

            var response = GenerateCsv(request);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in CSV generate request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating CSV");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while generating the CSV", details = ex.Message });
        }
    }

    private string DecodeBase64(string base64Content)
    {
        try
        {
            var bytes = Convert.FromBase64String(base64Content);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Invalid Base64 content");
        }
    }

    private CsvParseResponse ParseCsv(string content, CsvParseRequest options)
    {
        var delimiter = options.Delimiter[0];
        var textQualifier = string.IsNullOrEmpty(options.TextQualifier) ? '"' : options.TextQualifier[0];
        var lines = SplitLines(content);
        var rows = new List<Dictionary<string, string?>>();
        var headers = new List<string>();

        // Parse all rows first
        var allParsedRows = new List<List<string>>();
        foreach (var line in lines)
        {
            if (options.SkipEmptyRows && string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = ParseLine(line, delimiter, textQualifier, options.TrimValues);
            allParsedRows.Add(fields);
        }

        if (allParsedRows.Count == 0)
        {
            return new CsvParseResponse
            {
                Headers = headers,
                Rows = rows,
                RowCount = 0,
                ColumnCount = 0
            };
        }

        // Determine column count from the maximum fields in any row
        var columnCount = allParsedRows.Max(r => r.Count);

        // Extract headers
        if (options.HasHeader && allParsedRows.Count > 0)
        {
            var headerRow = allParsedRows[0];
            for (int i = 0; i < columnCount; i++)
            {
                var header = i < headerRow.Count && !string.IsNullOrEmpty(headerRow[i])
                    ? headerRow[i]
                    : $"Column{i + 1}";
                headers.Add(header);
            }
            allParsedRows.RemoveAt(0);
        }
        else
        {
            // Generate column names
            for (int i = 0; i < columnCount; i++)
            {
                headers.Add($"Column{i + 1}");
            }
        }

        // Convert to dictionaries
        foreach (var parsedRow in allParsedRows)
        {
            var rowDict = new Dictionary<string, string?>();
            for (int i = 0; i < headers.Count; i++)
            {
                var value = i < parsedRow.Count ? parsedRow[i] : null;
                rowDict[headers[i]] = string.IsNullOrEmpty(value) ? null : value;
            }
            rows.Add(rowDict);
        }

        return new CsvParseResponse
        {
            Headers = headers,
            Rows = rows,
            RowCount = rows.Count,
            ColumnCount = columnCount
        };
    }

    private List<string> SplitLines(string content)
    {
        // Handle both \r\n and \n line endings
        var lines = new List<string>();
        var currentLine = new StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < content.Length; i++)
        {
            var c = content[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
                currentLine.Append(c);
            }
            else if (c == '\r' && !inQuotes)
            {
                // Skip \r, handle \n next
                if (i + 1 < content.Length && content[i + 1] == '\n')
                {
                    continue;
                }
                lines.Add(currentLine.ToString());
                currentLine.Clear();
            }
            else if (c == '\n' && !inQuotes)
            {
                lines.Add(currentLine.ToString());
                currentLine.Clear();
            }
            else
            {
                currentLine.Append(c);
            }
        }

        // Add the last line if not empty
        if (currentLine.Length > 0)
        {
            lines.Add(currentLine.ToString());
        }

        return lines;
    }

    private List<string> ParseLine(string line, char delimiter, char textQualifier, bool trimValues)
    {
        var fields = new List<string>();
        var currentField = new StringBuilder();
        var inQuotes = false;
        var i = 0;

        while (i < line.Length)
        {
            var c = line[i];

            if (c == textQualifier)
            {
                if (inQuotes)
                {
                    // Check for escaped quote (double quote)
                    if (i + 1 < line.Length && line[i + 1] == textQualifier)
                    {
                        currentField.Append(textQualifier);
                        i += 2;
                        continue;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    inQuotes = true;
                }
                i++;
            }
            else if (c == delimiter && !inQuotes)
            {
                var value = currentField.ToString();
                fields.Add(trimValues ? value.Trim() : value);
                currentField.Clear();
                i++;
            }
            else
            {
                currentField.Append(c);
                i++;
            }
        }

        // Add the last field
        var lastValue = currentField.ToString();
        fields.Add(trimValues ? lastValue.Trim() : lastValue);

        return fields;
    }

    private CsvGenerateResponse GenerateCsv(CsvGenerateRequest request)
    {
        var delimiter = request.Delimiter[0];
        var textQualifier = string.IsNullOrEmpty(request.TextQualifier) ? '"' : request.TextQualifier[0];
        var lineEnding = request.LineEnding?.ToLower() == "lf" ? "\n" : "\r\n";
        var sb = new StringBuilder();

        // Determine headers
        var headers = request.Headers?.ToList() ?? new List<string>();
        if (headers.Count == 0 && request.Rows.Count > 0)
        {
            // Use keys from first row as headers
            headers = request.Rows[0].Keys.ToList();
        }

        // Write header row
        if (request.IncludeHeader && headers.Count > 0)
        {
            var headerLine = string.Join(delimiter.ToString(),
                headers.Select(h => EscapeField(h, delimiter, textQualifier)));
            sb.Append(headerLine);
            sb.Append(lineEnding);
        }

        // Write data rows
        foreach (var row in request.Rows)
        {
            var fields = new List<string>();
            foreach (var header in headers)
            {
                var value = row.TryGetValue(header, out var val) ? val?.ToString() ?? "" : "";
                fields.Add(EscapeField(value, delimiter, textQualifier));
            }
            sb.Append(string.Join(delimiter.ToString(), fields));
            sb.Append(lineEnding);
        }

        var content = sb.ToString();

        // Optionally encode as Base64
        if (request.OutputBase64)
        {
            content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
        }

        return new CsvGenerateResponse
        {
            Content = content,
            IsBase64 = request.OutputBase64,
            RowCount = request.Rows.Count,
            ColumnCount = headers.Count
        };
    }

    private string EscapeField(string value, char delimiter, char textQualifier)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var needsQuoting = value.Contains(delimiter) ||
                          value.Contains(textQualifier) ||
                          value.Contains('\r') ||
                          value.Contains('\n');

        if (needsQuoting)
        {
            // Escape existing text qualifiers by doubling them
            var escaped = value.Replace(textQualifier.ToString(), $"{textQualifier}{textQualifier}");
            return $"{textQualifier}{escaped}{textQualifier}";
        }

        return value;
    }
}
