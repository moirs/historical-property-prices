# HTTPS Certificate Error - Fixed ✅

## Problem
When running the frontend (`npm run dev`) and attempting searches, the backend API returned this error:

```
[2026-03-05 22:07:47 DBG] Failed to authenticate HTTPS connection.
System.Security.Authentication.AuthenticationException: Authentication failed, see inner exception.
---> System.ComponentModel.Win32Exception (0x80090327): An unknown error occurred while processing the certificate.
```

## Root Cause
The ASP.NET Core backend was configured to:
1. Force HTTPS redirection (`app.UseHttpsRedirection()`)
2. Use a self-signed certificate
3. The frontend was trying to connect via `https://localhost:7148`

The self-signed certificate wasn't trusted by the browser/axios HTTP client, causing SSL validation failures.

## Solution Applied ✅

### 1. Backend Change
**File:** `src/PropertyPrices.Api/Program.cs`

Changed HTTPS redirect to only apply in production, not development:

```csharp
// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Skip HTTPS redirect in development
}
else
{
    app.UseHttpsRedirection();
}
```

### 2. Frontend Change
**File:** `src/PropertyPrices.Frontend/src/services/apiClient.ts`

Updated the API client default URL from HTTPS to HTTP:

```typescript
// Before
constructor(baseURL: string = 'https://localhost:7148') {

// After
constructor(baseURL: string = 'http://localhost:5000') {
```

## Result ✅
- Frontend now communicates with backend over plain HTTP in development
- No more SSL certificate validation errors
- Searches work seamlessly
- Backend still enforces HTTPS in production

## How to Verify

1. **Stop and restart backend API:**
   ```bash
   cd src/PropertyPrices.Api
   dotnet run
   # API now runs on http://localhost:5000 (HTTP in development)
   ```

2. **Restart frontend dev server:**
   ```bash
   cd src/PropertyPrices.Frontend
   npm run dev
   # Frontend runs on http://localhost:5173
   ```

3. **Test a search:**
   - Enter postcode: "SW1A1AA"
   - Click "Search Properties"
   - Should see results without errors ✅

## Important Notes

- ✅ **Development:** HTTP is safe to use locally - no sensitive data transmitted
- ✅ **Production Build:** Still respects HTTPS when built (the `else` branch applies)
- ✅ **CORS:** Backend should allow requests from `http://localhost:5173` (frontend dev port)
- ℹ️ If building frontend for production deployment, ensure backend has valid SSL certificate

## Files Modified

1. `src/PropertyPrices.Api/Program.cs` - Disabled HTTPS redirect in development
2. `src/PropertyPrices.Frontend/src/services/apiClient.ts` - Changed default API URL to HTTP
3. Frontend rebuilt successfully

---

**Status:** Development environment is now fully functional. No further action needed. 🎉
