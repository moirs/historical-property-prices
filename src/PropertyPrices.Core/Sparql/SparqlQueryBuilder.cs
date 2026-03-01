using System.Text;
using PropertyPrices.Core.Sparql.Models;

namespace PropertyPrices.Core.Sparql;

/// <summary>
/// Fluent builder for constructing SPARQL queries against the HM Land Registry endpoint.
/// Supports composable filters for postcode, address, date range, price range, property type, and pagination.
/// </summary>
public class SparqlQueryBuilder
{
    private string? _postcode;
    private string? _addressContains;
    private DateOnly? _startDate;
    private DateOnly? _endDate;
    private PropertyType? _propertyType;
    private decimal? _minPrice;
    private decimal? _maxPrice;
    private int? _limit;
    private int? _offset;

    private const string SparqlPrefixes = @"
PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
PREFIX owl: <http://www.w3.org/2002/07/owl#>
PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
PREFIX sr: <http://data.ordnancesurvey.co.uk/ontology/spatialrelations/>
PREFIX ukhpi: <http://landregistry.data.gov.uk/def/ukhpi/>
PREFIX lrppi: <http://landregistry.data.gov.uk/def/ppi/>
PREFIX skos: <http://www.w3.org/2004/02/skos/core#>
PREFIX lrcommon: <http://landregistry.data.gov.uk/def/common/>
";

    /// <summary>
    /// Sets a postcode filter (e.g., "SW1A 1AA" or "SW1A1AA").
    /// </summary>
    /// <param name="postcode">The postcode to filter on. Will be normalized and validated.</param>
    /// <returns>This builder instance for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if postcode format is invalid.</exception>
    public SparqlQueryBuilder WithPostcode(string postcode)
    {
        ValidatePostcode(postcode);
        _postcode = NormalizePostcode(postcode);
        return this;
    }

    /// <summary>
    /// Sets an address substring filter for partial address matching.
    /// </summary>
    /// <param name="addressSubstring">The address substring to match. Case-insensitive.</param>
    /// <returns>This builder instance for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if address is null or whitespace.</exception>
    public SparqlQueryBuilder WithAddressContains(string addressSubstring)
    {
        if (string.IsNullOrWhiteSpace(addressSubstring))
            throw new ArgumentException("Address substring cannot be null or whitespace.", nameof(addressSubstring));
        _addressContains = addressSubstring.Trim();
        return this;
    }

    /// <summary>
    /// Sets a date range filter for transaction dates (inclusive on both ends).
    /// </summary>
    /// <param name="startDate">The start date (inclusive).</param>
    /// <param name="endDate">The end date (inclusive).</param>
    /// <returns>This builder instance for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if date range is invalid.</exception>
    public SparqlQueryBuilder WithDateRange(DateOnly startDate, DateOnly endDate)
    {
        ValidateDateRange(startDate, endDate);
        _startDate = startDate;
        _endDate = endDate;
        return this;
    }

    /// <summary>
    /// Sets a price range filter for property prices (inclusive on both ends).
    /// </summary>
    /// <param name="minPrice">The minimum price (inclusive). Pass null to omit minimum price filter.</param>
    /// <param name="maxPrice">The maximum price (inclusive). Pass null to omit maximum price filter.</param>
    /// <returns>This builder instance for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if price range is invalid.</exception>
    public SparqlQueryBuilder WithPriceRange(decimal? minPrice, decimal? maxPrice)
    {
        ValidatePriceRange(minPrice, maxPrice);
        _minPrice = minPrice;
        _maxPrice = maxPrice;
        return this;
    }

    /// <summary>
    /// Sets a property type filter.
    /// </summary>
    /// <param name="propertyType">The property type to filter on.</param>
    /// <returns>This builder instance for chaining.</returns>
    public SparqlQueryBuilder WithPropertyType(PropertyType propertyType)
    {
        _propertyType = propertyType;
        return this;
    }

    /// <summary>
    /// Sets pagination parameters for the query results.
    /// </summary>
    /// <param name="limit">Maximum number of results to return (must be greater than 0).</param>
    /// <param name="offset">Number of results to skip (must be 0 or greater). Defaults to 0 if not specified.</param>
    /// <returns>This builder instance for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if pagination parameters are invalid.</exception>
    public SparqlQueryBuilder WithPagination(int limit, int offset = 0)
    {
        ValidatePagination(limit, offset);
        _limit = limit;
        _offset = offset;
        return this;
    }

    /// <summary>
    /// Builds the SPARQL query string from the configured filters.
    /// </summary>
    /// <returns>A complete SPARQL query string ready for execution against the HM Land Registry endpoint.</returns>
    public string Build()
    {
        var query = new StringBuilder();
        
        // Add prefixes
        query.AppendLine(SparqlPrefixes);
        
        // Start SELECT clause - returns: paon, saon, street, town, county, postcode, amount, date, category, propertyType
        query.AppendLine("SELECT ?paon ?saon ?street ?town ?county ?postcode ?amount ?date ?category ?propertyType");
        query.AppendLine("WHERE {");
        
        // Build WHERE clauses dynamically based on filters
        BuildWhereClause(query);
        
        query.AppendLine("}");
        
        // Add ORDER BY clause
        query.AppendLine("ORDER BY ?amount");
        
        // Add pagination
        if (_limit.HasValue)
        {
            query.AppendLine($"LIMIT {_limit}");
        }
        if (_offset.HasValue && _offset > 0)
        {
            query.AppendLine($"OFFSET {_offset}");
        }
        
        return query.ToString();
    }

