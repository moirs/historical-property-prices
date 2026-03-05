import { useState, useCallback } from 'react';
import { apiClient } from '../services/apiClient';
import type {
  PropertySearchRequest,
  PropertySearchResponse,
  SearchFormState,
} from '../types/index';

export const useSearch = () => {
  const [results, setResults] = useState<PropertySearchResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const executeSearch = useCallback(
    async (formState: SearchFormState) => {
      setLoading(true);
      setError(null);

      try {
        const request: PropertySearchRequest = {
          postcode: formState.postcode || undefined,
          priceMin: formState.priceMin ? parseInt(formState.priceMin) : undefined,
          priceMax: formState.priceMax ? parseInt(formState.priceMax) : undefined,
          pageNumber: 1,
          pageSize: 50,
        };

        const response = await apiClient.searchProperties(request);
        setResults(response);
        setError(null);
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Search failed';
        setError(errorMessage);
        setResults(null);
      } finally {
        setLoading(false);
      }
    },
    []
  );

  const clearResults = useCallback(() => {
    setResults(null);
    setError(null);
  }, []);

  return {
    results,
    loading,
    error,
    executeSearch,
    clearResults,
  };
};
