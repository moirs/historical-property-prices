namespace PropertyPrices.Infrastructure.Data.Models;

/// <summary>
/// Represents a single variable binding in a SPARQL query result.
/// Maps to a variable value pair from the SPARQL JSON results format.
/// </summary>
public class SparqlBinding
{
    /// <summary>Gets or sets the variable name (e.g., "property", "price", "date").</summary>
    public required string Variable { get; set; }

    /// <summary>Gets or sets the value type (typically "uri" or "literal").</summary>
    public string? Type { get; set; }

    /// <summary>Gets or sets the actual value of the binding.</summary>
    public string? Value { get; set; }

    /// <summary>Gets or sets the data type for literals (e.g., "http://www.w3.org/2001/XMLSchema#date").</summary>
    public string? DataType { get; set; }
}

/// <summary>
/// Represents the complete result set from a SPARQL query.
/// Follows the W3C SPARQL JSON Results Format.
/// </summary>
public class SparqlResult
{
    /// <summary>Gets or sets the SPARQL query that produced this result.</summary>
    public string? Query { get; set; }

    /// <summary>Gets or sets the list of variable names in the SELECT clause.</summary>
    public List<string> Variables { get; set; } = new();

    /// <summary>Gets or sets the list of result bindings (one per row returned).</summary>
    public List<Dictionary<string, SparqlBinding>> Bindings { get; set; } = new();

    /// <summary>Gets the number of results returned.</summary>
    public int ResultCount => Bindings.Count;

    /// <summary>Gets whether this result set appears to be incomplete (e.g., due to LIMIT).</summary>
    public bool IsTruncated { get; set; }
}

