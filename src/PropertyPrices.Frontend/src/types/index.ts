// API Request/Response Types
export interface PropertySearchRequest {
  postcode?: string;
  priceMin?: number;
  priceMax?: number;
  propertyType?: string;
  dateFrom?: string;
  dateTo?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface PropertyDto {
  address: string;
  postcode: string;
  postcodeArea: string;
  price: number;
  transactionDate: string;
  propertyType: string;
}

export interface PropertySearchResponse {
  results: PropertyDto[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

// Search Form State
export interface SearchFormState {
  postcode: string;
  priceMin: string;
  priceMax: string;
}

// Map Marker Data
export interface PropertyMarker extends PropertyDto {
  latitude: number;
  longitude: number;
}

// API Error Response
export interface ApiError {
  title: string;
  detail: string;
  status: number;
}

// Geolocation Result
export interface GeocodeResult {
  latitude: number;
  longitude: number;
  postcode?: string;
}
