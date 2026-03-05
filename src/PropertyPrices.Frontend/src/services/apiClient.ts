import axios, { type AxiosInstance } from 'axios';
import type {
  PropertySearchRequest,
  PropertySearchResponse,
  ApiError,
} from '../types/index';

class ApiClient {
  private client: AxiosInstance;
  private baseURL: string;

  constructor(baseURL: string = 'http://localhost:5000') {
    this.baseURL = baseURL;
    this.client = axios.create({
      baseURL: this.baseURL,
      headers: {
        'Content-Type': 'application/json',
      },
    });
  }

  async searchProperties(
    request: PropertySearchRequest
  ): Promise<PropertySearchResponse> {
    try {
      const response = await this.client.post<PropertySearchResponse>(
        '/properties/search',
        request
      );
      return response.data;
    } catch (error) {
      if (axios.isAxiosError(error)) {
        const apiError = error.response?.data as ApiError;
        throw new Error(
          apiError?.detail || error.message || 'Search request failed'
        );
      }
      throw error;
    }
  }

  async healthCheck(): Promise<{ status: string; timestamp: string }> {
    try {
      const response = await this.client.get('/health');
      return response.data;
    } catch (error) {
      throw new Error('Health check failed - API may be unavailable');
    }
  }
}

export const apiClient = new ApiClient();
