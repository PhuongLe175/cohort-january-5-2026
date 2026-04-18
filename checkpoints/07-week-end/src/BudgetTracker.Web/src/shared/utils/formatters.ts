import { format } from 'date-fns';

/**
 * Format currency amounts with proper sign and locale formatting
 */
export function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(amount);
}

/**
 * Format amounts with sign prefix and consistent decimal places
 */
export function formatAmount(amount: number): string {
  const sign = amount < 0 ? '-' : '+';
  return `${sign}$${Math.abs(amount).toLocaleString('en-US', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  })}`;
}

/**
 * Format percentage values
 */
export function formatPercentage(value: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'percent',
    minimumFractionDigits: 1,
    maximumFractionDigits: 1
  }).format(value / 100);
}

/**
 * Format large numbers with abbreviations (K, M, B)
 */
export function formatCompactNumber(value: number): string {
  return new Intl.NumberFormat('en-US', {
    notation: 'compact',
    compactDisplay: 'short',
    maximumFractionDigits: 1
  }).format(value);
}

/**
 * Format date strings consistently across the app
 */
export function formatDate(dateString: string): string {
  return format(new Date(dateString), 'MMM dd, yyyy');
}

/**
 * Format date with time
 */
export function formatDateTime(dateString: string): string {
  return format(new Date(dateString), 'MMM dd, yyyy • h:mm a');
}

/**
 * Format date for display in forms (YYYY-MM-DD)
 */
export function formatDateForInput(dateString: string): string {
  return format(new Date(dateString), 'yyyy-MM-dd');
}