namespace PropertyPrices.Core.Models;

/// <summary>
/// Represents a physical address with parsed UK postcode components.
/// </summary>
public class Address
{
    /// <summary>Gets the street name or house name/number.</summary>
    public string? StreetName { get; set; }

    /// <summary>Gets the locality, town, or village name.</summary>
    public string? Locality { get; set; }

    /// <summary>Gets the postcode area extracted from full postcode (e.g., "SW1A" from "SW1A 1AA").</summary>
    public string? PostcodeArea { get; set; }

    /// <summary>Gets the full postcode.</summary>
    public string? Postcode { get; set; }

    /// <summary>
    /// Creates an Address instance with all properties initialized.
    /// </summary>
    public Address(string? streetName = null, string? locality = null, string? postcode = null, string? postcodeArea = null)
    {
        StreetName = streetName;
        Locality = locality;
        Postcode = postcode;
        PostcodeArea = postcodeArea;
    }

    public override string ToString()
    {
        var parts = new[] { StreetName, Locality, Postcode }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();
        return string.Join(", ", parts);
    }
}
