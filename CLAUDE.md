# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PowerTools is a .NET 8 ASP.NET Core Web API solution for Power Platform tools and utilities. The solution consists of:
- **PowerTools.Server**: Main ASP.NET Core Web API project with Swagger/OpenAPI support
- **PowerTools.Tests**: xUnit test project

### Project Philosophy

Power Platform emphasizes low-code development, which offers rapid application development but comes at the cost of flexibility that full-code solutions provide. PowerTools addresses this limitation by serving as a **Swiss Army knife for Power Platform** - providing a collection of lightweight, single-purpose APIs that extend Power Platform's capabilities.

Each API is designed to:
- Focus on a single, well-defined function
- Be lightweight and performant
- Seamlessly integrate with Power Platform flows and apps
- Fill gaps where Power Platform's low-code approach falls short

## Implemented APIs

### Image Processing (`/api/image`)
- **POST /resize**: Resize JPEG/PNG images with aspect ratio control and quality settings
- **POST /info**: Get image metadata (dimensions, format, file size)
- Uses SkiaSharp for image processing

### Barcode & QR Code Generation (`/api/barcode`)
- **POST /qrcode**: Generate QR codes (PNG/SVG, configurable size and error correction level)
- **POST /barcode**: Generate barcodes (Code128, Code39, EAN13, EAN8, UPC_A, ITF, Codabar)
- **GET /types**: List supported barcode types
- Uses ZXing.Net with SkiaSharp renderer

### Excel Processing (`/api/excel`)
- **POST /sheet**: Read entire sheet data as JSON
- **POST /range**: Read specific cell range as JSON
- **POST /table**: Read named table as JSON
- **POST /sheets**: List all sheet names
- **POST /tables**: List all tables with metadata
- Uses ClosedXML for Excel file processing

### Regular Expression (`/api/regex`)
- **POST /ismatch**: Check if pattern matches input
- **POST /match**: Find first match with captured groups
- **POST /matches**: Find all matches
- **POST /replace**: Replace pattern with string
- **POST /split**: Split string by pattern
- Includes ReDoS protection (3-second timeout, 1MB input limit)

### Character Counter (`/api/charactercounter`)
- **POST /count**: Count character types (alphabets, digits, spaces, tabs, kanji, hiragana, katakana, etc.)
- Supports Japanese character classification

### Encoding Converter (`/api/encodingconverter`)
- **POST /convert**: Convert file encoding with auto-detection
- **GET /encodings**: List supported encodings
- Supports UTF-8, Shift_JIS, EUC-JP, GB2312, Big5, etc.

## Language Guidelines

- **English**: All code, code comments, and CLAUDE.md
- **Japanese**: Documentation in `docs/` directory and `README.md`
- **Communication**: Respond to the user in Japanese

## Commands

### Build
```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build PowerTools.Server/
dotnet build PowerTools.Tests/
```

### Run
```bash
# Run the server (from root or PowerTools.Server directory)
dotnet run --project PowerTools.Server
# Or from the project directory:
cd PowerTools.Server && dotnet run

# The API runs on HTTPS by default with Swagger UI available at /swagger
```

### Test
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test PowerTools.Tests/
```

### Development
```bash
# Watch and run (hot reload)
dotnet watch run --project PowerTools.Server

# Clean solution
dotnet clean

# Restore packages
dotnet restore
```

### Docker
```bash
# Build Docker image
docker build -t powertools:latest .

# Run container
docker run -d -p 8080:8080 --name powertools-server powertools:latest

