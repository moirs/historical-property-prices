using Polly;
using Polly.Extensions.Http;
using PropertyPrices.Api.Models;
using PropertyPrices.Core;
using PropertyPrices.Core.Sparql;
using PropertyPrices.Core.Transformations;
using PropertyPrices.Infrastructure.Data;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, loggerConfig) =>
{
    var logLevel = context.Configuration.GetValue<string>("Logging:LogLevel:Default") ?? "Information";

    loggerConfig
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "PropertyPrices.Api")
        .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

    if (builder.Environment.IsDevelopment())
    {
        loggerConfig.MinimumLevel.Debug();
    }
});

// Add services
builder.Services.AddOpenApi();

// Configure SPARQL options
var sparqlOptions = new SparqlEndpointOptions();
builder.Configuration.GetSection("SparqlEndpoint").Bind(sparqlOptions);
builder.Services.AddSingleton(sparqlOptions);

// Configure Pagination options
var paginationOptions = new PaginationOptions();
builder.Configuration.GetSection("Pagination").Bind(paginationOptions);
builder.Services.AddSingleton(paginationOptions);

// Configure HTTP client for SPARQL queries with Polly resilience policies
var sparqlHttpPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .Or<HttpRequestException>()
    .OrResult(r => (int)r.StatusCode == 503) // Service Unavailable
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt =>
            TimeSpan.FromSeconds(Math.Pow(2, attempt))); // Exponential backoff: 2s, 4s, 8s

builder.Services
    .AddHttpClient<SparqlDataAccessClient>(client =>
    {
        client.BaseAddress = new Uri(sparqlOptions.Url);
        client.Timeout = TimeSpan.FromSeconds(sparqlOptions.TimeoutSeconds);
    });

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Health endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("Health")
    .Produces<HealthResponse>(StatusCodes.Status200OK)
    .WithDescription("Health check endpoint");

// Helper method to parse property type string to SPARQL code
static string? ParsePropertyTypeCode(string? typeString)
{
    if (string.IsNullOrWhiteSpace(typeString))
        return null;
    
    return typeString.ToUpperInvariant() switch
    {
        "D" or "DETACHED" => "D",
        "S" or "SEMI-DETACHED" => "S",
        "T" or "TERRACED" => "T",
        "F" or "FLAT" => "F",
        "O" or "OTHER" => "O",
        _ => null
    };
}

app.MapPost("/properties/search",
    async (PropertySearchRequest request, SparqlDataAccessClient dataAccessClient, PaginationOptions paginationOptions, ILogger<Program> log) =>
{
    try
    {
        // Validate request
        var validationErrors = request.Validate(paginationOptions.MaxPageSize);
        if (validationErrors.Count > 0)
        {
            return Results.BadRequest(new
            {
                title = "Validation failed",
                status = StatusCodes.Status400BadRequest,
                detail = string.Join("; ", validationErrors)
            });
        }

        // Build and execute SPARQL query using SparqlQueryBuilder from core project
        log.LogInformation("Searching properties with filters: postcode={Postcode}, dateFrom={DateFrom}, dateTo={DateTo}, priceMin={PriceMin}, priceMax={PriceMax}, propertyType={PropertyType}",
            request.Postcode, request.DateFrom, request.DateTo, request.PriceMin, request.PriceMax, request.PropertyType);

        // Build query using fluent query builder
        var queryBuilder = new SparqlQueryBuilder();
        
        if (!string.IsNullOrEmpty(request.Postcode))
        {
            queryBuilder.WithPostcode(request.Postcode);
        }
        
        if (request.DateFrom.HasValue || request.DateTo.HasValue)
        {
            var startDate = request.DateFrom ?? new DateOnly(1995, 1, 1); // HM Land Registry data starts from 1995
            var endDate = request.DateTo ?? DateOnly.FromDateTime(DateTime.Today);
            queryBuilder.WithDateRange(startDate, endDate);
        }
        
        if (request.PriceMin.HasValue || request.PriceMax.HasValue)
        {
            queryBuilder.WithPriceRange(request.PriceMin, request.PriceMax);
        }
        
        if (!string.IsNullOrEmpty(request.PropertyType))
        {
            var propertyTypeCode = ParsePropertyTypeCode(request.PropertyType);
            if (!string.IsNullOrEmpty(propertyTypeCode))
            {
                queryBuilder.WithPropertyType(propertyTypeCode);
            }
        }
        
        if (request.PageSize > 0)
        {
            queryBuilder.WithPagination(request.PageSize, (request.PageNumber - 1) * request.PageSize);
        }
        
        var sparqlQuery = queryBuilder.Build();
        log.LogInformation("SPARQL Query to be executed: {Query}", sparqlQuery);

        var rawResults = await dataAccessClient.ExecuteQueryAsync(sparqlQuery);

        // Transform results
        var transformedResults = PropertySaleTransformer.TransformBulk(rawResults);

        // Results are already filtered and paginated at SPARQL level
        var paginatedResults = transformedResults
            .Select(x => new PropertyDto
            {
                Address = x.Address.StreetName,
                Postcode = x.Address.Postcode,
                PostcodeArea = x.Address.PostcodeArea,
                Price = x.Price,
                TransactionDate = x.TransactionDate,
                PropertyType = x.PropertyType
            })
            .ToList();

        var response = new PropertySearchResponse
        {
            Results = paginatedResults,
            TotalCount = paginatedResults.Count,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        log.LogInformation("Search completed: found {TotalCount} properties, returning {Count} on page {PageNumber}",
            paginatedResults.Count, paginatedResults.Count, request.PageNumber);

        return Results.Ok(response);
    }
    catch (ArgumentException ex)
    {
        log.LogWarning("Invalid search request: {Message}", ex.Message);
        return Results.BadRequest(new
        {
            title = "Invalid search request",
            status = StatusCodes.Status400BadRequest,
            detail = ex.Message
        });
    }
    catch (OperationCanceledException ex)
    {
        log.LogError("Search request timed out: {Message}", ex.Message);
        return Results.StatusCode(StatusCodes.Status504GatewayTimeout);
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Error executing property search");
        return Results.Problem(
            detail: "An error occurred while searching for properties",
            title: "Internal server error",
            statusCode: StatusCodes.Status500InternalServerError);
    }
})
.WithName("Search Properties")
.Produces<PropertySearchResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status500InternalServerError)
.WithDescription("Search for properties with optional filters");

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("PropertyPrices API starting...");

app.Run();

record HealthResponse(string Status, DateTime Timestamp);

