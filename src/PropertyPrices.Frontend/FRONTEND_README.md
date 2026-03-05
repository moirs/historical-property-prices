# PropertyPrices Frontend

A modern React + TypeScript + Tailwind CSS web application for searching and visualizing UK historical property sales data from the HM Land Registry SPARQL endpoint.

## ✨ Features

- **Two-Column Layout**
  - Left Panel: Search form + results list
  - Right Panel: Interactive Leaflet map with property markers
  
- **Search Functionality**
  - Postcode-based search (with UK format validation)
  - Min/Max price filtering
  - Real-time form validation
  - Loading indicators
  
- **Interactive Map**
  - OpenStreetMap tiles
  - Property markers clustered around searched postcode
  - Click marker to view property details
  - Auto-centers on search area
  - Postcode geocoding via postcodes.io API
  
- **Results Display**
  - Property details: address, postcode, price, type, transaction date
  - Currency formatting (GBP)
  - Responsive result cards
  - Total count display

## 🛠️ Tech Stack

- **React 19** - UI framework
- **Vite 7** - Build tool (lightning-fast)
- **TypeScript** - Type safety
- **Tailwind CSS 4** - Utility-first styling
- **Leaflet** - Interactive mapping library
- **react-leaflet** - React bindings for Leaflet
- **Axios** - HTTP client
- **postcodes.io** - UK postcode geocoding API

## 📦 Project Structure

```
src/PropertyPrices.Frontend/
├── src/
│   ├── components/
│   │   ├── Layout.tsx          # Two-column layout wrapper
│   │   ├── SearchForm.tsx      # Search input form with validation
│   │   ├── ResultsPanel.tsx    # Property results list
│   │   └── MapView.tsx         # Leaflet map integration
│   ├── hooks/
│   │   └── useSearch.ts        # Custom hook for search state + API calls
│   ├── services/
│   │   └── apiClient.ts        # Typed HTTP client for API
│   ├── types/
│   │   └── index.ts            # TypeScript interfaces for all data types
│   ├── App.tsx                 # Main app component
│   ├── main.tsx                # React entry point
│   └── index.css               # Tailwind CSS directives
├── index.html                  # HTML entry point
├── package.json                # Dependencies
├── vite.config.ts              # Vite configuration
├── tailwind.config.js          # Tailwind CSS configuration
├── postcss.config.js           # PostCSS configuration
├── tsconfig.json               # TypeScript configuration
└── dist/                       # Production build (generated)
```

## 🚀 Getting Started

### Development

```bash
cd src/PropertyPrices.Frontend

# Install dependencies (already done)
npm install

# Start dev server (Vite HMR enabled)
npm run dev

# Open browser to http://localhost:5173
```

### Production Build

```bash
npm run build          # Produces optimized dist/ folder
npm run preview        # Preview production build locally
```

### Linting

```bash
npm run lint           # Run ESLint
```

## 🔗 API Integration

The frontend communicates with the backend API at `http://localhost:5000` (default).

**API Endpoint:** `POST /properties/search`

**Request:**
```typescript
{
  postcode: string;           // e.g., "SW1A1AA" (required)
  priceMin?: number;          // Min sale price filter
  priceMax?: number;          // Max sale price filter
  pageNumber?: number;        // Pagination (default: 1)
  pageSize?: number;          // Results per page (default: 50)
}
```

**Response:**
```typescript
{
  results: PropertyDto[];     // Array of property results
  totalCount: number;         // Total matching properties
  pageNumber: number;         // Current page
  pageSize: number;           // Results per page
}
```

## 📍 Geocoding

The app uses the free **postcodes.io API** to convert UK postcodes to latitude/longitude:
- Automatically converts search postcode to map center
- Adds slight random offsets to property markers to avoid overlap
- Falls back to London (51.5074, -0.1278) if geocoding fails

## 🎨 Styling

Built with **Tailwind CSS 4** for utility-first design:
- Responsive layout
- Dark mode support (easily togglable)
- Professional color scheme
- Custom components for reusability

## 🧪 Testing

To manually test the frontend:

1. **Ensure backend API is running:**
   ```bash
   cd src/PropertyPrices.Api
   dotnet run
   ```

2. **Start frontend dev server:**
   ```bash
   cd src/PropertyPrices.Frontend
   npm run dev
   ```

3. **Test with a valid UK postcode:**
   - Try "SW1A1AA" (Parliament), "M11AA" (Manchester), "B33AE" (Birmingham)
   - Enter min/max prices (optional)
   - Click "Search Properties"
   - View results in sidebar and markers on map

## 🐛 Troubleshooting

### Build Errors
- If you see Node.js version warnings, they can be ignored (app builds despite warnings)
- Run `npm install` if dependencies are missing

### Map Not Displaying
- Check browser console for errors
- Ensure Leaflet CSS is loaded: look for `leaflet/dist/leaflet.css` import
- Verify OpenStreetMap tiles are loading

### API Connection Fails
- Confirm backend API is running on `localhost:5000`
- Check CORS settings on backend
- Look at browser Network tab for 404/500 errors

### Search Returns No Results
- Verify postcode format (UK postcode required)
- Try a well-known area postcode: SW1A1AA, M11AA, E1 6AN
- Check if price range filters are too restrictive

## 📝 Next Steps (Optional Polish)

The following enhancement tasks are tracked but not yet implemented:
- Mobile-responsive adjustments (tablet/phone layout)
- Advanced error handling with user-friendly messages
- Loading skeleton screens
- Pagination controls for large result sets
- Search history/favorites
- Export results to CSV
- Property comparison view

## 📄 License

Part of the PropertyPrices project - UK Historical Property Data Explorer
