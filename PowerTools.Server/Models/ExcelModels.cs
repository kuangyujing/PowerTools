using System.ComponentModel.DataAnnotations;

namespace PowerTools.Server.Models;

/// <summary>
/// Request to read all data from a specified sheet
/// </summary>
public class ExcelSheetRequest
{
    /// <summary>
    /// Excel file content in Base64 format
    /// </summary>
    [Required]
    public string FileContentBase64 { get; set; } = string.Empty;

    /// <summary>
    /// Name of the sheet to read. If null, reads the first sheet.
    /// </summary>
    public string? SheetName { get; set; }
}

/// <summary>
/// Request to read data from a specified range in a sheet
/// </summary>
public class ExcelRangeRequest
{
    /// <summary>
    /// Excel file content in Base64 format
    /// </summary>
    [Required]
    public string FileContentBase64 { get; set; } = string.Empty;

    /// <summary>
    /// Name of the sheet to read. If null, reads the first sheet.
    /// </summary>
    public string? SheetName { get; set; }

    /// <summary>
    /// Cell range in Excel notation (e.g., "A1:D10", "B2:F20")
    /// </summary>
    [Required]
    public string Range { get; set; } = string.Empty;
}

/// <summary>
/// Request to read data from a named table
/// </summary>
public class ExcelTableRequest
{
    /// <summary>
    /// Excel file content in Base64 format
    /// </summary>
    [Required]
    public string FileContentBase64 { get; set; } = string.Empty;

    /// <summary>
    /// Name of the table to read
    /// </summary>
    [Required]
    public string TableName { get; set; } = string.Empty;
}

/// <summary>
/// Response containing Excel data as JSON
/// </summary>
public class ExcelDataResponse
{
    /// <summary>
    /// The sheet name from which data was read
    /// </summary>
    public string SheetName { get; set; } = string.Empty;

    /// <summary>
    /// Column headers (first row values)
    /// </summary>
    public List<string> Headers { get; set; } = new();

    /// <summary>
    /// Data rows as list of dictionaries (column name -> value)
    /// </summary>
    public List<Dictionary<string, object?>> Rows { get; set; } = new();

    /// <summary>
    /// Total number of rows (excluding header)
    /// </summary>
    public int RowCount { get; set; }

    /// <summary>
    /// Total number of columns
    /// </summary>
    public int ColumnCount { get; set; }
}

/// <summary>
/// Response containing table data as JSON
/// </summary>
public class ExcelTableResponse
{
    /// <summary>
    /// The table name
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// The sheet name containing the table
    /// </summary>
    public string SheetName { get; set; } = string.Empty;

    /// <summary>
    /// Column headers from the table
    /// </summary>
    public List<string> Headers { get; set; } = new();

    /// <summary>
    /// Data rows as list of dictionaries (column name -> value)
    /// </summary>
    public List<Dictionary<string, object?>> Rows { get; set; } = new();

    /// <summary>
    /// Total number of rows (excluding header)
    /// </summary>
    public int RowCount { get; set; }

    /// <summary>
    /// Total number of columns
    /// </summary>
    public int ColumnCount { get; set; }
}

/// <summary>
/// Response containing list of sheets in the workbook
/// </summary>
public class ExcelSheetsResponse
{
    /// <summary>
    /// List of sheet names in the workbook
    /// </summary>
    public List<string> Sheets { get; set; } = new();
}

/// <summary>
/// Response containing list of tables in the workbook
/// </summary>
public class ExcelTablesResponse
{
    /// <summary>
    /// List of table information
    /// </summary>
    public List<ExcelTableInfo> Tables { get; set; } = new();
}

/// <summary>
/// Information about a table in the workbook
/// </summary>
public class ExcelTableInfo
{
    /// <summary>
    /// Table name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Sheet containing the table
    /// </summary>
    public string SheetName { get; set; } = string.Empty;

    /// <summary>
    /// Table range (e.g., "A1:D10")
    /// </summary>
    public string Range { get; set; } = string.Empty;
}
