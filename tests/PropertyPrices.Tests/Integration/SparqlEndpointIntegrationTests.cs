using FluentAssertions;
using PropertyPrices.Core;
using PropertyPrices.Core.Sparql;
using PropertyPrices.Core.Sparql.Models;
using PropertyPrices.Infrastructure.Data;
using Xunit;

namespace PropertyPrices.Tests.Integration;

/// <summary>
/// Integration tests for the HM Land Registry SPARQL endpoint.
/// These tests run against the real endpoint and verify that queries execute successfully.
/// Note: These tests require internet connectivity and may be slow.
/// </summary>
[Trait("Category", "Integration")]
public class SparqlEndpointIntegrationTests
{
    private readonly SparqlDataAccessClient _client;
    private readonly string _endpointUrl = "http://landregistry.data.gov.uk/landregistry/sparql";

    public SparqlEndpointIntegrationTests()
    {
        var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        httpClient.BaseAddress = new Uri(_endpointUrl);
        
        var logger = new TestLogger();
        var options = new SparqlEndpointOptions { Url = _endpointUrl, TimeoutSeconds = 30 };
        
        _client = new SparqlDataAccessClient(httpClient, logger, options);
    }

    [Fact(Skip = "Integration test - requires internet connectivity")]
    public async Task ExecuteQuery_WithBasicQuery_ReturnsResults()
    {
        // Arrange - Build a simple query to get recent property transactions
        var query = new SparqlQueryBuilder()
            .WithPagination(limit: 10) // Get just 10 results for quick testing
            .Build();

        // Act
        var results = await _client.ExecuteQueryAsync(query);

        // Assert
        results.Should().NotBeNull();
        results.Should().NotBeEmpty("Basic query should return some results");
        
        // Verify structure of results
        results.ForEach(r =>
        {
            r.Address.Should().NotBeNullOrEmpty("Address should be populated");
            r.Postcode.Should().NotBeNullOrEmpty("Postcode should be populated");
            r.Price.Should().BeGreaterThan(0, "Price should be positive");
            r.TransactionDate.Should().NotBe(default(DateOnly), "Transaction date should be set");
        });
    }

    [Fact(Skip = "Integration test - requires internet connectivity")]
    public async Task ExecuteQuery_WithPostcodeFilter_ReturnFilteredResults()
    {
        // Arrange - Query for a specific postcode (using a known test postcode)
        // Note: This test uses a real UK postcode that should have some historical data
        var query = new SparqlQueryBuilder()
            .WithPostcode("SW1A 1AA") // UK Parliament postcode - should have results
            .WithPagination(limit: 10)
            .Build();

        // Act
        var results = await _client.ExecuteQueryAsync(query);

        // Assert
        // Results may be empty if no data exists for this postcode, so we just verify no errors
        results.Should().NotBeNull();
        if (results.Any())
        {
            // If results exist, verify they're valid
            results.All(r => r.Address != null && r.Postcode != null).Should().BeTrue();
        }
    }

    [Fact(Skip = "Integration test - requires internet connectivity")]
    public async Task ExecuteQuery_WithDateRange_ReturnsFilteredResults()
    {
        // Arrange - Query for properties sold in 2023
        var query = new SparqlQueryBuilder()
            .WithDateRange(
                startDate: new DateOnly(2023, 1, 1),
                endDate: new DateOnly(2023, 12, 31)
            )
            .WithPagination(limit: 10)
            .Build();

        // Act
        var results = await _client.ExecuteQueryAsync(query);

        // Assert
        results.Should().NotBeNull();
        results.Should().NotBeEmpty("Should find properties sold in 2023");
        
        results.ForEach(r =>
        {
            r.TransactionDate.Should().BeOnOrAfter(new DateOnly(2023, 1, 1));
            r.TransactionDate.Should().BeOnOrBefore(new DateOnly(2023, 12, 31));
        });
    }

    [Fact(Skip = "Integration test - requires internet connectivity")]
    public async Task ExecuteQuery_WithPagination_RespectsPaginationLimits()
    {
        // Arrange - Query with explicit limit
        var query = new SparqlQueryBuilder()
            .WithPagination(limit: 5)
            .Build();

        // Act
        var results = await _client.ExecuteQueryAsync(query);

        // Assert
        results.Should().NotBeNull();
        results.Count.Should().BeLessThanOrEqualTo(5, "Results should respect LIMIT clause");
    }

    [Fact(Skip = "Integration test - requires internet connectivity")]
    public async Task ExecuteQuery_WithMultipleFilters_CombinesFiltersCorrectly()
    {
        // Arrange - Query with multiple filters
        var query = new SparqlQueryBuilder()
            .WithPropertyTypeEnum(PropertyType.Detached)
            .WithDateRange(
                startDate: new DateOnly(2023, 1, 1),
                endDate: new DateOnly(2023, 12, 31)
            )
            .WithPagination(limit: 10)
            .Build();

        // Act
        var results = await _client.ExecuteQueryAsync(query);

        // Assert
        results.Should().NotBeNull();
        // Results may be empty if no detached properties in 2023, which is ok
        if (results.Any())
        {
            results.ForEach(r =>
            {
                r.TransactionDate.Should().BeOnOrAfter(new DateOnly(2023, 1, 1));
                r.TransactionDate.Should().BeOnOrBefore(new DateOnly(2023, 12, 31));
            });
        }
    }
}

/// <summary>
/// Test logger for integration tests - logs to console.
/// </summary>
public class TestLogger : Microsoft.Extensions.Logging.ILogger<SparqlDataAccessClient>
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
        Console.WriteLine($"[{logLevel}] {message}");
        if (exception != null)
            Console.WriteLine($"Exception: {exception}");
    }
}
