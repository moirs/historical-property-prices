using FluentAssertions;
using PropertyPrices.Api.Models;
using PropertyPrices.Core;
using PropertyPrices.Infrastructure.Data;
using Xunit;

namespace PropertyPrices.Tests.Api;

/// <summary>
/// Integration tests for the Property Search API endpoint with real SPARQL backend.
/// These tests verify the end-to-end flow from HTTP request to database response.
/// Tests are skipped by default as they require running API and internet connectivity.
/// </summary>
[Trait("Category", "Integration")]
public class PropertySearchEndpointIntegrationTests
{
    private readonly SparqlDataAccessClient _dataAccessClient;

    public PropertySearchEndpointIntegrationTests()
    {
        var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var logger = new TestApiLogger();
        var options = new SparqlEndpointOptions 
        { 
            Url = "http://landregistry.data.gov.uk/landregistry/sparql",
            TimeoutSeconds = 30 
        };
        httpClient.BaseAddress = new Uri(options.Url);
        
        _dataAccessClient = new SparqlDataAccessClient(httpClient, logger, options);
    }

    [Fact(Skip = "Integration test - requires API running with real endpoint")]
    public async Task SearchProperties_WithBasicRequest_ReturnsValidResponse()
    {
        // Arrange
        var request = new PropertySearchRequest
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var results = await _dataAccessClient.ExecuteQueryAsync(
            "PREFIX ppd: <http://purl.org/voc/ppd#> PREFIX xsd: <http://www.w3.org/2001/XMLSchema#> SELECT DISTINCT ?address ?postcode ?price ?date WHERE { ?transaction ppd:propertyAddress ?address . ?transaction ppd:postcode ?postcode . ?transaction ppd:pricePaid ?price . ?transaction ppd:transactionDate ?date . } LIMIT 10");

        // Assert
        results.Should().NotBeNull();
        if (results.Any())
        {
            results.ForEach(r =>
            {
                r.Address.Should().NotBeNullOrEmpty();
                r.Postcode.Should().NotBeNullOrEmpty();
                r.Price.Should().BeGreaterThan(0);
            });
        }
    }

    [Fact(Skip = "Integration test - requires API running with real endpoint")]
    public async Task SearchProperties_WithPostcodeFilter_ReturnsFilteredResults()
    {
        // Arrange - Search for properties in a specific postcode
        var sparqlQuery = """
            PREFIX ppd: <http://purl.org/voc/ppd#>
            PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
            
            SELECT DISTINCT ?address ?postcode ?price ?date
            WHERE {
              ?transaction ppd:propertyAddress ?address .
              ?transaction ppd:postcode ?postcode .
              ?transaction ppd:pricePaid ?price .
              ?transaction ppd:transactionDate ?date .
              FILTER(?postcode = "SW1A1AA")
            }
            LIMIT 10
            """;

        // Act
        var results = await _dataAccessClient.ExecuteQueryAsync(sparqlQuery);

        // Assert
        results.Should().NotBeNull();
        if (results.Any())
        {
            results.All(r => r.Postcode?.Replace(" ", "").ToUpper() == "SW1A1AA").Should().BeTrue();
        }
    }

    [Fact(Skip = "Integration test - requires API running with real endpoint")]
    public async Task SearchProperties_WithDateRangeFilter_ReturnsFilteredResults()
    {
        // Arrange - Search for properties sold in a specific year
        var sparqlQuery = """
            PREFIX ppd: <http://purl.org/voc/ppd#>
            PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
            
            SELECT DISTINCT ?address ?postcode ?price ?date
            WHERE {
              ?transaction ppd:propertyAddress ?address .
              ?transaction ppd:postcode ?postcode .
              ?transaction ppd:pricePaid ?price .
              ?transaction ppd:transactionDate ?date .
              FILTER(?date >= "2023-01-01"^^xsd:date)
              FILTER(?date <= "2023-12-31"^^xsd:date)
            }
            LIMIT 10
            """;

        // Act
        var results = await _dataAccessClient.ExecuteQueryAsync(sparqlQuery);

        // Assert
        results.Should().NotBeNull();
        if (results.Any())
        {
            results.ForEach(r =>
            {
                r.TransactionDate.Should().BeOnOrAfter(new DateOnly(2023, 1, 1));
                r.TransactionDate.Should().BeOnOrBefore(new DateOnly(2023, 12, 31));
            });
        }
    }

    [Fact(Skip = "Integration test - requires API running with real endpoint")]
    public async Task SearchProperties_WithPriceFilter_ReturnsFilteredResults()
    {
        // Arrange - Search for properties in a specific price range
        var sparqlQuery = """
            PREFIX ppd: <http://purl.org/voc/ppd#>
            PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
            
            SELECT DISTINCT ?address ?postcode ?price ?date
            WHERE {
              ?transaction ppd:propertyAddress ?address .
              ?transaction ppd:postcode ?postcode .
              ?transaction ppd:pricePaid ?price .
              ?transaction ppd:transactionDate ?date .
              FILTER(?price >= 100000)
              FILTER(?price <= 500000)
            }
            LIMIT 10
            """;

        // Act
        var results = await _dataAccessClient.ExecuteQueryAsync(sparqlQuery);

        // Assert
        results.Should().NotBeNull();
        if (results.Any())
        {
            results.ForEach(r =>
            {
                r.Price.Should().BeGreaterThanOrEqualTo(100000);
                r.Price.Should().BeLessThanOrEqualTo(500000);
            });
        }
    }
}

/// <summary>
/// Test logger for API integration tests.
/// </summary>
public class TestApiLogger : Microsoft.Extensions.Logging.ILogger<SparqlDataAccessClient>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
    
    public void Log<TState>(
        Microsoft.Extensions.Logging.LogLevel logLevel,
        Microsoft.Extensions.Logging.EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        Console.WriteLine($"[API Integration Test] [{logLevel}] {message}");
        if (exception != null)
            Console.WriteLine($"Exception: {exception}");
    }
}
