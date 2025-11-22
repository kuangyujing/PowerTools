using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using PowerTools.Server.Models;

namespace PowerTools.Server.Controllers;

/// <summary>
/// Controller for Excel file operations - read data and convert to JSON
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExcelController : ControllerBase
{
    private readonly ILogger<ExcelController> _logger;

    public ExcelController(ILogger<ExcelController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all data from a specified sheet as JSON
    /// </summary>
    /// <param name="request">Request containing Excel file and sheet name</param>
    /// <returns>Sheet data as JSON</returns>
    [HttpPost("sheet")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ExcelDataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetSheetData([FromBody] ExcelSheetRequest request)
    {
        try
        {
            _logger.LogInformation("Reading sheet data: {SheetName}", request.SheetName ?? "first sheet");

            using var workbook = OpenWorkbook(request.FileContentBase64);
            var worksheet = GetWorksheet(workbook, request.SheetName);

            var response = ReadWorksheetData(worksheet);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in sheet request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading Excel sheet");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while reading the Excel file", details = ex.Message });
        }
    }

    /// <summary>
    /// Get data from a specified range in a sheet as JSON
    /// </summary>
    /// <param name="request">Request containing Excel file, sheet name, and range</param>
    /// <returns>Range data as JSON</returns>
    [HttpPost("range")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ExcelDataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetRangeData([FromBody] ExcelRangeRequest request)
    {
        try
        {
            _logger.LogInformation("Reading range data: Sheet={SheetName}, Range={Range}",
                request.SheetName ?? "first sheet", request.Range);

            if (string.IsNullOrWhiteSpace(request.Range))
            {
                return BadRequest(new { error = "Range is required" });
            }

            using var workbook = OpenWorkbook(request.FileContentBase64);
            var worksheet = GetWorksheet(workbook, request.SheetName);

            var response = ReadRangeData(worksheet, request.Range);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in range request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading Excel range");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while reading the Excel range", details = ex.Message });
        }
    }

    /// <summary>
    /// Get data from a named table as JSON
    /// </summary>
    /// <param name="request">Request containing Excel file and table name</param>
    /// <returns>Table data as JSON</returns>
    [HttpPost("table")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ExcelTableResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetTableData([FromBody] ExcelTableRequest request)
    {
        try
        {
            _logger.LogInformation("Reading table data: {TableName}", request.TableName);

            if (string.IsNullOrWhiteSpace(request.TableName))
            {
                return BadRequest(new { error = "Table name is required" });
            }

            using var workbook = OpenWorkbook(request.FileContentBase64);

            // Find the table in any worksheet
            IXLTable? table = null;
            IXLWorksheet? tableWorksheet = null;

            foreach (var ws in workbook.Worksheets)
            {
                table = ws.Tables.FirstOrDefault(t =>
                    string.Equals(t.Name, request.TableName, StringComparison.OrdinalIgnoreCase));
                if (table != null)
                {
                    tableWorksheet = ws;
                    break;
                }
            }

            if (table == null || tableWorksheet == null)
            {
                return BadRequest(new { error = $"Table '{request.TableName}' not found in the workbook" });
            }

            var response = ReadTableData(table, tableWorksheet.Name);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in table request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading Excel table");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while reading the Excel table", details = ex.Message });
        }
    }

    /// <summary>
    /// Get list of sheets in the workbook
    /// </summary>
    /// <param name="request">Request containing Excel file</param>
    /// <returns>List of sheet names</returns>
    [HttpPost("sheets")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ExcelSheetsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetSheets([FromBody] ExcelSheetRequest request)
    {
        try
        {
            _logger.LogInformation("Getting list of sheets");

            using var workbook = OpenWorkbook(request.FileContentBase64);

            var response = new ExcelSheetsResponse
            {
                Sheets = workbook.Worksheets.Select(ws => ws.Name).ToList()
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in sheets request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Excel sheets");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while reading the Excel file", details = ex.Message });
        }
    }

    /// <summary>
    /// Get list of tables in the workbook
    /// </summary>
    /// <param name="request">Request containing Excel file</param>
    /// <returns>List of tables with their information</returns>
    [HttpPost("tables")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ExcelTablesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetTables([FromBody] ExcelSheetRequest request)
    {
        try
        {
            _logger.LogInformation("Getting list of tables");

            using var workbook = OpenWorkbook(request.FileContentBase64);

            var tables = new List<ExcelTableInfo>();
            foreach (var ws in workbook.Worksheets)
            {
                foreach (var table in ws.Tables)
                {
                    tables.Add(new ExcelTableInfo
                    {
                        Name = table.Name,
                        SheetName = ws.Name,
                        Range = table.RangeAddress.ToString()
                    });
                }
            }

            var response = new ExcelTablesResponse { Tables = tables };
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in tables request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Excel tables");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while reading the Excel file", details = ex.Message });
        }
    }

    private XLWorkbook OpenWorkbook(string fileContentBase64)
    {
        if (string.IsNullOrWhiteSpace(fileContentBase64))
        {
            throw new ArgumentException("File content cannot be empty");
        }

        byte[] fileBytes;
        try
        {
            fileBytes = Convert.FromBase64String(fileContentBase64);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Invalid Base64 file content");
        }

        if (fileBytes.Length == 0)
        {
            throw new ArgumentException("File content cannot be empty");
        }

        var stream = new MemoryStream(fileBytes);
        try
        {
            return new XLWorkbook(stream);
        }
        catch (Exception ex)
        {
            stream.Dispose();
            throw new ArgumentException($"Invalid Excel file: {ex.Message}");
        }
    }

    private IXLWorksheet GetWorksheet(XLWorkbook workbook, string? sheetName)
    {
        if (string.IsNullOrWhiteSpace(sheetName))
        {
            var firstWorksheet = workbook.Worksheets.FirstOrDefault();
            if (firstWorksheet == null)
            {
                throw new ArgumentException("The workbook contains no worksheets.");
            }
            return firstWorksheet;
        }

        var worksheet = workbook.Worksheets.FirstOrDefault(ws =>
            string.Equals(ws.Name, sheetName, StringComparison.OrdinalIgnoreCase));

        if (worksheet == null)
        {
            throw new ArgumentException($"Sheet '{sheetName}' not found in the workbook");
        }

        return worksheet;
    }

    private ExcelDataResponse ReadWorksheetData(IXLWorksheet worksheet)
    {
        var usedRange = worksheet.RangeUsed();
        if (usedRange == null)
        {
            return new ExcelDataResponse
            {
                SheetName = worksheet.Name,
                Headers = new List<string>(),
                Rows = new List<Dictionary<string, object?>>(),
                RowCount = 0,
                ColumnCount = 0
            };
        }

        return ReadRangeDataInternal(usedRange, worksheet.Name);
    }

    private ExcelDataResponse ReadRangeData(IXLWorksheet worksheet, string range)
    {
        IXLRange xlRange;
        try
        {
            xlRange = worksheet.Range(range);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Invalid range '{range}': {ex.Message}");
        }

        return ReadRangeDataInternal(xlRange, worksheet.Name);
    }

    private ExcelDataResponse ReadRangeDataInternal(IXLRange range, string sheetName)
    {
        var firstRow = range.FirstRow();
        var headers = new List<string>();

        // Read headers from first row
        foreach (var cell in firstRow.Cells())
        {
            var headerValue = GetCellValue(cell)?.ToString() ?? $"Column{cell.Address.ColumnNumber}";
            headers.Add(headerValue);
        }

        // Read data rows
        var rows = new List<Dictionary<string, object?>>();
        var dataRows = range.RowsUsed().Skip(1); // Skip header row

        foreach (var row in dataRows)
        {
            var rowData = new Dictionary<string, object?>();
            var cells = row.Cells().ToList();

            for (int i = 0; i < headers.Count && i < cells.Count; i++)
            {
                rowData[headers[i]] = GetCellValue(cells[i]);
            }

            rows.Add(rowData);
        }

        return new ExcelDataResponse
        {
            SheetName = sheetName,
            Headers = headers,
            Rows = rows,
            RowCount = rows.Count,
            ColumnCount = headers.Count
        };
    }

    private ExcelTableResponse ReadTableData(IXLTable table, string sheetName)
    {
        var headers = table.Fields.Select(f => f.Name).ToList();
        var rows = new List<Dictionary<string, object?>>();

        foreach (var row in table.DataRange.RowsUsed())
        {
            var rowData = new Dictionary<string, object?>();
            var cells = row.Cells().ToList();

            for (int i = 0; i < headers.Count && i < cells.Count; i++)
            {
                rowData[headers[i]] = GetCellValue(cells[i]);
            }

            rows.Add(rowData);
        }

        return new ExcelTableResponse
        {
            TableName = table.Name,
            SheetName = sheetName,
            Headers = headers,
            Rows = rows,
            RowCount = rows.Count,
            ColumnCount = headers.Count
        };
    }

    private object? GetCellValue(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        return cell.DataType switch
        {
            XLDataType.Boolean => cell.GetBoolean(),
            XLDataType.Number => cell.GetDouble(),
            XLDataType.DateTime => cell.GetDateTime(),
            XLDataType.TimeSpan => cell.GetTimeSpan(),
            _ => cell.GetString()
        };
    }
}
