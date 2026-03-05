import React, { useEffect, useState } from 'react';
import {
  MapContainer,
  TileLayer,
  Marker,
  Popup,
  useMap,
} from 'react-leaflet';
import L from 'leaflet';
import type { PropertyDto } from '../types/index';
import 'leaflet/dist/leaflet.css';

// Fix Leaflet marker icons issue in Vite
delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl:
    'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon-2x.png',
  iconUrl:
    'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon.png',
  shadowUrl:
    'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png',
});

interface MapViewProps {
  properties: PropertyDto[];
  postcode: string;
}

interface GeocodedProperty extends PropertyDto {
  lat: number;
  lng: number;
}

const geocodeCache = new Map<string, { lat: number; lng: number }>();

export const MapView: React.FC<MapViewProps> = ({ properties, postcode }) => {
  const [geocodedProperties, setGeocodedProperties] = useState<
    GeocodedProperty[]
  >([]);
  const [center, setCenter] = useState<[number, number]>([51.5074, -0.1278]); // Default: London

  useEffect(() => {
    let isMounted = true;

    const geocodeProperties = async () => {
      try {
        let centerCoords = { lat: 51.5074, lng: -0.1278 }; // Default: London
        
        // Get center point from search postcode
        if (postcode) {
          const cached = geocodeCache.get(postcode);
          if (cached) {
            centerCoords = cached;
          } else {
            const coords = await geocodePostcode(postcode);
            if (coords) {
              centerCoords = coords;
              geocodeCache.set(postcode, coords);
            }
          }
          if (isMounted) {
            setCenter([centerCoords.lat, centerCoords.lng]);
          }
        }

        // Geocode each property using its own postcode
        if (isMounted) {
          const coded = await Promise.all(
            properties.map(async (prop) => {
              const cacheKey = prop.postcode;
              let coords = geocodeCache.get(cacheKey);
              
              if (!coords) {
                const result = await geocodePostcode(prop.postcode);
                if (result) {
                  coords = result;
                  geocodeCache.set(cacheKey, result);
                }
              }
              
              return {
                ...prop,
                lat: coords?.lat ?? centerCoords.lat,
                lng: coords?.lng ?? centerCoords.lng,
              };
            })
          );
          setGeocodedProperties(coded);
        }
      } catch (error) {
        console.error('Geocoding error:', error);
        // Use default coordinates on error
        if (isMounted) {
          const coded = properties.map((prop) => ({
            ...prop,
            lat: 51.5074,
            lng: -0.1278,
          }));
          setGeocodedProperties(coded);
        }
      }
    };

    if (properties.length > 0) {
      geocodeProperties();
    }

    return () => {
      isMounted = false;
    };
  }, [properties, postcode]);

  return (
    <MapContainer
      center={center}
      zoom={14}
      style={{ height: '100%', width: '100%' }}
    >
      <TileLayer
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
      />
      <MapViewUpdater center={center} />
      {geocodedProperties.map((property, index) => (
        <Marker key={index} position={[property.lat, property.lng]}>
          <Popup>
            <div className="text-sm">
              <p className="font-semibold">{property.address}</p>
              <p className="text-gray-600">{property.postcode}</p>
              <p className="font-semibold text-green-600">
                £{property.price.toLocaleString()}
              </p>
              <p className="text-gray-600">{property.propertyType}</p>
              <p className="text-gray-500 text-xs">
                {new Date(property.transactionDate).toLocaleDateString()}
              </p>
            </div>
          </Popup>
        </Marker>
      ))}
    </MapContainer>
  );
};

interface MapViewUpdaterProps {
  center: [number, number];
}

const MapViewUpdater: React.FC<MapViewUpdaterProps> = ({ center }) => {
  const map = useMap();

  useEffect(() => {
    map.setView(center, 14);
  }, [center, map]);

  return null;
};

// Simple geocoding function using Open Cage Data (free tier available)
// For production, consider using a proper geocoding service
async function geocodePostcode(postcode: string): Promise<{
  lat: number;
  lng: number;
} | null> {
  try {
    // Using approximate UK postcode to coordinate mapping
    // In production, use a proper geocoding API
    const encodedPostcode = encodeURIComponent(postcode);
    const response = await fetch(
      `https://api.postcodes.io/postcodes/${encodedPostcode}`
    );

    if (response.ok) {
      const data = await response.json();
      return {
        lat: data.result.latitude,
        lng: data.result.longitude,
      };
    }
  } catch (error) {
    console.error('Geocoding request failed:', error);
  }
  return null;
}
