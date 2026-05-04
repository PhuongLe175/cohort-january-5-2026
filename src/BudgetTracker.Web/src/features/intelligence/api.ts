import api from '../../api/client';

export interface QueryRequest {
  query: string;
}

export interface TransactionDto {
  id: string;
  date: string;
  description: string;
  amount: number;
  balance?: number;
  category?: string;
  labels?: string;
  importedAt: string;
  account: string;
}

export interface QueryResponse {
  answer: string;
  amount?: number;
  transactions?: TransactionDto[];
}

export interface ProactiveRecommendation {
  id: string;
  title: string;
  description: string;
  type: 'SpendingAlert' | 'SavingsOpportunity' | 'BudgetTip' | 'TrendInsight' | 'CategoryOptimization';
  priority: 'Low' | 'Medium' | 'High';
  amount?: number;
  category?: string;
  createdAt: string;
  expiresAt?: string;
}

export const intelligenceApi = {
  askQuery: async (query: string): Promise<QueryResponse> => {
    const response = await api.post<QueryResponse>('/query/ask', { query });
    return response.data;
  },

  getRecommendations: async (): Promise<ProactiveRecommendation[]> => {
    const response = await api.get<ProactiveRecommendation[]>('/recommendations');
    return response.data;
  }
};
