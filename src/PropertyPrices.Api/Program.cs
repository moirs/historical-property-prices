using Polly;
using Polly.Extensions.Http;
using PropertyPrices.Api.Models;
using PropertyPrices.Core;
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

// Helper function to build SPARQL query
static string BuildSparqlQuery(PropertySearchRequest request)
{
    // HM Land Registry SPARQL endpoint uses correct Land Registry ontologies:
    // lrcommon: http://landregistry.data.gov.uk/def/common/ - Address properties
    // lrppi: http://landregistry.data.gov.uk/def/ppi/ - Price Paid Data properties
    // skos: http://www.w3.org/2004/02/skos/core# - For category labels
    
    var query = new StringBuilder();
    
    // Add prefixes
    query.AppendLine("PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>");
    query.AppendLine("PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>");
    query.AppendLine("PREFIX owl: <http://www.w3.org/2002/07/owl#>");
    query.AppendLine("PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>");
    query.AppendLine("PREFIX sr: <http://data.ordnancesurvey.co.uk/ontology/spatialrelations/>");
    query.AppendLine("PREFIX ukhpi: <http://landregistry.data.gov.uk/def/ukhpi/>");
    query.AppendLine("PREFIX lrppi: <http://landregistry.data.gov.uk/def/ppi/>");
    query.AppendLine("PREFIX skos: <http://www.w3.org/2004/02/skos/core#>");
    query.AppendLine("PREFIX lrcommon: <http://landregistry.data.gov.uk/def/common/>");
    query.AppendLine();
    
    // SELECT clause
    query.AppendLine("SELECT ?paon ?saon ?street ?town ?county ?postcode ?amount ?date ?category");
    query.AppendLine("WHERE {");
    
    // VALUES clause for postcode if provided
    if (!string.IsNullOrEmpty(request.Postcode))
    {
        // Normalize postcode: trim, uppercase, and ensure single space between parts
        // UK postcodes format: "AA9A 9AA" or similar (with space)
        var normalized = request.Postcode.Trim().ToUpper();
        // Split on whitespace and rejoin with single space to normalize multiple spaces
        var parts = normalized.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var normalizedPostcode = string.Join(" ", parts);
        query.AppendLine($"  VALUES ?postcode {{\"{normalizedPostcode}\"^^xsd:string}}");
    }
    
    // Core query pattern
    query.AppendLine("  ?addr lrcommon:postcode ?postcode .");
    query.AppendLine("  ?transx lrppi:propertyAddress ?addr ;");
    query.AppendLine("          lrppi:pricePaid ?amount ;");
    query.AppendLine("          lrppi:transactionDate ?date ;");
    query.AppendLine("          lrppi:transactionCategory/skos:prefLabel ?category .");
    
    // Optional address components
    query.AppendLine("  OPTIONAL {?addr lrcommon:county ?county}");
    query.AppendLine("  OPTIONAL {?addr lrcommon:paon ?paon}");
    query.AppendLine("  OPTIONAL {?addr lrcommon:saon ?saon}");
    query.AppendLine("  OPTIONAL {?addr lrcommon:street ?street}");
    query.AppendLine("  OPTIONAL {?addr lrcommon:town ?town}");
    
    // Add date range filters if provided
    if (request.DateFrom.HasValue)
    {
        var startDate = request.DateFrom.Value.ToString("yyyy-MM-dd");
        query.AppendLine($"  FILTER(?date >= \"{startDate}\"^^xsd:date)");
    }
    
    if (request.DateTo.HasValue)
    {
        var endDate = request.DateTo.Value.ToString("yyyy-MM-dd");
        query.AppendLine($"  FILTER(?date <= \"{endDate}\"^^xsd:date)");
    }
    
    query.AppendLine("}");
    query.AppendLine("ORDER BY ?amount");
    
    // Add pagination
    if (request.PageSize > 0)
    {
        query.AppendLine($"LIMIT {request.PageSize}");
    }
    
    if (request.PageNumber > 1)
    {
        var offset = (request.PageNumber - 1) * request.PageSize;
        query.AppendLine($"OFFSET {offset}");
    }
    
    return query.ToString();
}

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

// Property search endpoint
app.MapPost("/properties/search", 
    async (PropertySearchRequest request, SparqlDataAccessClient dataAccessClient, ILogger<Program> log) =>
{
    try
    {
        // Validate request
        var validationErrors = request.Validate();
        if (validationErrors.Count > 0)
        {
            return Results.BadRequest(new
            {
                title = "Validation failed",
                status = StatusCodes.Status400BadRequest,
                detail = string.Join("; ", validationErrors)
            });
        }

        // Build and execute SPARQL query
        log.LogInformation("Searching properties with filters: postcode={Postcode}, dateFrom={DateFrom}, dateTo={DateTo}, priceMin={PriceMin}, priceMax={PriceMax}",
            request.Postcode, request.DateFrom, request.DateTo, request.PriceMin, request.PriceMax);

        // For now, execute a simple query without using the query builder
        // (Query builder would need to be enhanced for complex queries)
        var sparqlQuery = BuildSparqlQuery(request);
        log.LogInformation("SPARQL Query to be executed: {Query}", sparqlQuery);

        var rawResults = await dataAccessClient.ExecuteQueryAsync(sparqlQuery);

        // Transform results
        var transformedResults = PropertySaleTransformer.TransformBulk(rawResults);

        // Apply filters
        var filtered = transformedResults
            .FilterByPostcodeArea(request.Postcode ?? "")
            .Where(x => request.DateFrom == null || x.TransactionDate >= request.DateFrom)
            .Where(x => request.DateTo == null || x.TransactionDate <= request.DateTo)
            .Where(x => request.PriceMin == null || x.Price >= request.PriceMin)
            .Where(x => request.PriceMax == null || x.Price <= request.PriceMax)
            .ToList();

        // Apply pagination
        var totalCount = filtered.Count;
        var paginatedResults = filtered
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new PropertyDto
            {
                Address = x.Address.StreetName,
                Postcode = x.Address.Postcode,
                PostcodeArea = x.Address.PostcodeArea,
                Price = x.Price,
                TransactionDate = x.TransactionDate
            })
            .ToList();

        var response = new PropertySearchResponse
        {
            Results = paginatedResults,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        log.LogInformation("Search completed: found {TotalCount} properties, returning {Count} on page {PageNumber}",
            totalCount, paginatedResults.Count, request.PageNumber);

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

