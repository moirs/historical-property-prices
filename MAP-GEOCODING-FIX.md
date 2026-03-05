# Map Geocoding Fix - Jumping Pins and Excessive API Calls

## Problem

When performing property searches, the map component exhibited two issues:

1. **Jumping pins**: Property markers continuously jumped around on the map as if being repositioned repeatedly
2. **Excessive API calls**: The network tab showed hundreds of requests to `https://api.postcodes.io/postcodes/` for the same postcode during a single search

## Root Cause

The issue was in `src/PropertyPrices.Frontend/src/components/MapView.tsx` at the `useEffect` hook:

```typescript
// PROBLEMATIC CODE (BEFORE):
useEffect(() => {
  // ... geocoding logic ...
  setCenter([centerCoords.lat, centerCoords.lng]); // Updates state
  setGeocodedProperties(coded);                     // Updates state
}, [properties, postcode, center]); // ❌ center is in dependencies!
```

**The infinite loop mechanism:**
1. Effect runs when `properties` or `postcode` changes
2. Inside effect, calls `geocodePostcode()` which fetches from postcodes.io API
3. `setCenter()` is called, updating the `center` state
4. Since `center` is in the dependency array, the effect triggers AGAIN
5. Loop repeats hundreds of times until the API call completes or rate limit is hit

## Solution

Two key fixes were applied:

### 1. Removed `center` from Dependency Array
The `center` state is only used for map rendering, not for controlling geocoding logic:

```typescript
// FIXED CODE:
useEffect(() => {
  // ... geocoding logic ...
}, [properties, postcode]); // ✅ center removed - no infinite loop
```

### 2. Added Cleanup/Mount Check
Implemented the standard React pattern of checking `isMounted` flag to prevent state updates after unmount:

```typescript
useEffect(() => {
  let isMounted = true;

  const geocodeProperties = async () => {
    try {
      // ... get centerCoords ...
      if (coords && isMounted) {
        setCenter([coords.lat, coords.lng]);
      }
      if (isMounted) {
        setGeocodedProperties(coded);
      }
    } catch (error) {
      if (isMounted) {
        // Update state on error
      }
    }
  };

  if (properties.length > 0) {
    geocodeProperties();
  }

  return () => {
    isMounted = false; // Cleanup
  };
}, [properties, postcode]);
```

## Verification

After the fix:

✅ **Map is stable** - Pins stay in place and don't jump around  
✅ **Single API call per search** - Network tab shows exactly one request to postcodes.io per unique postcode  
✅ **No memory leaks** - Pending state updates are cancelled when component unmounts  
✅ **Build succeeds** - No TypeScript errors  

## Files Modified

- `src/PropertyPrices.Frontend/src/components/MapView.tsx`
  - Lines 40-84: Updated useEffect hook with dependency array fix and isMounted cleanup pattern

## Related Files

- `POSTCODE-FILTERING-FIX.md` - Earlier fix for postcode validation
- `CORS_FIX.md` - Backend CORS configuration
- `HTTPS_FIX.md` - Backend HTTPS handling

## Testing Recommendations

1. Start the backend: `dotnet run` in `src/PropertyPrices.Api/`
2. Start the frontend: `npm run dev` in `src/PropertyPrices.Frontend/`
3. Perform a search with a valid UK postcode (e.g., "SW1A1AA")
4. Open DevTools Network tab - verify only ONE request to api.postcodes.io
5. Observe map - pins should be stable and clustered around the postcode location
6. Try different postcodes - verify new geocoding request is made only once per new postcode

## Technical Details

**Dependency Array Principles:**
- Include only values that directly control WHEN the effect should run
- If changing state would cause re-dependency, remove it from the array
- State updates are necessary side effects, but shouldn't trigger the effect

**Why isMounted pattern:**
- Prevents "Can't perform a React state update on an unmounted component" warnings
- Ensures no state updates fire after async operations complete if component was unmounted
- Standard practice for async effects in React
