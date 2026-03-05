import React from 'react';
import type { PropertyDto } from '../types/index';

interface ResultsPanelProps {
  results: PropertyDto[];
  totalCount: number;
  isLoading: boolean;
  error: string | null;
}

export const ResultsPanel: React.FC<ResultsPanelProps> = ({
  results,
  totalCount,
  isLoading,
  error,
}) => {
  if (error) {
    return (
      <div className="p-6 bg-red-50 border border-red-200 rounded-lg">
        <h3 className="text-red-900 font-semibold mb-2">Search Error</h3>
        <p className="text-red-700 text-sm">{error}</p>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="p-6">
        <div className="animate-pulse space-y-4">
          {[...Array(3)].map((_, i) => (
            <div key={i} className="bg-gray-200 rounded h-24"></div>
          ))}
        </div>
      </div>
    );
  }

  if (!results || results.length === 0) {
    return (
      <div className="p-6 text-center text-gray-500">
        <p>No results found. Use the search form to find properties.</p>
      </div>
    );
  }

  return (
    <div className="space-y-2 p-2">
      <div className="px-4 py-2 bg-gray-50 rounded">
        <p className="text-sm text-gray-600">
          Showing {results.length} of {totalCount} results
        </p>
      </div>
      {results.map((property, index) => (
        <PropertyResultItem key={index} property={property} />
      ))}
    </div>
  );
};

interface PropertyResultItemProps {
  property: PropertyDto;
}

const PropertyResultItem: React.FC<PropertyResultItemProps> = ({ property }) => {
  const formattedPrice = new Intl.NumberFormat('en-GB', {
    style: 'currency',
    currency: 'GBP',
  }).format(property.price);

  const transactionDate = new Date(property.transactionDate).toLocaleDateString(
    'en-GB',
    {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    }
  );

  return (
    <div className="bg-white border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow duration-200">
      <h4 className="font-semibold text-gray-900 mb-2">{property.address}</h4>
      <div className="space-y-1 text-sm text-gray-600">
        <p>
          <span className="font-medium">Postcode:</span> {property.postcode}
        </p>
        <p>
          <span className="font-medium">Price:</span>{' '}
          <span className="text-green-600 font-semibold">{formattedPrice}</span>
        </p>
        <p>
          <span className="font-medium">Type:</span> {property.propertyType}
        </p>
        <p>
          <span className="font-medium">Date:</span> {transactionDate}
        </p>
      </div>
    </div>
  );
};
