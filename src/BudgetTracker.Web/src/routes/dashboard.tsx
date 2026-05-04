import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import Header from '../shared/components/layout/Header';
import { QueryAssistant, RecommendationsCard, intelligenceApi } from '../features/intelligence';
import type { ProactiveRecommendation } from '../features/intelligence';

export function loader() {
  return {};
}

export default function Dashboard() {
  const [recommendations, setRecommendations] = useState<ProactiveRecommendation[]>([]);

  useEffect(() => {
    intelligenceApi.getRecommendations().then(setRecommendations).catch(() => {});
  }, []);

  return (
    <div className="px-4 py-6 sm:px-0">
      <Header
        title="Dashboard"
        subtitle="Welcome to your budget tracker"
      />

      {recommendations.length > 0 && (
        <div className="mb-6">
          <RecommendationsCard recommendations={recommendations} />
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
        <QueryAssistant className="lg:col-span-2" />

        <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-2">Transactions</h3>
          <p className="text-gray-600 mb-4">
            View and manage your imported transaction data.
          </p>
          <Link
            to="/transactions"
            className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors duration-200"
          >
            View Transactions
          </Link>
        </div>

        <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-2">Spending Summary</h3>
          <p className="text-gray-600">
            Spending charts will be implemented in future steps.
          </p>
        </div>
      </div>
    </div>
  );
}
