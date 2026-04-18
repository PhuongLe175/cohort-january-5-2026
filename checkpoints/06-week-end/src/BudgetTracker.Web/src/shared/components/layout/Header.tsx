interface HeaderProps {
  title: string;
  subtitle?: string;
  className?: string;
  action?: {
    label: string;
    onClick: () => void;
    icon?: React.ReactNode;
  };
}

export default function Header({ title, subtitle, className = '', action }: HeaderProps) {
  return (
    <div className={`mb-8 ${className}`}>
      <div className="flex items-center justify-between">
        <div className="space-y-1">
          <h1 className="text-2xl font-semibold text-gray-900 tracking-tight">{title}</h1>
          {subtitle && (
            <p className="text-sm text-gray-600 font-normal leading-relaxed">{subtitle}</p>
          )}
        </div>
        {action && (
          <button
            onClick={action.onClick}
            className="inline-flex items-center px-4 py-2 rounded-lg bg-indigo-600 text-white text-sm font-medium hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 transition-colors duration-200"
          >
            {action.icon && <span className="mr-2">{action.icon}</span>}
            {action.label}
          </button>
        )}
      </div>
    </div>
  );
}
