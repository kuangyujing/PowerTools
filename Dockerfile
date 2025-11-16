# PowerTools - ASP.NET Core Web API Dockerfile
# Multi-stage build for optimized image size

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["PowerTools.sln", "./"]
COPY ["PowerTools.Server/PowerTools.Server.csproj", "PowerTools.Server/"]
COPY ["PowerTools.Tests/PowerTools.Tests.csproj", "PowerTools.Tests/"]
COPY ["global.json", "./"]

# Restore dependencies
RUN dotnet restore "PowerTools.sln"

# Copy all source files
COPY . .

# Build the application
WORKDIR "/src/PowerTools.Server"
RUN dotnet build "PowerTools.Server.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "PowerTools.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Create a non-root user
RUN adduser --disabled-password --gecos "" --uid 1000 appuser

WORKDIR /app

# Copy published application from publish stage
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port 8080 (ASP.NET Core 8 default)
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:8080/api/encodingconverter/encodings || exit 1

# Start the application
ENTRYPOINT ["dotnet", "PowerTools.Server.dll"]
