using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PowerTools.Server.Controllers;
using PowerTools.Server.Models;
using Xunit;

namespace PowerTools.Tests;

public class ExcelControllerTests
{
    private readonly ExcelController _controller;

    public ExcelControllerTests()
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<ExcelController>();
        _controller = new ExcelController(logger);
    }

    private string CreateTestExcelBase64(Action<XLWorkbook> configure)
    {
        using var workbook = new XLWorkbook();
        configure(workbook);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Convert.ToBase64String(stream.ToArray());
    }

    #region GetSheetData Tests

    [Fact]
    public void GetSheetData_ReadsFirstSheet_WhenSheetNameNotSpecified()
    {
        // Arrange
        var base64 = CreateTestExcelBase64(wb =>
        {
            var ws = wb.Worksheets.Add("Sheet1");
            ws.Cell("A1").Value = "Name";
            ws.Cell("B1").Value = "Age";
            ws.Cell("A2").Value = "Alice";
            ws.Cell("B2").Value = 30;
            ws.Cell("A3").Value = "Bob";
            ws.Cell("B3").Value = 25;
        });

        var request = new ExcelSheetRequest
        {
            FileContentBase64 = base64,
            SheetName = null
        };

        // Act
        var result = _controller.GetSheetData(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);

        var response = result.Value as ExcelDataResponse;
        Assert.NotNull(response);
        Assert.Equal("Sheet1", response.SheetName);
        Assert.Equal(2, response.Headers.Count);
        Assert.Contains("Name", response.Headers);
        Assert.Contains("Age", response.Headers);
        Assert.Equal(2, response.RowCount);
        Assert.Equal("Alice", response.Rows[0]["Name"]?.ToString());
        Assert.Equal(30.0, response.Rows[0]["Age"]);
    }

    [Fact]
    public void GetSheetData_ReadsSpecificSheet_WhenSheetNameProvided()
    {
        // Arrange
        var base64 = CreateTestExcelBase64(wb =>
        {
            var ws1 = wb.Worksheets.Add("Sheet1");
            ws1.Cell("A1").Value = "Data1";

            var ws2 = wb.Worksheets.Add("Sheet2");
            ws2.Cell("A1").Value = "Header";
            ws2.Cell("A2").Value = "Value";
        });

        var request = new ExcelSheetRequest
        {
            FileContentBase64 = base64,
            SheetName = "Sheet2"
        };

        // Act
        var result = _controller.GetSheetData(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as ExcelDataResponse;
        Assert.NotNull(response);
        Assert.Equal("Sheet2", response.SheetName);
        Assert.Single(response.Headers);
        Assert.Equal("Header", response.Headers[0]);
    }

    [Fact]
    public void GetSheetData_ReturnsEmptyData_WhenSheetIsEmpty()
    {
        // Arrange
        var base64 = CreateTestExcelBase64(wb =>
        {
            wb.Worksheets.Add("EmptySheet");
        });

        var request = new ExcelSheetRequest
        {
            FileContentBase64 = base64,
            SheetName = "EmptySheet"
        };

        // Act
        var result = _controller.GetSheetData(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as ExcelDataResponse;
        Assert.NotNull(response);
        Assert.Empty(response.Headers);
        Assert.Empty(response.Rows);
        Assert.Equal(0, response.RowCount);
    }

    [Fact]
    public void GetSheetData_ReturnsBadRequest_WhenSheetNotFound()
    {
        // Arrange
        var base64 = CreateTestExcelBase64(wb =>
        {
            wb.Worksheets.Add("Sheet1");
        });

        var request = new ExcelSheetRequest
        {
            FileContentBase64 = base64,
            SheetName = "NonExistent"
        };

        // Act
        var result = _controller.GetSheetData(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void GetSheetData_ReturnsBadRequest_WhenInvalidBase64()
    {
        // Arrange
        var request = new ExcelSheetRequest
        {
            FileContentBase64 = "invalid-base64!!!",
            SheetName = null
        };

        // Act
        var result = _controller.GetSheetData(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void GetSheetData_ReturnsBadRequest_WhenEmptyContent()
    {
        // Arrange
        var request = new ExcelSheetRequest
        {
            FileContentBase64 = "",
            SheetName = null
        };

        // Act
        var result = _controller.GetSheetData(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void GetSheetData_HandlesJapaneseText()
    {
        // Arrange
        var base64 = CreateTestExcelBase64(wb =>
        {
            var ws = wb.Worksheets.Add("Sheet1");
            ws.Cell("A1").Value = "名前";
            ws.Cell("B1").Value = "年齢";
            ws.Cell("A2").Value = "田中太郎";
            ws.Cell("B2").Value = 35;
        });

        var request = new ExcelSheetRequest
        {
            FileContentBase64 = base64,
            SheetName = null
        };

        // Act
        var result = _controller.GetSheetData(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as ExcelDataResponse;
        Assert.NotNull(response);
        Assert.Contains("名前", response.Headers);
        Assert.Equal("田中太郎", response.Rows[0]["名前"]?.ToString());
    }

    #endregion

    #region GetRangeData Tests

    [Fact]
    public void GetRangeData_ReadsSpecificRange()
    {
        // Arrange
        var base64 = CreateTestExcelBase64(wb =>
        {
            var ws = wb.Worksheets.Add("Sheet1");
            ws.Cell("A1").Value = "Outside";
            ws.Cell("B2").Value = "Header1";
            ws.Cell("C2").Value = "Header2";
            ws.Cell("B3").Value = "Data1";
            ws.Cell("C3").Value = "Data2";
            ws.Cell("D4").Value = "Outside";
        });

        var request = new ExcelRangeRequest
        {
            FileContentBase64 = base64,
            SheetName = "Sheet1",
            Range = "B2:C3"
        };

        // Act
        var result = _controller.GetRangeData(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as ExcelDataResponse;
        Assert.NotNull(response);
        Assert.Equal(2, response.Headers.Count);
        Assert.Equal("Header1", response.Headers[0]);
        Assert.Equal("Header2", response.Headers[1]);
        Assert.Single(response.Rows);
        Assert.Equal("Data1", response.Rows[0]["Header1"]?.ToString());
    }

    [Fact]
    public void GetRangeData_ReturnsBadRequest_WhenRangeIsEmpty()
    {
        // Arrange
        var base64 = CreateTestExcelBase64(wb =>
        {
            wb.Worksheets.Add("Sheet1");
        });

        var request = new ExcelRangeRequest
        {
            FileContentBase64 = base64,
            SheetName = "Sheet1",
            Range = ""
        };

        // Act
        var result = _controller.GetRangeData(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void GetRangeData_ReturnsBadRequest_WhenRangeIsInvalid()
    {
        // Arrange
        var base64 = CreateTestExcelBase64(wb =>
        {
            wb.Worksheets.Add("Sheet1");
        });

        var request = new ExcelRangeRequest
        {
            FileContentBase64 = base64,
            SheetName = "Sheet1",
            Range = "InvalidRange"
        };

        // Act
        var result = _controller.GetRangeData(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    #endregion

    #region GetTableData Tests

    [Fact]
    public void GetTableData_ReadsTableData()
    {
        // Arrange
        var base64 = CreateTestExcelBase64(wb =>
        {
            var ws = wb.Worksheets.Add("Sheet1");
            ws.Cell("A1").Value = "ID";
            ws.Cell("B1").Value = "Product";
            ws.Cell("C1").Value = "Price";
            ws.Cell("A2").Value = 1;
            ws.Cell("B2").Value = "Apple";
            ws.Cell("C2").Value = 1.50;
            ws.Cell("A3").Value = 2;
            ws.Cell("B3").Value = "Banana";
            ws.Cell("C3").Value = 0.75;

            var range = ws.Range("A1:C3");
            range.CreateTable("ProductTable");
        });

        var request = new ExcelTableRequest
        {
            FileContentBase64 = base64,
            TableName = "ProductTable"
        };

        // Act
        var result = _controller.GetTableData(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as ExcelTableResponse;
        Assert.NotNull(response);
        Assert.Equal("ProductTable", response.TableName);
        Assert.Equal("Sheet1", response.SheetName);
        Assert.Equal(3, response.Headers.Count);
        Assert.Equal(2, response.RowCount);
        Assert.Equal("Apple", response.Rows[0]["Product"]?.ToString());
        Assert.Equal(0.75, response.Rows[1]["Price"]);
    }

    [Fact]
    public void GetTableData_ReturnsBadRequest_WhenTableNotFound()
    {
        // Arrange
        var base64 = CreateTestExcelBase64(wb =>
        {
            wb.Worksheets.Add("Sheet1");
        });

        var request = new ExcelTableRequest
        {
            FileContentBase64 = base64,
            TableName = "NonExistentTable"
        };

        // Act
        var result = _controller.GetTableData(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void GetTableData_ReturnsBadRequest_WhenTableNameIsEmpty()
    {
        // Arrange
        var base64 = CreateTestExcelBase64(wb =>
        {
            wb.Worksheets.Add("Sheet1");
        });

        var request = new ExcelTableRequest
        {
            FileContentBase64 = base64,
            TableName = ""
        };

        // Act
        var result = _controller.GetTableData(request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void GetTableData_FindsTableInAnySheet()
    {
        // Arrange
        var base64 = CreateTestExcelBase64(wb =>
        {
            wb.Worksheets.Add("Sheet1");

            var ws2 = wb.Worksheets.Add("Sheet2");
            ws2.Cell("A1").Value = "Col1";
            ws2.Cell("A2").Value = "Value1";
            ws2.Range("A1:A2").CreateTable("MyTable");
        });

        var request = new ExcelTableRequest
        {
            FileContentBase64 = base64,
            TableName = "MyTable"
        };

        // Act
        var result = _controller.GetTableData(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as ExcelTableResponse;
        Assert.NotNull(response);
        Assert.Equal("Sheet2", response.SheetName);
    }

    #endregion

    #region GetSheets Tests

    [Fact]
    public void GetSheets_ReturnsAllSheets()
    {
        // Arrange
        var base64 = CreateTestExcelBase64(wb =>
        {
            wb.Worksheets.Add("Sheet1");
            wb.Worksheets.Add("Sheet2");
            wb.Worksheets.Add("Sheet3");
        });

        var request = new ExcelSheetRequest
        {
            FileContentBase64 = base64
        };

        // Act
        var result = _controller.GetSheets(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as ExcelSheetsResponse;
        Assert.NotNull(response);
        Assert.Equal(3, response.Sheets.Count);
        Assert.Contains("Sheet1", response.Sheets);
        Assert.Contains("Sheet2", response.Sheets);
        Assert.Contains("Sheet3", response.Sheets);
    }

    #endregion

    #region GetTables Tests

    [Fact]
    public void GetTables_ReturnsAllTables()
    {
        // Arrange
        var base64 = CreateTestExcelBase64(wb =>
        {
            var ws1 = wb.Worksheets.Add("Sheet1");
            ws1.Cell("A1").Value = "Header1";
            ws1.Cell("A2").Value = "Data1";
            ws1.Range("A1:A2").CreateTable("Table1");

            var ws2 = wb.Worksheets.Add("Sheet2");
            ws2.Cell("A1").Value = "Header2";
            ws2.Cell("A2").Value = "Data2";
            ws2.Range("A1:A2").CreateTable("Table2");
        });

        var request = new ExcelSheetRequest
        {
            FileContentBase64 = base64
        };

        // Act
        var result = _controller.GetTables(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as ExcelTablesResponse;
        Assert.NotNull(response);
        Assert.Equal(2, response.Tables.Count);

        var table1 = response.Tables.FirstOrDefault(t => t.Name == "Table1");
        Assert.NotNull(table1);
        Assert.Equal("Sheet1", table1.SheetName);

        var table2 = response.Tables.FirstOrDefault(t => t.Name == "Table2");
        Assert.NotNull(table2);
        Assert.Equal("Sheet2", table2.SheetName);
    }

    [Fact]
    public void GetTables_ReturnsEmptyList_WhenNoTables()
    {
        // Arrange
        var base64 = CreateTestExcelBase64(wb =>
        {
            wb.Worksheets.Add("Sheet1");
        });

        var request = new ExcelSheetRequest
        {
            FileContentBase64 = base64
        };

        // Act
        var result = _controller.GetTables(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as ExcelTablesResponse;
        Assert.NotNull(response);
        Assert.Empty(response.Tables);
    }

    #endregion

    #region Data Type Tests

    [Fact]
    public void GetSheetData_HandlesVariousDataTypes()
    {
        // Arrange
        var testDate = new DateTime(2024, 1, 15, 10, 30, 0);
        var base64 = CreateTestExcelBase64(wb =>
        {
            var ws = wb.Worksheets.Add("Sheet1");
            ws.Cell("A1").Value = "String";
            ws.Cell("B1").Value = "Number";
            ws.Cell("C1").Value = "Boolean";
            ws.Cell("D1").Value = "Date";
            ws.Cell("E1").Value = "Empty";

            ws.Cell("A2").Value = "Hello";
            ws.Cell("B2").Value = 123.45;
            ws.Cell("C2").Value = true;
            ws.Cell("D2").Value = testDate;
            // E2 is empty
        });

        var request = new ExcelSheetRequest
        {
            FileContentBase64 = base64,
            SheetName = null
        };

        // Act
        var result = _controller.GetSheetData(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var response = result.Value as ExcelDataResponse;
        Assert.NotNull(response);

        var row = response.Rows[0];
        Assert.Equal("Hello", row["String"]?.ToString());
        Assert.Equal(123.45, row["Number"]);
        Assert.Equal(true, row["Boolean"]);
        Assert.Equal(testDate, row["Date"]);
        Assert.Null(row["Empty"]);
    }

    #endregion
}
