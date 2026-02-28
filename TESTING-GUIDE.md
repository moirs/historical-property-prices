# PropertyPrices API - Testing Guide

## Quick Start

### 1. Start the API
```powershell
cd "C:\Users\scot\source\repos\copilot-playground\real-estate-insights\historical-property-prices"
dotnet run --project src/PropertyPrices.Api
```

The API will start at: **https://localhost:7123**

### 2. Import Postman Collection
1. Open Postman
2. Click **Import** → **Upload Files**
3. Select: `PropertyPrices-Postman-Collection.json`
4. All 15 test requests will be available

### 3. Test the API
- Start with **"Health Check"** to verify API is running
- Try **"Search - By Postcode (SW1A 1AA)"** for real data
- Experiment with different filters

---

## Postman Collection Includes

### Basic Tests
- ✅ Health Check - Verify API is running
- ✅ Minimal Search - No filters
- ✅ Search by Postcode - Multiple UK postcodes

### Filter Tests
- ✅ Date Range - Properties sold in specific period
- ✅ Price Range - Properties between price limits
- ✅ Property Type - Filter by house type
- ✅ Complex - All filters combined

### Pagination Tests
- ✅ Page 2 Results - Navigate through pages
- ✅ Large Page Size - Get 100+ results

### Error Tests
- ✅ Invalid Postcode - Error handling
- ✅ Invalid Date Range - Validation

---

## API Endpoints

### Health Check
```
GET https://localhost:7123/health
```
**Response (200 OK):**
```json
{
  "status": "healthy",
  "timestamp": "2026-02-28T17:40:00.000Z"
}
```

### Search Properties
```
POST https://localhost:7123/properties/search
```

**Request Body Example:**
```json
{
  "postcode": "SW1A 1AA",
  "dateFrom": "2023-01-01",
  "dateTo": "2023-12-31",
  "priceMin": 100000,
  "priceMax": 500000,
  "propertyType": "Detached",
  "pageNumber": 1,
  "pageSize": 10
}
```

**Response (200 OK):**
```json
{
  "results": [
    {
      "address": "1 THE COMMONS",
      "postcode": "SW1A 1AA",
      "postcodeArea": "SW1A",
      "price": 450000,
      "transactionDate": "2023-06-15"
    }
  ],
  "totalCount": 42,
  "pageNumber": 1,
  "pageSize": 10
}
```

---

## Request Parameters

| Parameter | Type | Required | Example | Notes |
|-----------|------|----------|---------|-------|
| `postcode` | string | No | "SW1A 1AA" | 5-8 chars, spaces optional |
| `dateFrom` | string | No | "2023-01-01" | Format: YYYY-MM-DD |
| `dateTo` | string | No | "2023-12-31" | Format: YYYY-MM-DD |
| `priceMin` | number | No | 100000 | Minimum price in £ |
| `priceMax` | number | No | 500000 | Maximum price in £ |
| `propertyType` | string | No | "Detached" | Options: Detached, SemiDetached, Terraced, Flat, Other |
| `pageNumber` | number | Yes | 1 | Must be ≥ 1 |
| `pageSize` | number | Yes | 10 | Must be 1-1000 |

---

## Test Postcodes (Have Real Data)

| Postcode | Area | Best For |
|----------|------|----------|
| SW1A 1AA | Westminster, London | Parliament area - guaranteed data |
| M1 1AA | Manchester | Large city test |
| B33 8TH | Birmingham | Another major city |
| EC1A 1BB | London (City) | Financial district |
| W1A 1AA | London (West End) | Central London |

---

## Response Status Codes

| Code | Meaning | Example |
|------|---------|---------|
| 200 | Success | Properties returned |
| 400 | Bad Request | Invalid postcode format, invalid date range |
| 500 | Server Error | SPARQL endpoint unavailable |
| 504 | Gateway Timeout | Query took too long |

---

## Troubleshooting

### API Won't Start
```
Error: "Address already in use"
→ Another process using port 7123
→ Kill it: Get-Process -Name "PropertyPrices.Api" | Stop-Process
```

### SSL Certificate Error
```
"The remote certificate is invalid"
→ Normal for localhost HTTPS
→ In Postman: Disable "SSL Certificate Verification" (Settings → General)
```

### No Results Returned
```
→ Postcode may not have historical data
→ Try SW1A 1AA or M1 1AA instead
→ Check internet connectivity (queries real endpoint)
```

### Query Timeout
```
"Gateway Timeout (504)"
→ SPARQL endpoint is slow or busy
→ Try a more specific query (add postcode filter)
→ Wait and retry
```

---

## Performance Notes

- ⚡ Postcode searches: 2-5 seconds
- ⏱️ Date range searches: 5-15 seconds
- 🐢 Very broad searches: 15-30 seconds
- 🌍 Requires internet connectivity (queries real HM Land Registry)

---

## Next Steps

1. ✅ Start API: `dotnet run --project src/PropertyPrices.Api`
2. ✅ Import Postman collection
3. ✅ Run Health Check
4. ✅ Try postcode search
5. ✅ Experiment with filters

**Happy testing!** 🚀
