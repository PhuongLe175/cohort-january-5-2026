interface SkeletonProps {
  className?: string;
  lines?: number;
  height?: string;
}

export default function Skeleton({ className = '', lines = 1, height = 'h-4' }: SkeletonProps) {
  if (lines === 1) {
    return (
      <div className={`animate-pulse bg-gray-200 rounded ${height} ${className}`} />
    );
  }

  return (
    <div className={`space-y-2 ${className}`}>
      {Array.from({ length: lines }).map((_, index) => (
        <div
          key={index}
          className={`animate-pulse bg-gray-200 rounded ${height} ${
            index === lines - 1 ? 'w-3/4' : 'w-full'
          }`}
        />
      ))}
    </div>
  );
}

// Specific skeleton components for common use cases
export function SkeletonCard({ className = '' }: { className?: string }) {
  return (
    <div className={`bg-white rounded-lg border border-gray-200 p-6 ${className}`}>
      <div className="flex items-center">
        <div className="flex-shrink-0">
          <div className="w-8 h-8 animate-pulse bg-gray-200 rounded" />
        </div>
        <div className="ml-4 w-0 flex-1">
          <div className="space-y-2">
            <div className="animate-pulse bg-gray-200 rounded h-4 w-1/3" />
            <div className="animate-pulse bg-gray-200 rounded h-6 w-1/2" />
          </div>
        </div>
      </div>
    </div>
  );
}

export function SkeletonTableRow({ className = '' }: { className?: string }) {
  return (
    <tr className={`animate-pulse ${className}`}>
      <td className="px-6 py-4 whitespace-nowrap">
        <div className="bg-gray-200 rounded h-4 w-20" />
      </td>
      <td className="px-6 py-4">
        <div className="bg-gray-200 rounded h-4 w-32" />
      </td>
      <td className="px-6 py-4 whitespace-nowrap">
        <div className="bg-gray-200 rounded h-4 w-16" />
      </td>
      <td className="px-6 py-4 whitespace-nowrap">
        <div className="bg-gray-200 rounded h-4 w-24" />
      </td>
    </tr>
  );
}

export function SkeletonCardRow({ className = '' }: { className?: string }) {
  return (
    <div className={`bg-white rounded-lg border border-gray-200 p-4 animate-pulse ${className}`}>
      <div className="space-y-3">
        <div className="flex justify-between items-start">
          <div className="flex-1 min-w-0 space-y-2">
            <div className="bg-gray-200 rounded h-4 w-3/4" />
            <div className="bg-gray-200 rounded h-3 w-1/2" />
          </div>
          <div className="ml-4 flex-shrink-0">
            <div className="bg-gray-200 rounded h-5 w-16" />
          </div>
        </div>
        <div className="flex items-center">
          <div className="bg-gray-200 rounded-full h-6 w-20" />
        </div>
      </div>
    </div>
  );
}
