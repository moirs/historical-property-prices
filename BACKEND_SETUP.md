# Backend Architecture Skeleton

This directory contains the ASP.NET Core backend skeleton for the UK Historical Property Prices application.

## Project Structure

- **src/PropertyPrices.Api** - Main ASP.NET Core API application
- **src/PropertyPrices.Core** - Domain models, interfaces, and business logic
- **src/PropertyPrices.Infrastructure** - Data access, external service clients, configuration
- **tests/PropertyPrices.Tests** - Unit and integration tests

## Quick Start

### Build
```bash
dotnet build
```

### Run Tests
```bash
# All tests
dotnet test

# Specific test file
dotnet test tests/PropertyPrices.Tests/HealthCheckTests.cs

# With verbosity
dotnet test --verbosity detailed
```

### Run API Locally
```bash
dotnet run --project src/PropertyPrices.Api
```

The API will start on `https://localhost:7123` (HTTPS) or `http://localhost:5000` (HTTP).

### Health Check
Once running, verify the API is healthy:
```bash
curl http://localhost:5000/health
```

Expected response:
```json
{
  "status": "healthy",
  "timestamp": "2026-02-24T15:30:00Z"
}
```

## Configuration

Configuration is managed through:
- **appsettings.json** - Base configuration
- **appsettings.Development.json** - Development overrides
- **Environment variables** - Can override any setting

Key settings:
- `SparqlEndpoint:Url` - HM Land Registry SPARQL endpoint
- `SparqlEndpoint:TimeoutSeconds` - Query timeout
- `Logging:LogLevel:Default` - Log level (Debug/Information/Warning/Error)

## Logging

Serilog is configured for structured logging:
- **Console output** - Formatted logs to stdout
- **Development mode** - Debug level logging enabled
- **Production** - Information level and above
- **Structured context** - All logs include application name and timestamp

## Code Style

EditorConfig is enabled for consistent formatting:
- 4-space indentation
- UTF-8 encoding
- Trailing whitespace trimmed
- Final newlines enforced

## CI/CD

GitHub Actions workflow (`.github/workflows/ci.yml`) runs on every push/PR:
1. Build (Release configuration)
2. Run tests
3. Check code formatting

## Next Steps

This skeleton provides:
- ✅ Modular project structure
- ✅ Serilog structured logging
- ✅ Configuration management
- ✅ Health endpoint for verification
- ✅ Unit test infrastructure
- ✅ CI pipeline

Implement next:
1. **SPARQL Query Builder** - Dynamic query construction
2. **Data Access Layer** - SPARQL client with retry/timeout logic
3. **Domain Transformation** - RDF to domain model mapping
4. **API Endpoints** - Property search/filter endpoints
5. **Caching** - Optional performance optimization
