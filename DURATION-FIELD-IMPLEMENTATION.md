# Duration Field Implementation - Complete

## Overview
Successfully threaded the estate type duration field through the entire backend and frontend data pipeline. Duration data (Freehold/Leasehold) is now available in API responses.

## Changes Made

### 1. Backend Data Models (Core)
**PropertySaleRecord.cs** - Added:
```csharp
public string? Duration { get; set; }  // Gets or sets the estate type (Freehold, Leasehold, etc)
```

**PropertySaleInfo.cs** - Added:
```csharp
public string? Duration { get; set; }  // Gets or sets the estate type (e.g., "Freehold", "Leasehold")
```
Updated constructor to accept duration parameter.

### 2. Transformation Layer (Core)
**PropertySaleTransformer.cs** - Updated Transform() method:
```csharp
// Old: return new PropertySaleInfo(address, price, transactionDate, record.PropertyType);
// New: return new PropertySaleInfo(address, price, transactionDate, record.PropertyType, record.Duration);
```

### 3. SPARQL Data Access (Infrastructure)
**SparqlDataAccessClient.cs** - ParseSparqlJsonResults() updated:
- Added extraction: `Duration = GetBindingValue(binding, "duration"),`
- Now captures duration from SPARQL JSON result bindings

### 4. API Response DTO (API)
**PropertySearchModels.cs** - PropertyDto enhanced:
```csharp
public string? Duration { get; set; }  // Gets or sets the estate type (e.g., "Freehold", "Leasehold")
```

### 5. Endpoint Mapping (API)
**Program.cs** - Search endpoint updated:
- Added `Duration = x.Duration` to the PropertyDto mapping
- Duration now flows through to API response

### 6. Frontend Types (Frontend)
**types/index.ts** - PropertyDto interface updated:
```typescript
duration?: string;  // Added optional duration field
```

## Data Flow
```
SPARQL Query: ?duration (via lrppi:estateType/skos:prefLabel)
    ↓
PropertySaleRecord.Duration (raw extraction)
    ↓
PropertySaleInfo.Duration (domain model)
    ↓
PropertyDto.duration (API response DTO)
    ↓
JSON API Response: {"duration": "Freehold", ...}
```

## Testing

### Build Status
- ✅ Backend build: succeeded (0 warnings, 0 errors)
- ✅ Frontend build: succeeded (3.88s)

### Manual Testing Steps
1. Start backend: `dotnet run` in `src/PropertyPrices.Api/`
2. Call endpoint:
   ```bash
   curl -X POST http://localhost:5000/properties/search \
     -H "Content-Type: application/json" \
     -d '{"postcode": "SW1A1AA", "pageSize": 5}'
   ```
3. Verify response includes: `"duration": "Freehold"` or `"duration": "Leasehold"`

## Expected Values
- **"Freehold"** - Property owner has full ownership rights
- **"Leasehold"** - Property is rented for a specified term
- **null** - Duration data not available (rare in HM Land Registry data)

## Files Modified Summary
| File | Changes |
|------|---------|
| PropertySaleRecord.cs | +Duration property |
| PropertySaleInfo.cs | +Duration property, updated constructor |
| PropertySaleTransformer.cs | Updated Transform() to pass duration |
| SparqlDataAccessClient.cs | Extract duration from SPARQL JSON |
| PropertySearchModels.cs | +Duration to PropertyDto |
| Program.cs | Map Duration in endpoint |
| types/index.ts | +duration to PropertyDto interface |

## Build & Test
All code changes compile successfully:
```
Backend: dotnet build ✓
Frontend: npm run build ✓
```

Next: Start services and test API response includes duration field.
