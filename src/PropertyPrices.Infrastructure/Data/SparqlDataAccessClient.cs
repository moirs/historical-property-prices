using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using PropertyPrices.Core;
using PropertyPrices.Core.Models;
using PropertyPrices.Infrastructure.Data.Exceptions;
using PropertyPrices.Infrastructure.Data.Models;

namespace PropertyPrices.Infrastructure.Data;

/// <summary>
/// HTTP client for executing SPARQL queries against the HM Land Registry endpoint.
/// Handles query execution, result parsing, timeouts, retries, and comprehensive logging.
/// </summary>
public class SparqlDataAccessClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SparqlDataAccessClient> _logger;
    private readonly SparqlEndpointOptions _options;

    /// <summary>Initializes a new instance of the SparqlDataAccessClient class.</summary>
    public SparqlDataAccessClient(
        HttpClient httpClient,
        ILogger<SparqlDataAccessClient> logger,
        SparqlEndpointOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Executes a SPARQL query and returns the results as a list of PropertySaleRecord objects.
    /// </summary>
    /// <param name="sparqlQuery">The SPARQL query string to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of PropertySaleRecord objects parsed from the SPARQL results.</returns>
    /// <exception cref="ArgumentException">Thrown if the query is null or empty.</exception>
    /// <exception cref="SparqlTimeoutException">Thrown if the query times out.</exception>
    /// <exception cref="SparqlEndpointException">Thrown if the endpoint returns an error.</exception>
    /// <exception cref="SparqlQueryException">Thrown if the results cannot be parsed.</exception>
    public async Task<List<PropertySaleRecord>> ExecuteQueryAsync(
        string sparqlQuery,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sparqlQuery))
            throw new ArgumentException("SPARQL query cannot be null or empty.", nameof(sparqlQuery));

        _logger.LogInformation("Executing SPARQL query against endpoint {EndpointUrl}", _options.Url);

        var startTime = DateTime.UtcNow;
        try
        {
            // Create request with SPARQL query as URL-encoded parameter
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("query", sparqlQuery)
            });

            var request = new HttpRequestMessage(HttpMethod.Post, _options.Url)
            {
                Content = content
            };

            // Set Accept header for JSON SPARQL results
            request.Headers.Add("Accept", "application/sparql-results+json");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            var elapsed = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "SPARQL query completed in {ElapsedMs}ms with status {StatusCode}",
                elapsed.TotalMilliseconds,
                (int)response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "SPARQL endpoint returned error status {StatusCode}: {ErrorContent}",
                    response.StatusCode,
                    errorContent);

                throw new SparqlEndpointException(
                    $"SPARQL endpoint returned HTTP {response.StatusCode}. Details: {errorContent}",
                    (int)response.StatusCode);
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var results = ParseSparqlJsonResults(jsonContent);

            _logger.LogInformation("Parsed {ResultCount} records from SPARQL results", results.Count);
            return results;
        }
        catch (OperationCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(
                ex,
                "SPARQL query timed out after {TimeoutSeconds}s",
                _options.TimeoutSeconds);

            throw new SparqlTimeoutException(
                $"SPARQL query timed out after {_options.TimeoutSeconds} seconds.",
                TimeSpan.FromSeconds(_options.TimeoutSeconds),
                ex);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "SPARQL query was cancelled");
            throw new SparqlException("SPARQL query was cancelled.", ex);
        }
        catch (SparqlException)
        {
            throw; // Re-throw SPARQL exceptions as-is
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error executing SPARQL query");
            throw new SparqlException("Unexpected error executing SPARQL query.", ex);
        }
    }

    /// <summary>
    /// Parses SPARQL JSON results into a list of PropertySaleRecord objects.
    /// Handles the W3C SPARQL JSON Results Format.
    /// </summary>
    private List<PropertySaleRecord> ParseSparqlJsonResults(string jsonContent)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var doc = JsonDocument.Parse(jsonContent);
            var results = doc.RootElement.GetProperty("results");
            var bindings = results.GetProperty("bindings");

            var records = new List<PropertySaleRecord>();

            foreach (var binding in bindings.EnumerateArray())
            {
                var record = new PropertySaleRecord
                {
                    PropertyUri = GetBindingValue(binding, "property"),
                    Address = GetBindingValue(binding, "address"),
                    Postcode = GetBindingValue(binding, "postcode"),
                    PropertyType = GetBindingValue(binding, "type"),
                    RetrievedAt = DateTime.UtcNow
                };

                // Parse price (should be numeric)
                var priceStr = GetBindingValue(binding, "price");
                if (!string.IsNullOrEmpty(priceStr) && decimal.TryParse(priceStr, out var price))
                {
                    record.Price = price;
                }

                // Parse transaction date (ISO format)
                var dateStr = GetBindingValue(binding, "date");
                if (!string.IsNullOrEmpty(dateStr) && DateOnly.TryParse(dateStr, out var date))
                {
                    record.TransactionDate = date;
                }

                records.Add(record);
            }

            return records;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse SPARQL JSON results");
            throw new SparqlQueryException("Failed to parse SPARQL JSON results. Invalid JSON format.", null, ex);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "SPARQL results missing expected structure");
            throw new SparqlQueryException("SPARQL results missing expected JSON structure.", null, ex);
        }
    }

    /// <summary>
    /// Extracts a variable value from a SPARQL result binding.
    /// </summary>
    private static string? GetBindingValue(JsonElement binding, string variable)
    {
        if (!binding.TryGetProperty(variable, out var element))
            return null;

        if (element.TryGetProperty("value", out var valueElement))
            return valueElement.GetString();

        return null;
    }
}
