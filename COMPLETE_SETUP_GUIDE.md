# Complete Setup and Verification Guide

## ✅ Full Stack Ready

Both frontend and backend are now configured and ready to work together without errors.

---

## 🚀 COMPLETE SETUP STEPS

### Step 1: Build Frontend (Optional - Already Built)

The frontend is already built and ready. If you need to rebuild:

```bash
cd src/PropertyPrices.Frontend
npm run build
```

✅ Build output exists in `dist/` folder

### Step 2: Start Backend API

**Terminal 1:**
```bash
cd src/PropertyPrices.Api
dotnet run
```

**Backend will:**
- Listen on `http://localhost:5000` (HTTP in development)
- Have CORS enabled for localhost:5173
- Accept requests from frontend
- Serve health check endpoint

**Verify startup:**
- Look for: `Application started. Press Ctrl+C to shut down.`

### Step 3: Start Frontend Dev Server

**Terminal 2:**
```bash
cd src/PropertyPrices.Frontend
npm run dev
```

**Frontend will:**
- Start on `http://localhost:5173`
- Hot reload enabled for development
- Connect to backend via API client

**Verify startup:**
- Look for: `Local: http://localhost:5173/`
- Open browser to this URL

---

## 🧪 TEST A SEARCH

1. **In browser** (http://localhost:5173):
   - Postcode field: `SW1A1AA` (Parliament, London)
   - Price fields: Leave empty (optional)
   - Click: "Search Properties"

2. **Expected Results:**
   - Left sidebar shows property list with:
     - Address
     - Postcode
     - Price (£)
     - Property Type
     - Transaction Date
   - Right panel shows map with property markers
   - Click markers to see property details in popup

3. **Browser Console (F12):**
   - ✅ Should have NO errors
   - ✅ Should NOT see CORS errors
   - ✅ Should NOT see SSL/Certificate errors
   - Network tab should show: `POST /properties/search` with status `200`

---

## ✅ All Issues Fixed

### Issue 1: HTTPS Certificate Error ✅ FIXED
**What was wrong:** Backend forced HTTPS with self-signed certificate
**Fix Applied:**
- Backend: Disabled HTTPS redirect in development (`src/PropertyPrices.Api/Program.cs`)
- Frontend: Changed API URL to HTTP (`src/PropertyPrices.Frontend/src/services/apiClient.ts`)
- Status: ✅ Fixed - HTTP in development, HTTPS still enforced in production

**Documentation:** See `HTTPS_FIX.md`

### Issue 2: CORS Error ✅ FIXED
**What was wrong:** Backend didn't allow cross-origin requests from frontend
**Fix Applied:**
- Backend: Added CORS service with `AllowLocalhost` policy
- Backend: Enabled CORS middleware in pipeline
- Allowed origins: `localhost:3000, 5173, 5174, 5175`
- Status: ✅ Fixed - Frontend can now communicate with backend

**Documentation:** See `CORS_FIX.md`

---

## 📁 Project Structure

```
historical-property-prices/
├── src/
│   ├── PropertyPrices.Api/           (Backend - ASP.NET Core)
│   │   └── Program.cs                (✅ HTTPS & CORS configured)
│   ├── PropertyPrices.Core/          (Domain logic)
│   ├── PropertyPrices.Infrastructure/(Data access)
│   └── PropertyPrices.Frontend/      (React frontend)
│       ├── src/
│       │   ├── components/           (React components)
│       │   ├── services/             (API client)
│       │   ├── hooks/                (Custom hooks)
│       │   ├── types/                (TypeScript types)
│       │   └── App.tsx               (Main app)
│       ├── dist/                     (✅ Build output)
│       ├── package.json              (Dependencies)
│       └── vite.config.ts            (Build config)
├── HTTPS_FIX.md                      (SSL certificate fix)
├── CORS_FIX.md                       (CORS configuration fix)
└── FRONTEND_IMPLEMENTATION_SUMMARY.md(Frontend overview)
```

---

## 🔧 Technology Stack

### Frontend
- React 19.2.0
- TypeScript 5.9.3
- Vite 7.3.1 (build tool)
- Tailwind CSS 4.2.1
- Leaflet 1.9.4 (maps)
- Axios 1.13.6 (HTTP client)

### Backend
- ASP.NET Core
- Serilog (logging)
- Polly (resilience policies)
- SPARQL client

### External APIs
- HM Land Registry SPARQL Endpoint (property data)
- postcodes.io API (geocoding)
- OpenStreetMap (tiles)

---

## 🔍 Troubleshooting

### Backend won't start
```bash
# Check if port 5000 is in use
netstat -ano | findstr :5000
# Kill the process if needed
taskkill /PID <PID> /F
# Try again
dotnet run
```

### Frontend won't start
```bash
# Clear npm cache
npm cache clean --force
# Reinstall dependencies
npm install
# Try dev server
npm run dev
```

### CORS error in browser console
- ✅ Backend CORS is configured at line 34-47 in `Program.cs`
- ✅ Middleware enabled at line 79 in `Program.cs`
- If error persists: Restart backend API

### SSL/Certificate error in backend logs
- ✅ HTTPS redirect disabled in development at line 82-87 in `Program.cs`
- ✅ Frontend uses HTTP at line 12 in `apiClient.ts`
- If error persists: Restart backend API

### Search returns no results
- Verify postcode format: Must be valid UK postcode (e.g., SW1A1AA)
- Try popular postcodes: M11AA (Manchester), B33AE (Birmingham)
- Check browser Network tab for API response
- Check backend logs for SPARQL query errors

### Map not displaying markers
- Ensure Leaflet CSS is loaded (browser Network tab)
- Check browser console for JavaScript errors
- Verify postcode geocoding worked (postcodes.io should have been called)
- Try a different postcode

---

## 📝 Development Workflow

### Making Changes

**Frontend changes:**
```bash
cd src/PropertyPrices.Frontend
npm run dev
# Changes auto-reload thanks to Vite HMR
```

**Backend changes:**
- Stop backend (Ctrl+C)
- Make changes to `Program.cs` or other files
- Restart: `dotnet run`

### Building for Production

**Frontend production build:**
```bash
cd src/PropertyPrices.Frontend
npm run build
# Output in dist/ folder (ready to deploy)
```

**Backend production:**
```bash
cd src/PropertyPrices.Api
dotnet publish -c Release
```

---

## ✨ Features Implemented

✅ Postcode-based property search
✅ Min/Max price filtering
✅ Form validation (postcode format, price range)
✅ Results displayed in scrollable list
✅ Interactive Leaflet map with markers
✅ Click markers for property details
✅ Auto-center map on postcode
✅ Loading indicators
✅ Error handling
✅ Currency formatting (GBP)
✅ Date formatting
✅ Responsive two-column layout
✅ TypeScript type safety
✅ Production-ready build

---

## 🎉 Ready to Use

Everything is configured, built, and ready for testing. Simply follow the setup steps above and start searching for properties!

**Next Steps:**
1. Start backend (Terminal 1)
2. Start frontend (Terminal 2)
3. Open http://localhost:5173
4. Search for a property
5. Enjoy! 🎉
