using System.ComponentModel.DataAnnotations;

namespace PowerTools.Server.Models;

/// <summary>
/// Request to parse CSV data to JSON
/// </summary>
public class CsvParseRequest
{
    /// <summary>
    /// CSV content as a string or Base64 encoded
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Whether the content is Base64 encoded (default: false)
    /// </summary>
    public bool IsBase64 { get; set; } = false;

    /// <summary>
    /// Delimiter character (default: comma)
    /// </summary>
    public string Delimiter { get; set; } = ",";

    /// <summary>
    /// Whether the first row contains headers (default: true)
    /// </summary>
    public bool HasHeader { get; set; } = true;

    /// <summary>
    /// Text qualifier character for fields containing delimiters (default: double quote)
    /// </summary>
    public string TextQualifier { get; set; } = "\"";

    /// <summary>
    /// Skip empty rows (default: true)
    /// </summary>
    public bool SkipEmptyRows { get; set; } = true;

    /// <summary>
    /// Trim whitespace from values (default: true)
    /// </summary>
    public bool TrimValues { get; set; } = true;
}

/// <summary>
/// Response containing parsed CSV data as JSON
/// </summary>
public class CsvParseResponse
{
    /// <summary>
    /// Column headers (from first row or generated)
    /// </summary>
    public List<string> Headers { get; set; } = new();

    /// <summary>
    /// Data rows as list of dictionaries (column name -> value)
    /// </summary>
    public List<Dictionary<string, string?>> Rows { get; set; } = new();

    /// <summary>
    /// Total number of rows (excluding header if present)
    /// </summary>
    public int RowCount { get; set; }

    /// <summary>
    /// Total number of columns
    /// </summary>
    public int ColumnCount { get; set; }
}

/// <summary>
/// Request to generate CSV from JSON data
/// </summary>
public class CsvGenerateRequest
{
    /// <summary>
    /// Column headers for the CSV
    /// </summary>
    public List<string>? Headers { get; set; }

    /// <summary>
    /// Data rows as list of dictionaries (column name -> value)
    /// </summary>
    [Required]
    public List<Dictionary<string, object?>> Rows { get; set; } = new();

    /// <summary>
    /// Delimiter character (default: comma)
    /// </summary>
    public string Delimiter { get; set; } = ",";

    /// <summary>
    /// Whether to include header row in output (default: true)
    /// </summary>
    public bool IncludeHeader { get; set; } = true;

    /// <summary>
    /// Text qualifier character for fields containing delimiters (default: double quote)
    /// </summary>
    public string TextQualifier { get; set; } = "\"";

    /// <summary>
    /// Line ending style: "crlf" for Windows (\r\n), "lf" for Unix (\n) (default: crlf)
    /// </summary>
    public string LineEnding { get; set; } = "crlf";

    /// <summary>
    /// Return the CSV as Base64 encoded (default: false)
    /// </summary>
    public bool OutputBase64 { get; set; } = false;
}

/// <summary>
/// Response containing generated CSV data
/// </summary>
public class CsvGenerateResponse
{
    /// <summary>
    /// Generated CSV content (plain text or Base64 encoded)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Whether the content is Base64 encoded
    /// </summary>
    public bool IsBase64 { get; set; }

    /// <summary>
    /// Total number of rows (excluding header)
    /// </summary>
    public int RowCount { get; set; }

    /// <summary>
    /// Total number of columns
    /// </summary>
    public int ColumnCount { get; set; }
}