# Using docker-compose
docker-compose up -d      # Start in background
docker-compose down       # Stop and remove
docker-compose logs -f    # View logs
```

## Container Support

### Dockerfile
Multi-stage build optimized for production:
- **Stage 1 (build)**: Uses `mcr.microsoft.com/dotnet/sdk:8.0` to restore and build
- **Stage 2 (publish)**: Creates release artifacts
- **Stage 3 (final)**: Uses `mcr.microsoft.com/dotnet/aspnet:8.0` runtime image

Security features:
- Runs as non-root user (`appuser`)
- Minimal runtime image with only necessary dependencies
- Health check endpoint at `/api/health`

### docker-compose.yml
Development-ready configuration:
- **Port**: 8080 (host) -> 8080 (container)
- **Environment**: Development mode with configurable logging
- **Volumes**: `./logs:/app/logs` for log persistence
- **Health check**: Automatic container health monitoring
- **Network**: Isolated bridge network (`powertools-network`)
- **Restart policy**: `unless-stopped`

### Environment Variables
| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Production | Runtime environment |
| `ASPNETCORE_URLS` | http://+:8080 | Listening URL |
| `Logging__LogLevel__Default` | Information | Default log level |

## Deployment

This repository is automatically deployed via [Railway](https://railway.app/). When changes are pushed to the main branch, Railway automatically builds the Docker image using the `Dockerfile` and deploys it to production.

## Architecture

### Technology Stack
- **.NET 8**: Target framework for both projects
- **ASP.NET Core**: Web API framework with Controller-based architecture
- **Swagger/OpenAPI**: API documentation via Swashbuckle
- **xUnit**: Testing framework with test project referencing the server project
- **SkiaSharp**: Cross-platform 2D graphics library for image processing
- **ZXing.Net**: Barcode/QR code generation library
- **ClosedXML**: Excel file processing library
- **UtfUnknown**: Encoding detection library

### Project Structure
The solution follows standard .NET project organization:
- Solution file (`.sln`) at root manages both projects
- Server project configured as Web SDK with OpenAPI/Swagger packages
- Controllers located in `PowerTools.Server/Controllers/` directory
- Models located in `PowerTools.Server/Models/` directory
- Services located in `PowerTools.Server/Services/` directory
- Test project configured with xUnit and references the server project for integration testing

### Key Implementation Details
- The server uses Controller-based architecture with `AddControllers()` and `MapControllers()` in `Program.cs`
- Controllers inherit from `ControllerBase` and use standard `[ApiController]` and `[Route]` attributes
- Swagger UI is enabled in development environment for API exploration at `/swagger`
- HTTPS redirection and authorization middleware are configured by default
- Test project is set up with xUnit and code coverage collection support via coverlet

### Adding New APIs
When adding new APIs to PowerTools:
1. Create a new Controller class in `PowerTools.Server/Controllers/`
2. Inherit from `ControllerBase` and add `[ApiController]` attribute
3. Define route using `[Route("api/[controller]")]`
4. Create model classes in `PowerTools.Server/Models/` directory
5. Implement action methods with appropriate HTTP verb attributes (`[HttpGet]`, `[HttpPost]`, etc.)
6. Follow the single-purpose API principle - each controller should focus on one specific utility function
7. Create corresponding test class in `PowerTools.Tests/`

## Coding Conventions

### Naming Conventions
- **Namespaces**: `PowerTools.Server.Controllers`, `PowerTools.Server.Models`, `PowerTools.Server.Services`
- **Files**: PascalCase matching class name (e.g., `ImageController.cs`, `BarcodeModels.cs`)
- **Models**: Group related models in `*Models.cs` files (e.g., `BarcodeModels.cs` contains `QrCodeRequest`, `QrCodeResponse`, `BarcodeRequest`, `BarcodeResponse`)
- **Request/Response**: Use `*Request` suffix for input models, `*Response` suffix for output models

### Controller Pattern
```csharp
[ApiController]
[Route("api/[controller]")]
public class ExampleController : ControllerBase
{
    private readonly ILogger<ExampleController> _logger;

    public ExampleController(ILogger<ExampleController> logger)
    {
        _logger = logger;
    }

    [HttpPost("action")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ExampleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Action([FromBody] ExampleRequest request)
    {
        try
        {
            _logger.LogInformation("Action: Param={Param}", request.Param);

            // Validation
            if (string.IsNullOrWhiteSpace(request.RequiredField))
            {
                return BadRequest(new { error = "RequiredField cannot be empty" });
            }

            // Business logic here

            _logger.LogInformation("Action completed successfully");
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Action");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred", details = ex.Message });
        }
    }
}
```

### Model Pattern
```csharp
/// <summary>
/// Request model description
/// </summary>
public class ExampleRequest
{
    /// <summary>
    /// Property description
    /// </summary>
    [Required]
    public string RequiredField { get; set; } = string.Empty;

