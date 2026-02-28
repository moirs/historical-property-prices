namespace PropertyPrices.Core.Models;

/// <summary>
/// Enriched property sale information with parsed address components and validated data.
/// </summary>
public class PropertySaleInfo
{
    /// <summary>Gets the property address with parsed postcode components.</summary>
    public Address Address { get; set; } = new();

    /// <summary>Gets the transaction price in GBP (nullable if data was missing or invalid).</summary>
    public decimal? Price { get; set; }

    /// <summary>Gets the transaction date (nullable if data was missing or invalid).</summary>
    public DateOnly? TransactionDate { get; set; }

    /// <summary>Gets the date and time when this data was retrieved.</summary>
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets whether the price value passed validation (>0).</summary>
    public bool IsPriceValid => Price.HasValue && Price > 0;

    /// <summary>Gets whether the transaction date passed validation (not in future).</summary>
    public bool IsDateValid => TransactionDate.HasValue && TransactionDate <= DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Gets whether the address has a valid postcode with extracted area.</summary>
    public bool HasValidPostcode => !string.IsNullOrWhiteSpace(Address.PostcodeArea);

    /// <summary>
    /// Creates a PropertySaleInfo instance with required address.
    /// </summary>
    public PropertySaleInfo(Address? address = null)
    {
        Address = address ?? new Address();
    }

    /// <summary>
    /// Creates a PropertySaleInfo instance with all core properties.
    /// </summary>
    public PropertySaleInfo(Address address, decimal? price, DateOnly? transactionDate)
    {
        Address = address ?? new Address();
        Price = price;
        TransactionDate = transactionDate;
    }

    public override string ToString()
    {
        var parts = new[]
        {
            Address.ToString(),
            Price.HasValue ? $"£{Price:N0}" : "Price: N/A",
            TransactionDate.HasValue ? TransactionDate.Value.ToString("yyyy-MM-dd") : "Date: N/A"
        };
        return string.Join(" | ", parts);
    }
}
