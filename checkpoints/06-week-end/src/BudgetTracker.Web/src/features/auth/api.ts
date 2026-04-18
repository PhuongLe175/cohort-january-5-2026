import { apiClient } from '../../api';
import type { AuthResponse, LoginCredentials, RegisterData, UserInfo } from './types';

// In-memory session state
let currentUser: UserInfo | null = null;

export const authApi = {
  async login(credentials: LoginCredentials): Promise<AuthResponse> {
    try {
      const response = await apiClient.post('/users/login?useCookies=true', credentials);
      if (response.status !== 200) {
        return { success: false, message: response.data?.message || 'Failed to login' };
      }

      currentUser = response.data;
      return { success: true };
    } catch (error: any) {
      return { success: false, message: error.response?.data?.message || 'Failed to login' };
    }
  },

  async register(data: RegisterData): Promise<AuthResponse> {
    try {
      const response = await apiClient.post('/users/register', data);
      return { success: response.status === 200, message: response.data?.message };
    } catch (error: any) {
      return { success: false, message: error.response?.data?.message || 'Failed to register' };
    }
  },

  async logout(): Promise<void> {
    try {
      await apiClient.post('/users/logout');
      currentUser = null;
    } catch {
      currentUser = null;
    }
  },

  async isAuthenticated(): Promise<boolean> {
    try {
      if (currentUser) return true;
      const response = await apiClient.get<UserInfo>('/users/me');
      currentUser = response.status === 200 ? response.data : null;
      return response.status === 200;
    } catch {
      currentUser = null;
      return false;
    }
  },

  getCurrentUser(): UserInfo | null {
    return currentUser;
  }
};