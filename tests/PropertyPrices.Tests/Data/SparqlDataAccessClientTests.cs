using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using PropertyPrices.Core;
using PropertyPrices.Core.Models;
using PropertyPrices.Infrastructure.Data;
using PropertyPrices.Infrastructure.Data.Exceptions;
using PropertyPrices.Infrastructure.Data.Models;
using Xunit;

namespace PropertyPrices.Tests.Data;

public class SparqlDataAccessClientTests
{
    private readonly Mock<ILogger<SparqlDataAccessClient>> _mockLogger = new();
    private readonly SparqlEndpointOptions _options = new()
    {
        Url = "http://test.sparql.endpoint/query",
        TimeoutSeconds = 30
    };

    [Fact]
    public async Task ExecuteQueryAsync_WithValidQuery_ReturnsPropertySaleRecords()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(_options.Url)
        };

        var sparqlResults = CreateSparqlJsonResponse(new[]
        {
            ("property", "http://example.com/property/1"),
            ("address", "123 Main Street"),
            ("postcode", "SW1A1AA"),
            ("price", "250000"),
            ("date", "2022-03-15"),
            ("type", "D")
        });

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(sparqlResults)
            });

        var client = new SparqlDataAccessClient(httpClient, _mockLogger.Object, _options);

        // Act
        var result = await client.ExecuteQueryAsync("SELECT ?property WHERE { ?property a ppd:PricePaidRecord . }");

        // Assert
        result.Should().HaveCount(1);
        result[0].PropertyUri.Should().Be("http://example.com/property/1");
        result[0].Address.Should().Be("123 Main Street");
        result[0].Postcode.Should().Be("SW1A1AA");
        result[0].Price.Should().Be(250000m);
        result[0].TransactionDate.Should().Be(new DateOnly(2022, 3, 15));
        result[0].PropertyType.Should().Be("D");
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithMultipleResults_ReturnsAllRecords()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(_options.Url)
        };

        var sparqlResults = CreateSparqlJsonResponse(
            new[] { ("property", "http://example.com/1"), ("address", "Address 1"), ("postcode", "SW1A1AA"), ("price", "100000"), ("date", "2020-01-01"), ("type", "D") },
            new[] { ("property", "http://example.com/2"), ("address", "Address 2"), ("postcode", "M11AA"), ("price", "200000"), ("date", "2021-06-15"), ("type", "F") }
        );

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(sparqlResults)
            });

        var client = new SparqlDataAccessClient(httpClient, _mockLogger.Object, _options);

        // Act
        var result = await client.ExecuteQueryAsync("SELECT ?property WHERE { ?property a ppd:PricePaidRecord . }");

        // Assert
        result.Should().HaveCount(2);
        result[0].Address.Should().Be("Address 1");
        result[1].Address.Should().Be("Address 2");
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithNullOrEmptyQuery_ThrowsArgumentException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var client = new SparqlDataAccessClient(httpClient, _mockLogger.Object, _options);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => client.ExecuteQueryAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => client.ExecuteQueryAsync(null!));
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithEndpointError_ThrowsSparqlEndpointException()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(_options.Url)
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.InternalServerError,
                Content = new StringContent("Internal Server Error")
            });

        var client = new SparqlDataAccessClient(httpClient, _mockLogger.Object, _options);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<SparqlEndpointException>(
            () => client.ExecuteQueryAsync("SELECT ?x WHERE { ?x a ?type . }"));

        ex.StatusCode.Should().Be(500);
        ex.Message.Should().Contain("InternalServerError");
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithMalformedJson_ThrowsSparqlQueryException()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(_options.Url)
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{ invalid json }")
            });

        var client = new SparqlDataAccessClient(httpClient, _mockLogger.Object, _options);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<SparqlQueryException>(
            () => client.ExecuteQueryAsync("SELECT ?x WHERE { ?x a ?type . }"));

        ex.Message.Should().Contain("parse");
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithMissingResults_ThrowsSparqlQueryException()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(_options.Url)
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{ \"head\": { \"vars\": [] } }")
            });

        var client = new SparqlDataAccessClient(httpClient, _mockLogger.Object, _options);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<SparqlQueryException>(
            () => client.ExecuteQueryAsync("SELECT ?x WHERE { ?x a ?type . }"));

        ex.Message.Should().Contain("structure");
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithPartialData_ParsesAvailableFields()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(_options.Url)
        };

        var sparqlResults = CreateSparqlJsonResponse(new[]
        {
            ("property", "http://example.com/property/1"),
            ("address", "123 Main Street"),
            ("postcode", "SW1A1AA"),
            ("type", "D")
        });

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(sparqlResults)
            });

        var client = new SparqlDataAccessClient(httpClient, _mockLogger.Object, _options);

        // Act
        var result = await client.ExecuteQueryAsync("SELECT ?property WHERE { ?property a ppd:PricePaidRecord . }");

        // Assert
        result.Should().HaveCount(1);
        result[0].Address.Should().Be("123 Main Street");
        result[0].Price.Should().BeNull();
        result[0].TransactionDate.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithInvalidPrice_SkipsPrice()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(_options.Url)
        };

        var sparqlResults = CreateSparqlJsonResponse(new[]
        {
            ("property", "http://example.com/property/1"),
            ("address", "123 Main Street"),
            ("postcode", "SW1A1AA"),
            ("price", "not-a-number"),
            ("date", "2022-03-15"),
            ("type", "D")
        });

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(sparqlResults)
            });

        var client = new SparqlDataAccessClient(httpClient, _mockLogger.Object, _options);

        // Act
        var result = await client.ExecuteQueryAsync("SELECT ?property WHERE { ?property a ppd:PricePaidRecord . }");

        // Assert
        result.Should().HaveCount(1);
        result[0].Price.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithEmptyResults_ReturnsEmptyList()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(_options.Url)
        };

        var sparqlResults = @"{ ""head"": { ""vars"": [""property""] }, ""results"": { ""bindings"": [] } }";

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(sparqlResults)
            });

        var client = new SparqlDataAccessClient(httpClient, _mockLogger.Object, _options);

        // Act
        var result = await client.ExecuteQueryAsync("SELECT ?property WHERE { ?property a ppd:PricePaidRecord . }");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void SparqlDataAccessClient_WithNullHttpClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new SparqlDataAccessClient(null!, _mockLogger.Object, _options));

        ex.ParamName.Should().Be("httpClient");
    }

    [Fact]
    public void SparqlDataAccessClient_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new SparqlDataAccessClient(httpClient, null!, _options));

        ex.ParamName.Should().Be("logger");
    }

    [Fact]
    public void SparqlDataAccessClient_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new SparqlDataAccessClient(httpClient, _mockLogger.Object, null!));

        ex.ParamName.Should().Be("options");
    }

    private static string CreateSparqlJsonResponse(params (string variable, string value)[][] bindings)
    {
        var bindingsArray = new System.Collections.Generic.List<object>();

        foreach (var binding in bindings)
        {
            var bindingDict = new Dictionary<string, object>();
            foreach (var (variable, value) in binding)
            {
                bindingDict[variable] = new { value };
            }
            bindingsArray.Add(bindingDict);
        }

        var response = new
        {
            head = new { vars = new[] { "property", "address", "postcode", "price", "date", "type" } },
            results = new { bindings = bindingsArray }
        };

        return JsonSerializer.Serialize(response);
    }
}
