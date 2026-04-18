import axios from 'axios';
import { navigationService } from '../shared/services/navigationService';

const baseURL = import.meta.env.VITE_API_BASE_URL
  || (import.meta.env.DEV ? 'http://localhost:5295/api' : '/api');

const api = axios.create({
  baseURL,
  timeout: 50000,
  headers: { 'Content-Type': 'application/json' },
  withCredentials: true
});

// Add XSRF token for non-GET requests
api.interceptors.request.use(config => {
  if (config.method?.toLowerCase() !== 'get') {
    const token = document.cookie
      .split(';')
      .find(c => c.trim().startsWith('XSRF-TOKEN='))
      ?.split('=')[1];

    if (token) {
      config.headers['RequestVerificationToken'] = token;
    }
  }
  return config;
});

// Handle auth errors and redirects
api.interceptors.response.use(
  response => response,
  error => {
    const isAuthEndpoint = ['/users/login', '/users/register', '/users/me'].some(
      endpoint => error.config?.url?.endsWith(endpoint)
    );

    if (error.response?.status === 401 && !isAuthEndpoint) {
      navigationService.navigateTo('/login');
    }

    return Promise.reject(error);
  }
);

export default api;