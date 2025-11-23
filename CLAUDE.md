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

## Architecture

### Technology Stack
- **.NET 8**: Target framework for both projects
- **ASP.NET Core**: Web API framework with Controller-based architecture
- **Swagger/OpenAPI**: API documentation via Swashbuckle
- **xUnit**: Testing framework with test project referencing the server project

### Project Structure
The solution follows standard .NET project organization:
- Solution file (`.sln`) at root manages both projects
- Server project configured as Web SDK with OpenAPI/Swagger packages
- Controllers located in `PowerTools.Server/Controllers/` directory
- Models located in `PowerTools.Server/` directory root
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
3. Define route using `[Route("[controller]")]` or custom route
4. Create model classes as needed in the appropriate namespace
5. Implement action methods with appropriate HTTP verb attributes (`[HttpGet]`, `[HttpPost]`, etc.)
6. Follow the single-purpose API principle - each controller should focus on one specific utility function
