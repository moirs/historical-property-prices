namespace PropertyPrices.Core.Sparql.Models;

/// <summary>
/// Represents metadata about a SPARQL query result set.
/// </summary>
public class SparqlQueryResult
{
    /// <summary>Gets or sets the SPARQL query that was executed.</summary>
    public required string Query { get; set; }

    /// <summary>Gets or sets the number of results returned.</summary>
    public int ResultCount { get; set; }

    /// <summary>Gets or sets whether the result set was truncated due to LIMIT.</summary>
    public bool IsTruncated { get; set; }

    /// <summary>Gets or sets the timestamp when the query was executed.</summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}
