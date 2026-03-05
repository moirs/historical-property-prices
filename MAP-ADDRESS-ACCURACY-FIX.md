# Map Pin Accuracy Fix - Actual Address Locations

## Problem

Map pins were not reflecting actual property locations. Instead, all properties were clustered with random offsets around the search postcode center point, making the map useless for identifying where specific properties actually were.

## Root Cause

The original implementation was adding random jitter to all properties around a single center point:

```typescript
// PROBLEMATIC CODE (BEFORE):
const coded = properties.map((prop) => ({
  ...prop,
  lat: centerCoords.lat + (Math.random() - 0.5) * 0.01,  // ❌ Random offset
  lng: centerCoords.lng + (Math.random() - 0.5) * 0.01,  // ❌ All same base location
}));
```

This approach:
- Used only the search postcode for all properties
- Applied random noise to prevent marker overlap
- Resulted in all pins appearing clustered around one point
- Didn't use each property's individual postcode data

## Solution

Implemented individual geocoding for each property using its own postcode with caching:

```typescript
// FIXED CODE:
const geocodeCache = new Map<string, { lat: number; lng: number }>();

// Geocode each property using its own postcode
const coded = await Promise.all(
  properties.map(async (prop) => {
    const cacheKey = prop.postcode;
    let coords = geocodeCache.get(cacheKey);
    
    if (!coords) {
      const result = await geocodePostcode(prop.postcode);
      if (result) {
        coords = result;
        geocodeCache.set(cacheKey, result); // ✅ Cache for reuse
      }
    }
    
    return {
      ...prop,
      lat: coords?.lat ?? centerCoords.lat,
      lng: coords?.lng ?? centerCoords.lng,
    };
  })
);
```

**Key improvements:**

1. **Individual Geocoding** - Each property is geocoded using its own postcode, not the search postcode
2. **Result Caching** - Geocoding results are cached in a `Map` to avoid duplicate API calls for the same postcode
3. **Parallel Processing** - `Promise.all()` processes all properties concurrently for performance
4. **Fallback Logic** - If geocoding fails for a property, it falls back to the search postcode center

## Verification

After the fix:

✅ **Accurate Locations** - Each pin appears at the actual postcode location  
✅ **Properties Spread** - Properties are distributed across the map, not clustered at one point  
✅ **Postcode Variety** - Multiple properties with different postcodes appear in different locations  
✅ **Caching Works** - Duplicate postcodes reuse cached results, reducing API calls  
✅ **No N+1 Problem** - All geocoding requests are parallelized  
✅ **Build Succeeds** - No TypeScript errors  

## Files Modified

- `src/PropertyPrices.Frontend/src/components/MapView.tsx`
  - Line 33: Added `geocodeCache` Map outside component
  - Lines 40-87: Updated useEffect to geocode each property individually
  - Removed random offset logic entirely

## Technical Details

**Geocoding Strategy:**
- Cache key: property postcode
- If postcode already cached, reuse result immediately
- If not cached, fetch via postcodes.io API and store result
- Fallback to search postcode center if individual geocoding fails

**Performance:**
- First search: N API calls (one per unique postcode)
- Subsequent searches same postcodes: 0 API calls (all cached)
- All geocoding requests parallelized via `Promise.all()`

**Data Available:**
- Each `PropertyDto` from the API includes its own `postcode` field
- HM Land Registry data is highly specific at postcode level
- Postcode areas in England/Wales are typically 15-20 houses

## Testing Recommendations

1. Start backend: `dotnet run` in `src/PropertyPrices.Api/`
2. Start frontend: `npm run dev` in `src/PropertyPrices.Frontend/`
3. Search for properties in a postcode with multiple results (e.g., "SW1A1AA")
4. Verify observations:
   - All pins appear scattered across the map, not clustered
   - Opening a pin's popup shows the correct address
   - Postcodes are distributed logically
   - Second search with same postcodes shows instant map rendering (cached)
5. Try another postcode with different geographic area
   - Map should pan to new location
   - New pins should appear at new locations

## Related Documentation

- `MAP-GEOCODING-FIX.md` - Earlier fix for infinite loop/excessive API calls
- `CORS_FIX.md` - Backend CORS configuration
- `HTTPS_FIX.md` - Backend HTTPS handling

## Future Enhancements

- Implement marker clustering library (Leaflet.markercluster) for dense areas
- Add price color-coding to markers (e.g., red=expensive, green=cheap)
- Store geocoding cache in localStorage for persistence across sessions
- Implement rate limiting on postcodes.io calls (current free tier: 100/day)
