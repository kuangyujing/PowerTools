# CLAUDE.md

## Language Guidelines
- **English**: All code, comments, and this file
- **Japanese**: `docs/` directory and `README.md`
- **Communication**: Respond in Japanese

## Commands
```bash
dotnet build                    # Build solution
dotnet test                     # Run tests
dotnet run --project PowerTools.Server  # Run server (Swagger at /swagger)
dotnet watch run --project PowerTools.Server  # Dev with hot reload
docker build -t powertools:latest .     # Build Docker image
docker-compose up -d            # Run with docker-compose
```

## Project Overview

PowerTools is a .NET 8 ASP.NET Core Web API - a **Swiss Army knife for Power Platform** providing lightweight, single-purpose APIs.

**Structure:**
- `PowerTools.Server/` - Main API project
  - `Controllers/` - API controllers
  - `Models/` - Request/Response models
  - `Services/` - Business logic services
- `PowerTools.Tests/` - xUnit test project

**Tech Stack:** .NET 8, ASP.NET Core, Swagger, xUnit, SkiaSharp, ZXing.Net, ClosedXML, UtfUnknown

## Implemented APIs

| API | Endpoints | Description |
|-----|-----------|-------------|
| `/api/image` | POST `/resize`, `/info` | Image resize (JPEG/PNG), metadata |
| `/api/barcode` | POST `/qrcode`, `/barcode`, GET `/types` | QR/Barcode generation (PNG/SVG) |
| `/api/excel` | POST `/sheet`, `/range`, `/table`, `/sheets`, `/tables` | Excel to JSON conversion |
| `/api/regex` | POST `/ismatch`, `/match`, `/matches`, `/replace`, `/split` | Regex operations (ReDoS protected) |
| `/api/charactercounter` | POST `/count` | Character type counting (JP support) |
| `/api/encodingconverter` | POST `/convert`, GET `/encodings` | File encoding conversion |
| `/api/storage` | POST `/upload`, `/get`, `/delete` | Temporary file storage with expiration |

## Coding Conventions

### Naming
- **Namespaces**: `PowerTools.Server.{Controllers|Models|Services}`
- **Files**: `{Name}Controller.cs`, `{Feature}Models.cs`
- **Models**: `*Request` for input, `*Response` for output

### Controller Pattern
```csharp
[ApiController]
[Route("api/[controller]")]
public class ExampleController : ControllerBase
{
    private readonly ILogger<ExampleController> _logger;

    public ExampleController(ILogger<ExampleController> logger) => _logger = logger;

    [HttpPost("action")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ExampleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Action([FromBody] ExampleRequest request)
    {
        try
        {
            // Validation -> BadRequest(new { error = "message" })
            // Business logic
            return Ok(response);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex) { return StatusCode(500, new { error = "...", details = ex.Message }); }
    }
}
```

### Model Pattern
```csharp
public class ExampleRequest
{
    [Required] public string RequiredField { get; set; } = string.Empty;
    public string? OptionalField { get; set; }
    [Range(1, 100)] public int Size { get; set; } = 50;
    public List<string> Items { get; set; } = new();
}
```

### Key Rules
- Use XML doc comments (`/// <summary>`) on all public members
- Structured logging: `_logger.LogInformation("Action: Param={Param}", param)`
- Services: Register with `AddScoped<T>()`, throw `ArgumentException` for validation
- Error responses: `{ error: "message" }` for 400, `{ error: "...", details: "..." }` for 500

## Test Guidelines

### Structure
- One test class per controller: `{Controller}Tests.cs`
- Naming: `MethodName_Scenario_ExpectedBehavior`
- Use `#region` to group by action
- AAA pattern: Arrange, Act, Assert

### Test Pattern
```csharp
public class ExampleControllerTests
{
    private readonly ExampleController _controller;

    public ExampleControllerTests()
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<ExampleController>();
        _controller = new ExampleController(logger);
    }

    [Fact]
    public void Action_ValidInput_ReturnsOk()
    {
        var request = new ExampleRequest { RequiredField = "test" };
        var result = _controller.Action(request) as OkObjectResult;
        Assert.NotNull(result);
        var response = result.Value as ExampleResponse;
        Assert.NotNull(response);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("B")]
    public void Action_MultipleInputs_Works(string input) { /* ... */ }
}
```

### Coverage Requirements
1. Success paths with valid inputs
2. Validation failures (empty, invalid format, out of range)
3. Edge cases (special characters, boundary values)
4. Format variations (PNG/SVG, JPEG/PNG)

## Deployment

- **Production**: Auto-deployed via [Railway](https://railway.app/) on push to main
- **Container**: Multi-stage Dockerfile, runs as non-root user, health check at `/api/health`
- **Environment**: `ASPNETCORE_URLS=http://+:8080`, `ASPNETCORE_ENVIRONMENT`
