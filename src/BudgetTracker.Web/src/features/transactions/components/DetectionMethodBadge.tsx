interface DetectionMethodBadgeProps {
  method?: string;
  confidence?: number;
}

function DetectionMethodBadge({ method, confidence }: DetectionMethodBadgeProps) {
  if (!method) return null;

  const isAI = method === 'AI';

  return (
    <div className="flex items-center gap-2">
      <span className={`px-2.5 py-1 rounded-full text-xs font-medium ${
        isAI
          ? 'bg-purple-100 text-purple-700'
          : 'bg-blue-100 text-blue-700'
      }`}>
        {isAI ? 'AI Detection' : 'Pattern Match'}
      </span>
      {confidence !== undefined && (
        <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-600">
          {Math.round(confidence)}% confidence
        </span>
      )}
    </div>
  );
}

export default DetectionMethodBadge;
