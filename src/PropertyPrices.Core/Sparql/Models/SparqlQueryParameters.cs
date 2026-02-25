namespace PropertyPrices.Core.Sparql.Models;

/// <summary>
/// Encapsulates parameters for constructing a SPARQL query to the HM Land Registry endpoint.
/// </summary>
public class SparqlQueryParameters
{
    /// <summary>Gets or sets the postcode filter (e.g., "SW1A 1AA").</summary>
    public string? Postcode { get; set; }

    /// <summary>Gets or sets the address substring to filter on (partial address matching).</summary>
    public string? AddressContains { get; set; }

    /// <summary>Gets or sets the start date for filtering transaction dates (inclusive).</summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>Gets or sets the end date for filtering transaction dates (inclusive).</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>Gets or sets the property type filter.</summary>
    public PropertyType? PropertyType { get; set; }

    /// <summary>Gets or sets the maximum number of results to return (LIMIT).</summary>
    public int? Limit { get; set; }

    /// <summary>Gets or sets the number of results to skip (OFFSET).</summary>
    public int? Offset { get; set; }
}
