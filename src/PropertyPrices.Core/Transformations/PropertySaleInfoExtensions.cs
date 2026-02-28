using PropertyPrices.Core.Models;

namespace PropertyPrices.Core.Transformations;

/// <summary>
/// Extension methods for filtering and grouping PropertySaleInfo collections.
/// </summary>
public static class PropertySaleInfoExtensions
{
    /// <summary>
    /// Filters PropertySaleInfo records by postcode area.
    /// </summary>
    public static IEnumerable<PropertySaleInfo> FilterByPostcodeArea(
        this IEnumerable<PropertySaleInfo> source,
        string postcodeArea)
    {
        if (string.IsNullOrWhiteSpace(postcodeArea))
            return source;

        var normalized = postcodeArea.Trim().ToUpperInvariant();
        return source.Where(x => x.Address.PostcodeArea == normalized);
    }

    /// <summary>
    /// Filters PropertySaleInfo records by date range (inclusive).
    /// </summary>
    public static IEnumerable<PropertySaleInfo> FilterByDateRange(
        this IEnumerable<PropertySaleInfo> source,
        DateOnly? startDate,
        DateOnly? endDate)
    {
        return source.Where(x =>
        {
            if (!x.TransactionDate.HasValue)
                return false;

            if (startDate.HasValue && x.TransactionDate < startDate)
                return false;

            if (endDate.HasValue && x.TransactionDate > endDate)
                return false;

            return true;
        });
    }

    /// <summary>
    /// Filters PropertySaleInfo records by price range (inclusive).
    /// </summary>
    public static IEnumerable<PropertySaleInfo> FilterByPriceRange(
        this IEnumerable<PropertySaleInfo> source,
        decimal? minPrice,
        decimal? maxPrice)
    {
        return source.Where(x =>
        {
            if (!x.Price.HasValue)
                return false;

            if (minPrice.HasValue && x.Price < minPrice)
                return false;

            if (maxPrice.HasValue && x.Price > maxPrice)
                return false;

            return true;
        });
    }

    /// <summary>
    /// Filters PropertySaleInfo records with valid prices (> 0).
    /// </summary>
    public static IEnumerable<PropertySaleInfo> WhereValidPrice(this IEnumerable<PropertySaleInfo> source)
    {
        return source.Where(x => x.IsPriceValid);
    }

    /// <summary>
    /// Filters PropertySaleInfo records with valid transaction dates (not in future).
    /// </summary>
    public static IEnumerable<PropertySaleInfo> WhereValidDate(this IEnumerable<PropertySaleInfo> source)
    {
        return source.Where(x => x.IsDateValid);
    }

    /// <summary>
    /// Filters PropertySaleInfo records with valid postcodes (with extracted area).
    /// </summary>
    public static IEnumerable<PropertySaleInfo> WhereValidPostcode(this IEnumerable<PropertySaleInfo> source)
    {
        return source.Where(x => x.HasValidPostcode);
    }

    /// <summary>
    /// Groups PropertySaleInfo records by postcode area.
    /// </summary>
    public static IEnumerable<IGrouping<string, PropertySaleInfo>> GroupByPostcodeArea(
        this IEnumerable<PropertySaleInfo> source)
    {
        return source
            .Where(x => !string.IsNullOrWhiteSpace(x.Address.PostcodeArea))
            .GroupBy(x => x.Address.PostcodeArea!);
    }

    /// <summary>
    /// Orders PropertySaleInfo records by transaction date, newest first.
    /// </summary>
    public static IEnumerable<PropertySaleInfo> OrderByDateNewestFirst(this IEnumerable<PropertySaleInfo> source)
    {
        return source.OrderByDescending(x => x.TransactionDate);
    }

    /// <summary>
    /// Orders PropertySaleInfo records by transaction date, oldest first.
    /// </summary>
    public static IEnumerable<PropertySaleInfo> OrderByDateOldestFirst(this IEnumerable<PropertySaleInfo> source)
    {
        return source.OrderBy(x => x.TransactionDate);
    }

    /// <summary>
    /// Orders PropertySaleInfo records by price, highest first.
    /// </summary>
    public static IEnumerable<PropertySaleInfo> OrderByPriceDescending(this IEnumerable<PropertySaleInfo> source)
    {
        return source.OrderByDescending(x => x.Price);
    }

    /// <summary>
    /// Orders PropertySaleInfo records by price, lowest first.
    /// </summary>
    public static IEnumerable<PropertySaleInfo> OrderByPriceAscending(this IEnumerable<PropertySaleInfo> source)
    {
        return source.OrderBy(x => x.Price);
    }

    /// <summary>
    /// Calculates statistics for a group of PropertySaleInfo records.
    /// </summary>
    public static (int Count, decimal? AvgPrice, decimal? MinPrice, decimal? MaxPrice) GetStatistics(
        this IEnumerable<PropertySaleInfo> source)
    {
        var records = source.ToList();
        var validPrices = records
            .Where(x => x.IsPriceValid)
            .Select(x => x.Price!.Value)
            .ToList();

        return (
            Count: records.Count,
            AvgPrice: validPrices.Count > 0 ? validPrices.Average() : null,
            MinPrice: validPrices.Count > 0 ? validPrices.Min() : null,
            MaxPrice: validPrices.Count > 0 ? validPrices.Max() : null
        );
    }
}
