# Postcode Filtering Fix - Double Filtering Bug

## Problem Identified ❌

The API was filtering results **twice**, causing all data to be discarded:

1. **SPARQL Level (Correct)**: 
   - Query: `VALUES ?postcode {"SG4 9AH"^^xsd:string}`
   - Result: Returns all properties with postcode "SG4 9AH" ✅

2. **Client-Side Level (Broken)**:
   - Code: `.FilterByPostcodeArea(request.Postcode ?? "")`
   - Passes: `"SG4 9AH"` (full postcode)
   - Filter Logic: Compares against `x.Address.PostcodeArea` which is `"SG4"` (just the area)
   - Result: `"SG4 9AH" == "SG4"` → FALSE → All results filtered out ❌

## Example Failure

**Request:**
```json
{
  "postcode": "SG4 9AH"
}
```

**SPARQL Execution:**
```
Returns 15 properties with postcode "SG4 9AH" from HM Land Registry ✅
```

**Client-Side Filtering:**
```csharp
.FilterByPostcodeArea("SG4 9AH")  // Passes full postcode
.Where(x => x.Address.PostcodeArea == "SG4 9AH")  // But compares to area "SG4"
// Result: 0 matches → All 15 records filtered out ❌
```

**API Response:**
```json
{
  "results": [],
  "totalCount": 0
}
```

## Solution Implemented ✅

**Removed the redundant FilterByPostcodeArea call** because:

1. **SPARQL query already filtered** - The VALUES clause ensures only the requested postcode is returned
2. **No need for client-side filtering** - All data from the query is already correct
3. **Prevents false negatives** - Eliminates the mismatch between full postcode and postcode area

### Before (Broken)
```csharp
var filtered = transformedResults
    .FilterByPostcodeArea(request.Postcode ?? "")  // ❌ Filters everything out
    .Where(x => request.DateFrom == null || x.TransactionDate >= request.DateFrom)
    .Where(x => request.DateTo == null || x.TransactionDate <= request.DateTo)
    .Where(x => request.PriceMin == null || x.Price >= request.PriceMin)
    .Where(x => request.PriceMax == null || x.Price <= request.PriceMax)
    .ToList();
```

### After (Fixed)
```csharp
// Postcode filtering already done at SPARQL level, no need to filter again
var filtered = transformedResults
    .Where(x => request.DateFrom == null || x.TransactionDate >= request.DateFrom)
    .Where(x => request.DateTo == null || x.TransactionDate <= request.DateTo)
    .Where(x => request.PriceMin == null || x.Price >= request.PriceMin)
    .Where(x => request.PriceMax == null || x.Price <= request.PriceMax)
    .ToList();
```

## How FilterByPostcodeArea Works (For Reference)

The method is designed to filter by postcode **area** (not full postcode):

```csharp
public static IEnumerable<PropertySaleInfo> FilterByPostcodeArea(
    this IEnumerable<PropertySaleInfo> source,
    string postcodeArea)
{
    if (string.IsNullOrWhiteSpace(postcodeArea))
        return source;

    var normalized = postcodeArea.Trim().ToUpperInvariant();
    return source.Where(x => x.Address.PostcodeArea == normalized);
}
```

**Expected usage:**
```csharp
// Correct: Pass just the area
.FilterByPostcodeArea("SG4")  // Filters for area "SG4"

// Incorrect: Pass full postcode (what was happening)
.FilterByPostcodeArea("SG4 9AH")  // Tries to match area to "SG4 9AH" → No match
```

## Architecture Overview

```
User Request (postcode: "SG4 9AH")
    ↓
Build SPARQL Query with VALUES clause
    ↓
Execute SPARQL Query
    ├─ HM Land Registry endpoint receives: VALUES ?postcode {"SG4 9AH"^^xsd:string}
    ├─ Returns: All properties with postcode exactly "SG4 9AH"
    └─ Result: 15 property records ✅
    ↓
Transform Results to PropertySaleInfo objects
    └─ Result: 15 PropertySaleInfo objects with:
       - Address.Postcode = "SG4 9AH"
       - Address.PostcodeArea = "SG4"
    ↓
Apply Client-Side Filters (Date, Price ranges)
    ├─ No more postcode filtering (it's already done at SPARQL level)
    └─ Result: 12 property records matching date/price criteria ✅
    ↓
Apply Pagination
    └─ Result: Return page of results to user ✅
```

## Test Results

✅ All 90 tests passing
✅ Build: 0 errors, 0 warnings
✅ Commit: 52f4ccc

## Verification

To verify the fix works:

```bash
# Start the API
dotnet run --project src/PropertyPrices.Api

# Test with a postcode that has data (e.g., SG4 9AH)
curl -X POST http://localhost:5000/properties/search \
  -H "Content-Type: application/json" \
  -d '{
    "postcode": "SG4 9AH",
    "pageSize": 10
  }'
```

**Expected Response:**
```json
{
  "results": [
    {
      "address": "...",
      "postcode": "SG4 9AH",
      "price": 250000,
      "transactionDate": "2023-01-15"
    },
    ...
  ],
  "totalCount": 15,
  "pageNumber": 1,
  "pageSize": 10
}
```

Not empty results as before.

## Key Takeaways

1. **Filtering at the right level** - Database/SPARQL level filtering is more efficient than client-side
2. **Avoid double filtering** - Once filtered at source, no need to filter again
3. **Test with real data** - This bug was caught when actual SPARQL results returned data
4. **Understand API contracts** - FilterByPostcodeArea expects postcode **area**, not full postcode

## Files Changed

- `src/PropertyPrices.Api/Program.cs` - Removed redundant FilterByPostcodeArea call

## Related Issues

- **GitHub Issue #6**: Implement HM Land Registry integration
- **Commit 6c167e9**: Fixed SPARQL query builder with correct ontologies
- **Commit 2aebd5b**: Fixed postcode normalization to preserve space
- **Commit 52f4ccc**: Removed redundant postcode filtering
