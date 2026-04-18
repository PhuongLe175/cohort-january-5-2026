import { apiClient } from '../../api';
import type { TransactionListDto, GetTransactionsParams, ImportResult, ImportTransactionsParams } from './types';

export const transactionsApi = {
  async getTransactions(params: GetTransactionsParams = {}): Promise<TransactionListDto> {
    const { page = 1, pageSize = 20 } = params;
    const response = await apiClient.get<TransactionListDto>('/transactions', {
      params: { page, pageSize }
    });
    return response.data;
  },

  async importTransactions(params: ImportTransactionsParams): Promise<ImportResult> {
    try {
      const response = await apiClient.post<ImportResult>(
        '/transactions/import',
        params.formData,
        {
          headers: { 'Content-Type': 'multipart/form-data' },
          onUploadProgress: params.onUploadProgress as any
        }
      );
      return response.data as ImportResult;
    } catch (error) {
      if (error && typeof error === 'object' && 'response' in error) {
        const axiosError = error as { response?: { data?: string } };
        throw new Error(axiosError.response?.data || 'Failed to import transactions');
      }
      throw error;
    }
  }
};
