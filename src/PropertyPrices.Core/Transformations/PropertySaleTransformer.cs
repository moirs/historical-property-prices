using System.Text.RegularExpressions;
using PropertyPrices.Core.Models;

namespace PropertyPrices.Core.Transformations;

/// <summary>
/// Transforms raw SPARQL PropertySaleRecord objects into enriched PropertySaleInfo domain models
/// with parsed address components, validation, and normalization.
/// </summary>
public static class PropertySaleTransformer
{
    private static readonly Regex PostcodeAreaRegex = new(@"^[A-Z]{1,2}\d[A-Z\d]?", RegexOptions.Compiled);

    /// <summary>
    /// Transforms a PropertySaleRecord to a PropertySaleInfo with validated and normalized data.
    /// </summary>
    public static PropertySaleInfo Transform(PropertySaleRecord record)
    {
        if (record == null)
            throw new ArgumentNullException(nameof(record));

        var address = CreateAddress(record);
        var price = ValidatePrice(record.Price);
        var transactionDate = ValidateTransactionDate(record.TransactionDate);

        return new PropertySaleInfo(address, price, transactionDate);
    }

    /// <summary>
    /// Transforms multiple PropertySaleRecord objects to PropertySaleInfo in bulk.
    /// </summary>
    public static List<PropertySaleInfo> TransformBulk(IEnumerable<PropertySaleRecord> records)
    {
        return records
            .Where(r => r != null)
            .Select(Transform)
            .ToList();
    }

    /// <summary>
    /// Parses a UK postcode and extracts the area code (e.g., "SW1A" from "SW1A 1AA").
    /// Returns null if the postcode is invalid or empty.
    /// </summary>
    public static string? ParsePostcodeArea(string? postcode)
    {
        if (string.IsNullOrWhiteSpace(postcode))
            return null;

        var normalized = postcode.Trim().ToUpperInvariant();
        var match = PostcodeAreaRegex.Match(normalized);
        return match.Success ? match.Value : null;
    }

    /// <summary>
    /// Validates a price value. Returns null if price is null, zero, or negative.
    /// </summary>
    public static decimal? ValidatePrice(decimal? price)
    {
        return price.HasValue && price > 0 ? price : null;
    }

    /// <summary>
    /// Validates a transaction date. Returns null if date is null or in the future.
    /// </summary>
    public static DateOnly? ValidateTransactionDate(DateOnly? date)
    {
        if (!date.HasValue)
            return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return date <= today ? date : null;
    }

    /// <summary>
    /// Normalizes a postcode string: trim, uppercase, and validate format.
    /// </summary>
    public static string? NormalizePostcode(string? postcode)
    {
        if (string.IsNullOrWhiteSpace(postcode))
            return null;

        var normalized = postcode.Trim().ToUpperInvariant();
        return PostcodeAreaRegex.IsMatch(normalized) ? normalized : null;
    }

    private static Address CreateAddress(PropertySaleRecord record)
    {
        var normalizedPostcode = NormalizePostcode(record.Postcode);
        var postcodeArea = normalizedPostcode != null ? ParsePostcodeArea(normalizedPostcode) : null;

        return new Address(
            streetName: string.IsNullOrWhiteSpace(record.Address) ? null : record.Address.Trim(),
            locality: null, // SPARQL results don't include locality; could be enhanced with reverse geocoding
            postcode: normalizedPostcode,
            postcodeArea: postcodeArea
        );
    }
}