    private void BuildWhereClause(StringBuilder query)
    {
        // VALUES clause for postcode parameter (using SPARQL 1.1 VALUES syntax)
        if (!string.IsNullOrEmpty(_postcode))
        {
            // Normalize postcode: trim, uppercase, and ensure single space between parts
            // UK postcodes format: "AA9A 9AA" or similar (with space)
            var normalized = _postcode.Trim().ToUpper();
            // Split on whitespace and rejoin with single space to normalize multiple spaces
            var parts = normalized.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var normalizedPostcode = string.Join(" ", parts);
            query.AppendLine($"  VALUES ?postcode {{\"{normalizedPostcode}\"^^xsd:string}}");
        }
        
        // Core address and transaction pattern
        query.AppendLine("  ?addr lrcommon:postcode ?postcode .");
        query.AppendLine("  ?transx lrppi:propertyAddress ?addr ;");
        query.AppendLine("          lrppi:pricePaid ?amount ;");
        query.AppendLine("          lrppi:transactionDate ?date ;");
        query.AppendLine("          lrppi:transactionCategory/skos:prefLabel ?category .");
        
        // Optional property type
        query.AppendLine("  OPTIONAL {?transx lrppi:propertyType/skos:prefLabel ?propertyType}");
        
        // Optional address components
        query.AppendLine("  OPTIONAL {?addr lrcommon:county ?county}");
        query.AppendLine("  OPTIONAL {?addr lrcommon:paon ?paon}");
        query.AppendLine("  OPTIONAL {?addr lrcommon:saon ?saon}");
        query.AppendLine("  OPTIONAL {?addr lrcommon:street ?street}");
        query.AppendLine("  OPTIONAL {?addr lrcommon:town ?town}");
        
        // Add filters if specified (these are in addition to VALUES clause)
        if (!string.IsNullOrEmpty(_addressContains))
        {
            // Case-insensitive SPARQL FILTER with regex on address properties
            query.AppendLine($"  FILTER(regex(concat(str(?paon), \" \", str(?street), \" \", str(?town)), \"{_addressContains}\", \"i\"))");
        }
        
        if (_startDate.HasValue)
        {
            var startDateXsd = $"\"{_startDate:yyyy-MM-dd}\"^^xsd:date";
            query.AppendLine($"  FILTER(?date >= {startDateXsd})");
        }
        
        if (_endDate.HasValue)
        {
            var endDateXsd = $"\"{_endDate:yyyy-MM-dd}\"^^xsd:date";
            query.AppendLine($"  FILTER(?date <= {endDateXsd})");
        }
        
        if (_minPrice.HasValue)
        {
            query.AppendLine($"  FILTER(?amount >= {_minPrice}^^xsd:decimal)");
        }
        
        if (_maxPrice.HasValue)
        {
            query.AppendLine($"  FILTER(?amount <= {_maxPrice}^^xsd:decimal)");
        }
    }

    /// <summary>
    /// Returns the built SPARQL query string.
    /// </summary>
    public override string ToString() => Build();

    private static void ValidatePostcode(string postcode)
    {
        if (string.IsNullOrWhiteSpace(postcode))
            throw new ArgumentException("Postcode cannot be null or whitespace.", nameof(postcode));
        
        // UK postcode regex: 1-2 letters + 1-2 numbers + optional space + 1 number + 2 letters
        // Simplified validation: allow alphanumeric + spaces, 6-8 chars after normalization
        var normalized = postcode.ToUpper().Replace(" ", "");
        if (normalized.Length < 5 || normalized.Length > 8)
            throw new ArgumentException(
                "Postcode must be 5-8 characters when normalized (spaces removed). Format: e.g., 'SW1A 1AA', 'M1 1AA', or 'SW1A1AA'.",
                nameof(postcode));
    }

    private static string NormalizePostcode(string postcode)
    {
        return postcode.ToUpper().Trim();
    }

    private static void ValidateDateRange(DateOnly startDate, DateOnly endDate)
    {
        if (startDate > endDate)
            throw new ArgumentException(
                $"Start date ({startDate:yyyy-MM-dd}) cannot be after end date ({endDate:yyyy-MM-dd}).",
                nameof(startDate));
        
        // Sanity check: dates should be within reasonable bounds (e.g., after 1995 when Price Paid data started)
        var minDate = new DateOnly(1995, 1, 1);
        if (startDate < minDate)
            throw new ArgumentException(
                $"Start date must be 1995-01-01 or later (HM Land Registry Price Paid data starts from 1995).",
                nameof(startDate));
    }

    private static void ValidatePriceRange(decimal? minPrice, decimal? maxPrice)
    {
        if (minPrice.HasValue && minPrice < 0)
            throw new ArgumentException("Minimum price cannot be negative.", nameof(minPrice));
        
        if (maxPrice.HasValue && maxPrice < 0)
            throw new ArgumentException("Maximum price cannot be negative.", nameof(maxPrice));
        
        if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
            throw new ArgumentException(
                $"Minimum price ({minPrice}) cannot be greater than maximum price ({maxPrice}).",
                nameof(minPrice));
    }

    private static void ValidatePagination(int limit, int offset)
    {
        if (limit <= 0)
            throw new ArgumentException("Limit must be greater than 0.", nameof(limit));
        
        if (offset < 0)
            throw new ArgumentException("Offset must be 0 or greater.", nameof(offset));
    }

    private static string PropertyTypeToSparqlValue(PropertyType propertyType) => propertyType switch
    {
        PropertyType.Detached => "D",
        PropertyType.SemiDetached => "S",
        PropertyType.Terraced => "T",
        PropertyType.Flat => "F",
        PropertyType.Other => "O",
        _ => throw new ArgumentException($"Unknown property type: {propertyType}", nameof(propertyType))
    };
}
