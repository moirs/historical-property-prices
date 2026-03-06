namespace PropertyPrices.Api.Models;

/// <summary>
/// Request model for property search with optional filters.
/// </summary>
public class PropertySearchRequest
{
    /// <summary>Gets or sets the postcode to search for (e.g., "SW1A 1AA").</summary>
    public string? Postcode { get; set; }

    /// <summary>Gets or sets the start date for transaction date range (inclusive).</summary>
    public DateOnly? DateFrom { get; set; }

    /// <summary>Gets or sets the end date for transaction date range (inclusive).</summary>
    public DateOnly? DateTo { get; set; }

    /// <summary>Gets or sets the minimum price in GBP (inclusive).</summary>
    public decimal? PriceMin { get; set; }

    /// <summary>Gets or sets the maximum price in GBP (inclusive).</summary>
    public decimal? PriceMax { get; set; }

    /// <summary>Gets or sets the property type to filter by (e.g., "D" for Detached, "S" for Semi-Detached).</summary>
    public string? PropertyType { get; set; }

    /// <summary>Gets or sets the page number (1-based, default=1).</summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>Gets or sets the page size (1-1000, default=50).</summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Validates the request parameters.
    /// </summary>
    /// <param name="maxPageSize">Maximum allowed page size from configuration.</param>
    /// <returns>List of validation errors, empty if valid.</returns>
    public List<string> Validate(int maxPageSize = 100)
    {
        var errors = new List<string>();

        if (DateFrom.HasValue && DateTo.HasValue && DateFrom > DateTo)
            errors.Add("DateFrom must be less than or equal to DateTo.");

        if (PriceMin.HasValue && PriceMax.HasValue && PriceMin > PriceMax)
            errors.Add("PriceMin must be less than or equal to PriceMax.");

        if (PriceMin.HasValue && PriceMin < 0)
            errors.Add("PriceMin must be non-negative.");

        if (PriceMax.HasValue && PriceMax < 0)
            errors.Add("PriceMax must be non-negative.");

        if (!string.IsNullOrEmpty(PropertyType))
        {
            var validTypes = new[] { "D", "S", "T", "F", "O", "Detached", "Semi-Detached", "Terraced", "Flat", "Other" };
            if (!validTypes.Contains(PropertyType, StringComparer.OrdinalIgnoreCase))
                errors.Add("PropertyType must be one of: D, S, T, F, O, Detached, Semi-Detached, Terraced, Flat, or Other.");
        }

        if (PageNumber < 1)
            errors.Add("PageNumber must be >= 1.");

        if (PageSize < 1 || PageSize > maxPageSize)
            errors.Add($"PageSize must be between 1 and {maxPageSize}.");

        if (!string.IsNullOrEmpty(PropertyType))
        {
            var validTypes = new[] { "D", "S", "T", "F", "O", "Detached", "Semi-Detached", "Terraced", "Flat", "Other" };
            if (!validTypes.Contains(PropertyType, StringComparer.OrdinalIgnoreCase))
                errors.Add("PropertyType must be one of: D, S, T, F, O, Detached, Semi-Detached, Terraced, Flat, or Other.");
        }

        return errors;
    }
}

/// <summary>
/// Single property sale record for API response.
/// </summary>
public class PropertyDto
{
    /// <summary>Gets or sets the property address.</summary>
    public string? Address { get; set; }

    /// <summary>Gets or sets the postcode.</summary>
    public string? Postcode { get; set; }

    /// <summary>Gets or sets the postcode area (parsed from postcode).</summary>
    public string? PostcodeArea { get; set; }

    /// <summary>Gets or sets the sale price in GBP.</summary>
    public decimal? Price { get; set; }

    /// <summary>Gets or sets the transaction date.</summary>
    public DateOnly? TransactionDate { get; set; }

    /// <summary>Gets or sets the property type (e.g., "Detached", "Semi-Detached", "Terraced", "Flat", "Other").</summary>
    public string? PropertyType { get; set; }

    /// <summary>Gets or sets the estate type (e.g., "Freehold", "Leasehold").</summary>
    public string? Duration { get; set; }
}

/// <summary>
/// Response model for property search with pagination metadata.
/// </summary>
public class PropertySearchResponse
{
    /// <summary>Gets or sets the list of properties found.</summary>
    public List<PropertyDto> Results { get; set; } = new();

    /// <summary>Gets or sets the total number of properties matching the search criteria.</summary>
    public int TotalCount { get; set; }

    /// <summary>Gets or sets the current page number (1-based).</summary>
    public int PageNumber { get; set; }

    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }

    /// <summary>Gets the total number of pages.</summary>
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;

    /// <summary>Gets whether there are more results available.</summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>Gets whether there are previous results available.</summary>
    public bool HasPreviousPage => PageNumber > 1;
}
