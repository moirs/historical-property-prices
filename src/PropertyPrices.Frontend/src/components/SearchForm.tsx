import React, { useState } from 'react';
import type { SearchFormState } from '../types/index';

interface SearchFormProps {
  onSearch: (formState: SearchFormState) => void;
  isLoading: boolean;
}

export const SearchForm: React.FC<SearchFormProps> = ({ onSearch, isLoading }) => {
  const [formState, setFormState] = useState<SearchFormState>({
    postcode: '',
    priceMin: '',
    priceMax: '',
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formState.postcode.trim()) {
      newErrors.postcode = 'Postcode is required';
    } else if (!/^[A-Z0-9]{2,4}\s?[A-Z0-9]{2,3}$/i.test(formState.postcode.trim())) {
      newErrors.postcode = 'Please enter a valid UK postcode (e.g., SW1A1AA)';
    }

    if (formState.priceMin && isNaN(parseInt(formState.priceMin))) {
      newErrors.priceMin = 'Min price must be a number';
    }

    if (formState.priceMax && isNaN(parseInt(formState.priceMax))) {
      newErrors.priceMax = 'Max price must be a number';
    }

    if (formState.priceMin && formState.priceMax) {
      if (parseInt(formState.priceMin) > parseInt(formState.priceMax)) {
        newErrors.priceMin = 'Min price cannot be greater than max price';
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validateForm()) {
      onSearch(formState);
    }
  };

  const handleInputChange = (
    field: keyof SearchFormState,
    value: string
  ) => {
    setFormState((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => ({ ...prev, [field]: '' }));
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Postcode *
        </label>
        <input
          type="text"
          value={formState.postcode}
          onChange={(e) => handleInputChange('postcode', e.target.value)}
          placeholder="e.g., SW1A1AA"
          className={`w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none ${
            errors.postcode ? 'border-red-500' : 'border-gray-300'
          }`}
          disabled={isLoading}
        />
        {errors.postcode && (
          <p className="text-red-500 text-sm mt-1">{errors.postcode}</p>
        )}
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Min Price (£)
          </label>
          <input
            type="number"
            value={formState.priceMin}
            onChange={(e) => handleInputChange('priceMin', e.target.value)}
            placeholder="0"
            className={`w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none ${
              errors.priceMin ? 'border-red-500' : 'border-gray-300'
            }`}
            disabled={isLoading}
          />
          {errors.priceMin && (
            <p className="text-red-500 text-sm mt-1">{errors.priceMin}</p>
          )}
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Max Price (£)
          </label>
          <input
            type="number"
            value={formState.priceMax}
            onChange={(e) => handleInputChange('priceMax', e.target.value)}
            placeholder="Unlimited"
            className={`w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none ${
              errors.priceMax ? 'border-red-500' : 'border-gray-300'
            }`}
            disabled={isLoading}
          />
          {errors.priceMax && (
            <p className="text-red-500 text-sm mt-1">{errors.priceMax}</p>
          )}
        </div>
      </div>

      <button
        type="submit"
        disabled={isLoading}
        className="w-full bg-blue-600 hover:bg-blue-700 disabled:bg-gray-400 text-white font-medium py-2 px-4 rounded-lg transition-colors duration-200"
      >
        {isLoading ? 'Searching...' : 'Search Properties'}
      </button>
    </form>
  );
};