    /// <summary>
    /// Optional property description
    /// </summary>
    public string? OptionalField { get; set; }

    /// <summary>
    /// Numeric property with range validation
    /// </summary>
    [Range(1, 100)]
    public int Size { get; set; } = 50;

    /// <summary>
    /// Collection property
    /// </summary>
    public List<string> Items { get; set; } = new();
}
```

### Service Pattern
- Register services in `Program.cs` with `builder.Services.AddScoped<T>()`
- Inject `ILogger<T>` via constructor
- Return result objects (not primitive types) for complex operations
- Throw `ArgumentException` for validation errors
- Use nested classes for result types when they are service-specific

### Error Handling
- **Client errors (400)**: Return `BadRequest(new { error = "message" })`
- **Server errors (500)**: Return `StatusCode(500, new { error = "...", details = "..." })`
- **Logging**: Use `LogWarning` for client errors, `LogError` for exceptions
- Always use structured logging with named placeholders: `_logger.LogInformation("Processing: Size={Size}", size)`

### Documentation
- All public classes and methods must have XML documentation comments (`/// <summary>`)
- Include `<param>` tags for method parameters
- Include `<returns>` tags describing return values
- Add `<response>` tags for HTTP status codes in controllers

## Test Guidelines

### Test Class Structure
```csharp
public class ExampleControllerTests
{
    private readonly ExampleController _controller;

    public ExampleControllerTests()
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<ExampleController>();
        _controller = new ExampleController(logger);
    }

    #region ActionName Tests

    [Fact]
    public void ActionName_Scenario_ExpectedBehavior()
    {
        // Arrange
        var request = new ExampleRequest { ... };

        // Act
        var result = _controller.ActionName(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        var response = result.Value as ExampleResponse;
        Assert.NotNull(response);
        Assert.Equal(expected, response.Property);
    }

    #endregion
}
```

### Test Naming Convention
- Format: `MethodName_Scenario_ExpectedBehavior`
- Examples:
  - `Resize_ResizesJpegImage_WhenWidthSpecified`
  - `IsMatch_ValidPattern_ReturnsTrue`
  - `GenerateBarcode_ReturnsBadRequest_WhenContentIsEmpty`

### Test Organization
- One test class per controller: `{ControllerName}Tests.cs`
- Use `#region` to group tests by controller action
- Order: success cases first, then validation/error cases, then edge cases

### Test Coverage Requirements
1. **Success paths**: Test all valid input combinations
2. **Validation failures**: Test each validation rule (empty input, invalid format, out of range)
3. **Options/parameters**: Test different option combinations
4. **Edge cases**: Empty strings, special characters, boundary values
5. **Format-specific tests**: For APIs supporting multiple formats (PNG/SVG, JPEG/PNG)

### Test Data
- Create helper methods for generating test data:
  ```csharp
  private string CreateTestImageBase64(int width, int height, SKEncodedImageFormat format)
  ```
- Add comments explaining non-obvious test data:
  ```csharp
  Content = "4901234567894", // Valid EAN-13 (checksum digit: 4)
  ```
- Use realistic test data that represents actual use cases

### Assertions Pattern
```csharp
// 1. Cast result to expected type
var result = _controller.Action(request) as OkObjectResult;

// 2. Assert result is not null
Assert.NotNull(result);

// 3. Assert status code
Assert.Equal(200, result.StatusCode);

// 4. Cast Value to response type
var response = result.Value as ExampleResponse;

// 5. Assert response is not null
Assert.NotNull(response);

// 6. Assert response properties
Assert.Equal(expected, response.Property);
Assert.True(response.Count > 0);
Assert.Contains(response.Items, item => item.Name == "expected");
```

### Theory Tests
Use `[Theory]` with `[InlineData]` for testing multiple similar cases:
```csharp
[Theory]
[InlineData("L")]
[InlineData("M")]
[InlineData("Q")]
[InlineData("H")]
public void GenerateQrCode_AcceptsAllErrorCorrectionLevels(string level)
{
    var request = new QrCodeRequest { Content = "Test", ErrorCorrectionLevel = level };
    var result = _controller.GenerateQrCode(request) as OkObjectResult;
    Assert.NotNull(result);
    Assert.Equal(200, result.StatusCode);
}
```
