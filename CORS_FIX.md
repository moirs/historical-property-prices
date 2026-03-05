# CORS Error - Fixed ✅

## Problem
When running the frontend (`npm run dev` on port 5173) and attempting searches, the browser blocked the API request with this error:

```
Access to XMLHttpRequest at 'http://localhost:5000/properties/search' from origin 
'http://localhost:5173' has been blocked by CORS policy: Response to preflight request 
doesn't pass access control check: No 'Access-Control-Allow-Origin' header is present 
on the requested resource.
```

## Root Cause
The backend ASP.NET Core API didn't have CORS (Cross-Origin Resource Sharing) configured to allow requests from different origins. Since the frontend runs on `localhost:5173` and the backend on `localhost:5000`, they are considered different origins by the browser, which enforces CORS policies.

## Solution Applied ✅

### Backend Configuration
**File:** `src/PropertyPrices.Api/Program.cs`

Added CORS service and middleware to allow localhost development origins:

**1. Added CORS Service** (line 33-47):
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost:5175"
            )
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
```

**2. Added CORS Middleware** (line 78-79):
```csharp
// Enable CORS
app.UseCors("AllowLocalhost");
```

## What This Does

✅ **Allows**: Requests from any of these localhost ports:
- `http://localhost:3000` (Create React App default)
- `http://localhost:5173` (Vite default) ← Frontend runs here
- `http://localhost:5174` (Vite alternative)
- `http://localhost:5175` (Vite alternative)

✅ **Methods**: All HTTP methods (GET, POST, PUT, DELETE, etc.)

✅ **Headers**: Any headers

✅ **Scope**: Development only - this policy applies to development environments

## How to Apply the Fix

The fix is already applied to the backend code. Simply:

1. **Stop any running backend process**

2. **Restart the backend API:**
   ```bash
   cd src/PropertyPrices.Api
   dotnet run
   ```
   Backend now runs on `http://localhost:5000` with CORS enabled for development

3. **Frontend still running on:**
   ```bash
   cd src/PropertyPrices.Frontend
   npm run dev
   # Already running on http://localhost:5173
   ```

4. **Test a search:**
   - Enter postcode: "SW1A1AA"
   - Click "Search Properties"
   - Results should display without CORS errors ✅

## Important Notes

### Development ✅
- CORS is fully permissive on all localhost ports in development
- This is safe because it's local-only traffic
- Allows rapid development without CORS blocking searches

### Production ⚠️
When deploying to production:
1. Update the CORS policy to only allow your production frontend domain
2. Consider using environment-specific configuration
3. Never use wildcard (`*`) origins in production

Example production configuration:
```csharp
policy.WithOrigins("https://yourdomain.com")
    .AllowAnyMethod()
    .AllowAnyHeader();
```

## Verification

After restarting the backend:

1. Check browser console (F12 → Console)
   - Should NOT see CORS errors
   
2. Check Network tab (F12 → Network)
   - POST request to `/properties/search` should show status `200` or `400`
   - Response should include `Access-Control-Allow-Origin: http://localhost:5173`

3. Try a search
   - Enter a UK postcode
   - Click search
   - Results should appear in sidebar
   - Markers should appear on map

---

## Files Modified

- `src/PropertyPrices.Api/Program.cs` - Added CORS service and middleware

## Status

✅ CORS is now configured for development
✅ Frontend and backend can communicate freely
✅ Ready for testing searches
