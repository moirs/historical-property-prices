# SPARQL Query Examples

This document provides practical examples of SPARQL queries that can be constructed using the `SparqlQueryBuilder` module to query the HM Land Registry Price Paid Data.

## Overview

The SPARQL Query Builder provides a fluent API for dynamically constructing queries against the HM Land Registry endpoint. Each example below shows both the builder usage (C# code) and the resulting SPARQL query that would be executed.

---

## Example 1: Simple Postcode Query (Most Selective)

**Goal**: Find all property transactions for a specific postcode.

```csharp
var query = new SparqlQueryBuilder()
    .WithPostcode("SW1A 1AA")
    .Build();
```

**Resulting SPARQL**:
```sparql
PREFIX ppd: <http://data.ordnancesurvey.co.uk/ontology/property/Adapted>
PREFIX prop: <http://www.w3.org/1999/02/22-rdf-syntax-ns#type>
PREFIX geo: <http://www.w3.org/2003/01/geo/wgs84_pos#>
PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>

SELECT ?property ?address ?postcode ?price ?date ?type
WHERE {
  ?property prop: ppd:PricePaidRecord .
  ?property ppd:address ?address .
  ?property ppd:pricePaid ?price .
  ?property ppd:transactionDate ?date .
  ?property ppd:propertyType ?type .
  ?property ppd:postcode "SW1A1AA" .
}
```

**Performance**: ⚡ Optimal - Postcode is the most selective filter, suitable for individual address lookups.

---

## Example 2: Postcode with Date Range Filter

**Goal**: Find property sales in a specific postcode during a particular year.

```csharp
var query = new SparqlQueryBuilder()
    .WithPostcode("M1 1AA")
    .WithDateRange(
        startDate: new DateOnly(2022, 1, 1),
        endDate: new DateOnly(2022, 12, 31)
    )
    .Build();
```

**Resulting SPARQL**:
```sparql
PREFIX ppd: <http://data.ordnancesurvey.co.uk/ontology/property/Adapted>
PREFIX prop: <http://www.w3.org/1999/02/22-rdf-syntax-ns#type>
PREFIX geo: <http://www.w3.org/2003/01/geo/wgs84_pos#>
PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>

SELECT ?property ?address ?postcode ?price ?date ?type
WHERE {
  ?property prop: ppd:PricePaidRecord .
  ?property ppd:address ?address .
  ?property ppd:pricePaid ?price .
  ?property ppd:transactionDate ?date .
  ?property ppd:propertyType ?type .
  ?property ppd:postcode "M11AA" .
  FILTER(?date >= "2022-01-01"^^xsd:date)
  FILTER(?date <= "2022-12-31"^^xsd:date)
}
```

**Use Case**: Historical analysis of property values in a specific area during a time period.

---

## Example 3: Address Substring with Property Type Filter

**Goal**: Find flats and apartments on a specific street.

```csharp
var query = new SparqlQueryBuilder()
    .WithAddressContains("Baker Street")
    .WithPropertyType(PropertyType.Flat)
    .WithPagination(limit: 50)
    .Build();
```

**Resulting SPARQL**:
```sparql
PREFIX ppd: <http://data.ordnancesurvey.co.uk/ontology/property/Adapted>
PREFIX prop: <http://www.w3.org/1999/02/22-rdf-syntax-ns#type>
PREFIX geo: <http://www.w3.org/2003/01/geo/wgs84_pos#>
PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>

SELECT ?property ?address ?postcode ?price ?date ?type
WHERE {
  ?property prop: ppd:PricePaidRecord .
  ?property ppd:address ?address .
  ?property ppd:pricePaid ?price .
  ?property ppd:transactionDate ?date .
  ?property ppd:propertyType ?type .
  FILTER(regex(str(?address), "Baker Street", "i"))
  FILTER(?type = "F")
}
LIMIT 50
```

**Notes**: 
- Address filter is case-insensitive (the "i" flag in regex)
- Property types: D=Detached, S=Semi-detached, T=Terraced, F=Flat, O=Other

---

## Example 4: Paginated Results (Large Result Sets)

**Goal**: Retrieve paginated results for broad queries with offset/limit.

```csharp
var pageSize = 100;
var pageNumber = 1;
var offset = (pageNumber - 1) * pageSize;

var query = new SparqlQueryBuilder()
    .WithAddressContains("Road")
    .WithPropertyType(PropertyType.Detached)
    .WithPagination(limit: pageSize, offset: offset)
    .Build();
```

**Resulting SPARQL** (for page 1):
```sparql
PREFIX ppd: <http://data.ordnancesurvey.co.uk/ontology/property/Adapted>
PREFIX prop: <http://www.w3.org/1999/02/22-rdf-syntax-ns#type>
PREFIX geo: <http://www.w3.org/2003/01/geo/wgs84_pos#>
PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>

SELECT ?property ?address ?postcode ?price ?date ?type
WHERE {
  ?property prop: ppd:PricePaidRecord .
  ?property ppd:address ?address .
  ?property ppd:pricePaid ?price .
  ?property ppd:transactionDate ?date .
  ?property ppd:propertyType ?type .
  FILTER(regex(str(?address), "Road", "i"))
  FILTER(?type = "D")
}
LIMIT 100
OFFSET 0
```

**Performance Considerations**: 
- LIMIT/OFFSET enables result set pagination
- Large offsets may require more query time; consider caching frequently accessed ranges

---

## Example 5: Complex Multi-Filter Query

**Goal**: Analyze terraced houses in a region during a specific economic period.

```csharp
var query = new SparqlQueryBuilder()
    .WithAddressContains("Street")  // Targets mostly street addresses, not flats
    .WithPropertyType(PropertyType.Terraced)
    .WithDateRange(
        startDate: new DateOnly(2019, 1, 1),
        endDate: new DateOnly(2023, 12, 31)
    )
    .WithPagination(limit: 200)
    .Build();
```

**Resulting SPARQL**:
```sparql
PREFIX ppd: <http://data.ordnancesurvey.co.uk/ontology/property/Adapted>
PREFIX prop: <http://www.w3.org/1999/02/22-rdf-syntax-ns#type>
PREFIX geo: <http://www.w3.org/2003/01/geo/wgs84_pos#>
PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>

SELECT ?property ?address ?postcode ?price ?date ?type
WHERE {
  ?property prop: ppd:PricePaidRecord .
  ?property ppd:address ?address .
  ?property ppd:pricePaid ?price .
  ?property ppd:transactionDate ?date .
  ?property ppd:propertyType ?type .
  FILTER(regex(str(?address), "Street", "i"))
  FILTER(?type = "T")
  FILTER(?date >= "2019-01-01"^^xsd:date)
  FILTER(?date <= "2023-12-31"^^xsd:date)
}
LIMIT 200
```

**Use Case**: Market trend analysis comparing pre-pandemic (2019) to post-pandemic (2023) property values.

---

## Query Performance Tips

1. **Prioritize Postcode Filters**: Postcode is the most selective filter. Always include it when possible.
2. **Combine Filters Wisely**: Multiple filters reduce result sets and improve query performance.
3. **Date Ranges**: Specify narrower date ranges when possible to reduce SPARQL endpoint load.
4. **Address Matching**: Substring matching on addresses is less efficient; combine with other filters.
5. **Pagination**: For broad queries expecting many results, use LIMIT/OFFSET to page through results.

---

## Validation & Error Handling

The `SparqlQueryBuilder` validates all inputs:

### Postcode Validation
- Format: 5-8 characters after normalization (spaces removed)
- Examples: "SW1A 1AA", "M1 1AA", "B1 1AA"
- Invalid: "INVALID" (too long), "AB" (too short)

### Date Range Validation
- Start date must not be after end date
- Both dates must be on or after 1995-01-01 (when HM Land Registry data collection started)

### Property Type
- Valid values: Detached, SemiDetached, Terraced, Flat, Other
- Maps to SPARQL single-letter codes: D, S, T, F, O

### Pagination
- Limit must be > 0
- Offset must be >= 0

---

## Example: Error Handling in C#

```csharp
try
{
    var query = new SparqlQueryBuilder()
        .WithPostcode("TOOLONG")  // Will throw ArgumentException
        .Build();
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Invalid input: {ex.Message}");
}
```

---

## Next Steps

- The `SparqlQueryBuilder` returns a SPARQL query string suitable for execution by the Data Access Layer
- The Data Access Layer handles HTTP requests to the HM Land Registry SPARQL endpoint
- Results are transformed from RDF/JSON format into domain objects for the application layer
