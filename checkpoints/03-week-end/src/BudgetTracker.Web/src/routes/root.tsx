import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useToast } from '../shared/contexts/ToastContext';
import { authApi } from '../features/auth';

const WalletIcon = () => (
  <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
  </svg>
);

export default function Root() {
  const navigate = useNavigate();
  const { showToast } = useToast();

  const handleLogout = async () => {
    try {
      await authApi.logout();
      navigate('/login');
    } catch (error) {
      showToast('error', 'Logout Failed', 'Unable to complete logout process');
    }
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white/80 backdrop-blur-md border-b border-gray-200/60 sticky top-0 z-50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-18">
            <div className="flex items-center">
              <NavLink to="/" className="flex items-center space-x-3 text-xl font-semibold text-gray-900 hover:text-gray-700 transition-colors duration-200 group">
                <div className="p-2 bg-blue-50 rounded-xl group-hover:bg-blue-100 transition-colors duration-200">
                  <WalletIcon />
                </div>
                <span className="hidden sm:block">Budget Tracker</span>
              </NavLink>
            </div>
            <div className="flex items-center space-x-2">
              <NavLink
                to="/dashboard"
                className={({ isActive }: { isActive: boolean }) =>
                  `px-4 py-2.5 rounded-xl text-sm font-medium transition-all duration-200 ${isActive
                    ? 'bg-blue-100 text-blue-700 shadow-soft'
                    : 'text-primary-600 hover:text-gray-900 hover:bg-gray-50'
                  }`
                }
              >
                Dashboard
              </NavLink>
              <NavLink
                to="/transactions"
                className={({ isActive }: { isActive: boolean }) =>
                  `px-4 py-2.5 rounded-xl text-sm font-medium transition-all duration-200 ${isActive
                    ? 'bg-blue-100 text-blue-700 shadow-soft'
                    : 'text-primary-600 hover:text-gray-900 hover:bg-gray-50'
                  }`
                }
              >
                Transactions
              </NavLink>
              <NavLink
                to="/import"
                className={({ isActive }: { isActive: boolean }) =>
                  `px-4 py-2.5 rounded-xl text-sm font-medium transition-all duration-200 ${isActive
                    ? 'bg-blue-100 text-blue-700 shadow-soft'
                    : 'text-primary-600 hover:text-gray-900 hover:bg-gray-50'
                  }`
                }
              >
                Import
              </NavLink>
              <button
                onClick={handleLogout}
                className="px-4 py-2.5 rounded-xl text-sm font-medium text-red-600 hover:text-red-700 hover:bg-red-50 transition-all duration-200"
              >
                Logout
              </button>
            </div>
          </div>
        </div>
      </nav>

      <main className="max-w-7xl mx-auto py-8 px-4 sm:px-6 lg:px-8">
        <Outlet />
      </main>
    </div>
  )
}