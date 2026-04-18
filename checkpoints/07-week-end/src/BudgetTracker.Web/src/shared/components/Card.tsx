import { type ReactNode } from 'react';

interface CardProps {
  children: ReactNode;
  className?: string;
  padding?: 'sm' | 'md' | 'lg';
  shadow?: 'none' | 'soft' | 'soft-md' | 'soft-lg';
  hover?: boolean;
}

export default function Card({
  children,
  className = '',
  padding = 'md',
  shadow = 'soft',
  hover = true
}: CardProps) {
  const paddingClasses = {
    sm: 'p-5',
    md: 'p-6',
    lg: 'p-8'
  };

  const shadowClasses = {
    none: '',
    soft: 'shadow-soft',
    'soft-md': 'shadow-soft-md',
    'soft-lg': 'shadow-soft-lg'
  };

  const hoverClass = hover ? 'hover:shadow-soft-md hover:-translate-y-0.5' : '';

  return (
    <div className={`bg-white rounded-2xl border border-gray-200/60 ${paddingClasses[padding]} ${shadowClasses[shadow]} ${hoverClass} transition-all duration-300 ease-out ${className}`}>
      {children}
    </div>
  );
}
