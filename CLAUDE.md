# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PowerTools is a .NET 8 ASP.NET Core Web API solution for Power Platform tools and utilities. The solution consists of:
- **PowerTools.Server**: Main ASP.NET Core Web API project with Swagger/OpenAPI support
- **PowerTools.Tests**: xUnit test project

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

## Architecture

### Technology Stack
- **.NET 8**: Target framework for both projects
- **ASP.NET Core**: Web API framework with minimal API pattern
- **Swagger/OpenAPI**: API documentation via Swashbuckle
- **xUnit**: Testing framework with test project referencing the server project

### Project Structure
The solution follows standard .NET project organization:
- Solution file (`.sln`) at root manages both projects
- Server project configured as Web SDK with OpenAPI/Swagger packages
- Test project configured with xUnit and references the server project for integration testing
- Minimal API pattern in `Program.cs` with inline endpoint definitions

### Key Implementation Details
- The server currently implements a sample weather forecast endpoint demonstrating minimal API pattern
- Swagger UI is enabled in development environment for API exploration
- HTTPS redirection is configured by default
- Test project is set up with xUnit and code coverage collection support via coverlet