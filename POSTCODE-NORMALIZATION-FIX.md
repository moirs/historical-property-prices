# Fixed Postcode Normalization

## Problem
The previous code was removing ALL spaces from postcodes:
```csharp
var normalizedPostcode = request.Postcode.Replace(" ", "");  // WRONG: "PL6 8RU" → "PL68RU"
```

This caused queries to return zero results because HM Land Registry expects postcodes in the proper format with the space.

## Solution
Updated normalization to preserve the space between postcode parts:
```csharp
var normalized = request.Postcode.Trim().ToUpper();
var parts = normalized.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
var normalizedPostcode = string.Join(" ", parts);
```

## Examples of Correct Normalization

| Input | Output | Reason |
|-------|--------|--------|
| "PL6 8RU" | "PL6 8RU" | Already correct format |
| "pl6 8ru" | "PL6 8RU" | Converted to uppercase, space preserved |
| "PL6  8RU" | "PL6 8RU" | Multiple spaces normalized to single space |
| " PL6 8RU " | "PL6 8RU" | Leading/trailing whitespace trimmed |
| "M1  1AA" | "M1 1AA" | Multiple spaces normalized to single space |

## Generated SPARQL Query

For input: `WithPostcode("pl6 8ru")`

Before fix (ZERO RESULTS):
```sparql
VALUES ?postcode {"PL68RU"^^xsd:string}
```

After fix (RETURNS DATA):
```sparql
VALUES ?postcode {"PL6 8RU"^^xsd:string}
```

## HM Land Registry Postcode Format

UK postcodes follow the format: `outward_code inward_code`
- Outward code: 1-2 letters + 1-2 digits (+ optional letter) = "PL6", "M1", "SW1A"
- Inward code: 1 digit + 2 letters = "8RU", "1AA", "1AA"
- **They must have exactly one space between them**

## Testing

All 90 tests now pass with the corrected postcode normalization:
✅ Postcode with space is preserved: `"SW1A 1AA"` → `"SW1A 1AA"`
✅ Lowercase converted to uppercase: `"m1 1aa"` → `"M1 1AA"`
✅ Multiple spaces normalized: `"M1  1AA"` → `"M1 1AA"`
✅ Whitespace trimmed: `" PL6 8RU "` → `"PL6 8RU"`

## API Usage

```bash
curl -X POST http://localhost:5000/properties/search \
  -H "Content-Type: application/json" \
  -d '{
    "postcode": "PL6 8RU",
    "pageSize": 50
  }'
```

The postcode will be normalized to "PL6 8RU" (uppercase, single space preserved) and the SPARQL query will return actual results from the HM Land Registry endpoint.

## Files Changed

1. `src/PropertyPrices.Core/Sparql/SparqlQueryBuilder.cs` - Updated BuildWhereClause
2. `src/PropertyPrices.Api/Program.cs` - Updated BuildSparqlQuery
3. `tests/PropertyPrices.Tests/Sparql/SparqlQueryBuilderTests.cs` - Updated test expectations
