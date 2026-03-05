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
    /// Executes a SPARQL COUNT query and returns the count result.
    /// Used for pagination to get the total number of results matching the search criteria.
    /// </summary>
    /// <param name="countQuery">The SPARQL COUNT query string to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total count of results matching the query filters.</returns>
    /// <exception cref="ArgumentException">Thrown if the query is null or empty.</exception>
    /// <exception cref="SparqlTimeoutException">Thrown if the query times out.</exception>
    /// <exception cref="SparqlEndpointException">Thrown if the endpoint returns an error.</exception>
    /// <exception cref="SparqlQueryException">Thrown if the result cannot be parsed.</exception>
    public async Task<int> ExecuteCountQueryAsync(
        string countQuery,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(countQuery))
            throw new ArgumentException("SPARQL count query cannot be null or empty.", nameof(countQuery));

        _logger.LogInformation("Executing SPARQL COUNT query against endpoint {EndpointUrl}", _options.Url);

        var startTime = DateTime.UtcNow;
        try
        {
            // Create request with SPARQL query as URL-encoded parameter
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("query", countQuery)
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
                "SPARQL COUNT query completed in {ElapsedMs}ms with status {StatusCode}",
                elapsed.TotalMilliseconds,
                (int)response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "SPARQL endpoint returned error status {StatusCode}: {ErrorContent}",
                    (int)response.StatusCode,
                    errorContent);
                throw new SparqlEndpointException(
                    $"SPARQL endpoint returned error: {(int)response.StatusCode}",
                    (int)response.StatusCode);
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("SPARQL COUNT response: {Response}", jsonContent);

            return ParseSparqlCountResult(jsonContent);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning("SPARQL COUNT query was cancelled after {TimeoutSeconds}s", _options.TimeoutSeconds);
            throw new SparqlTimeoutException(
                $"SPARQL COUNT query was cancelled after {_options.TimeoutSeconds} seconds.",
                TimeSpan.FromSeconds(_options.TimeoutSeconds),
                ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error executing SPARQL COUNT query");
            throw new SparqlException("HTTP error executing SPARQL COUNT query.", ex);
        }
        catch (SparqlException)
        {
            throw; // Re-throw SPARQL exceptions as-is
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error executing SPARQL COUNT query");
            throw new SparqlException("Unexpected error executing SPARQL COUNT query.", ex);
        }
    }

    /// <summary>
    /// Parses a SPARQL COUNT query result and returns the integer count.
    /// </summary>
    private int ParseSparqlCountResult(string jsonContent)
    {
        try
        {
            var doc = JsonDocument.Parse(jsonContent);
            var results = doc.RootElement.GetProperty("results");
            var bindings = results.GetProperty("bindings");

            // COUNT query should return exactly one binding
            var bindingsArray = bindings.EnumerateArray().ToList();
            if (bindingsArray.Count == 0)
            {
                _logger.LogWarning("SPARQL COUNT query returned no results");
                return 0;
            }

            var binding = bindingsArray[0];
            var countStr = GetBindingValue(binding, "count");

            if (string.IsNullOrEmpty(countStr) || !int.TryParse(countStr, out var count))
            {
                _logger.LogWarning("SPARQL COUNT query returned invalid count value: {CountValue}", countStr);
                return 0;
            }

            _logger.LogInformation("SPARQL COUNT query returned: {Count}", count);
            return count;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse SPARQL COUNT JSON result");
            throw new SparqlQueryException("Failed to parse SPARQL COUNT JSON result.", null, ex);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "SPARQL COUNT result missing expected structure");
            throw new SparqlQueryException("SPARQL COUNT result missing expected structure (results.bindings).", null, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error parsing SPARQL COUNT result");
            throw new SparqlQueryException("Unexpected error parsing SPARQL COUNT result.", null, ex);
        }
    }

    /// <summary>
    /// Parses SPARQL JSON results into a list of PropertySaleRecord objects.
    /// Handles the W3C SPARQL JSON Results Format with HM Land Registry PPD ontology variables.
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
                // Build address from separate components (paon, saon, street, town, county)
                var paon = GetBindingValue(binding, "paon");
                var saon = GetBindingValue(binding, "saon");
                var street = GetBindingValue(binding, "street");
                var town = GetBindingValue(binding, "town");
                var county = GetBindingValue(binding, "county");
                
                // Combine address parts
                var addressParts = new[] { paon, saon, street, town, county }
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList();
                var fullAddress = string.Join(", ", addressParts);

                var record = new PropertySaleRecord
                {
                    PropertyUri = null, // HM Land Registry doesn't return property URI in PPD queries
                    Address = fullAddress,
                    Postcode = GetBindingValue(binding, "postcode"),
                    PropertyType = GetBindingValue(binding, "propertyType"),
                    RetrievedAt = DateTime.UtcNow
                };

                // Parse price (should be numeric, HM Land Registry returns as amount)
                var priceStr = GetBindingValue(binding, "amount");
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
