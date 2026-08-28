import { get } from 'svelte/store';
import { authStore, setAuthToken } from './store';
import { push } from 'svelte-spa-router';

const BASE_URL = import.meta.env.VITE_API_BASE_URL || 'https://barbershop-backend-production-f891.up.railway.app';

export async function apiRequest(endpoint, options = {}) {
  const { token } = get(authStore);
  
  const headers = {
    'Content-Type': 'application/json',
    ...(options.headers || {})
  };

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const url = endpoint.startsWith('http') ? endpoint : `${BASE_URL}${endpoint}`;

  try {
    const response = await fetch(url, {
      ...options,
      headers
    });

    if (response.status === 401) {
      setAuthToken(null);
      push('/login');
      throw new Error('Unauthorized');
    }
    let data = null;
    if (response.status !== 204) {
      const contentType = response.headers.get('content-type');
      if (contentType && contentType.includes('application/json')) {
        data = await response.json();
      } else {
        data = await response.text();
      }
    }

    if (!response.ok) {
      throw new Error(data?.message || data || 'API request failed');
    }

    return data;
  } catch (error) {
    console.error('API Error:', error);
    throw error;
  }
}
