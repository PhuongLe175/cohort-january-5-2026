import { useState } from 'react';
import { authApi } from '../api';
import type { LoginCredentials, RegisterData, AuthResponse, UserInfo } from '../types';

export function useAuth() {
  const [isLoading, setIsLoading] = useState(false);

  const login = async (credentials: LoginCredentials): Promise<AuthResponse> => {
    setIsLoading(true);
    try {
      const result = await authApi.login(credentials);
      return result;
    } finally {
      setIsLoading(false);
    }
  };

  const register = async (data: RegisterData): Promise<AuthResponse> => {
    setIsLoading(true);
    try {
      const result = await authApi.register(data);
      return result;
    } finally {
      setIsLoading(false);
    }
  };

  const logout = async (): Promise<void> => {
    setIsLoading(true);
    try {
      await authApi.logout();
    } finally {
      setIsLoading(false);
    }
  };

  const getCurrentUser = (): UserInfo | null => {
    return authApi.getCurrentUser();
  };

  const isAuthenticated = async (): Promise<boolean> => {
    return authApi.isAuthenticated();
  };

  return {
    isLoading,
    login,
    register,
    logout,
    getCurrentUser,
    isAuthenticated,
  };
}