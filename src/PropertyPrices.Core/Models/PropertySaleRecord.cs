namespace PropertyPrices.Core.Models;

/// <summary>
/// Represents a single property sale transaction from HM Land Registry data.
/// Parsed from SPARQL results.
/// </summary>
public class PropertySaleRecord
{
    /// <summary>Gets or sets the RDF URI of the property.</summary>
    public string? PropertyUri { get; set; }

    /// <summary>Gets or sets the property address.</summary>
    public string? Address { get; set; }

    /// <summary>Gets or sets the postcode (normalized, no spaces).</summary>
    public string? Postcode { get; set; }

    /// <summary>Gets or sets the sale price in GBP.</summary>
    public decimal? Price { get; set; }

    /// <summary>Gets or sets the transaction date.</summary>
    public DateOnly? TransactionDate { get; set; }

    /// <summary>Gets or sets the property type (D/S/T/F/O).</summary>
    public string? PropertyType { get; set; }

    /// <summary>Gets or sets the estate type (Freehold, Leasehold, etc).</summary>
    public string? Duration { get; set; }

    /// <summary>Gets or sets the query execution timestamp.</summary>
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
}
