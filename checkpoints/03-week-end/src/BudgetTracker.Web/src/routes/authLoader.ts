import { redirect } from 'react-router-dom';
import { authApi } from '../features/auth';

export async function authLoader() {
  try {
    const isAuthenticated = await authApi.isAuthenticated();
    if (!isAuthenticated) {
      return redirect('/login');
    }
    return null;
  } catch (error) {
    console.error('Auth loader error:', error);
    return redirect('/login');
  }
}