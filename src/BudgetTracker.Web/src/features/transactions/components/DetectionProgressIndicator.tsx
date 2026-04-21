type Phase = 'uploading' | 'detecting' | 'parsing' | 'enhancing' | 'complete';

interface PhaseConfig {
  key: Phase;
  label: string;
  description: string;
}

const PHASES: PhaseConfig[] = [
  { key: 'uploading',  label: 'Uploading',  description: 'Sending file to server...' },
  { key: 'detecting',  label: 'Detecting',  description: 'Identifying CSV structure...' },
  { key: 'parsing',    label: 'Parsing',    description: 'Reading transactions...' },
  { key: 'enhancing',  label: 'Enhancing',  description: 'Applying AI enhancements...' },
  { key: 'complete',   label: 'Complete',   description: 'All done!' },
];

const PHASE_ORDER: Phase[] = ['uploading', 'detecting', 'parsing', 'enhancing', 'complete'];

interface DetectionProgressIndicatorProps {
  currentPhase: Phase;
  progress: number;
}

function DetectionProgressIndicator({ currentPhase, progress }: DetectionProgressIndicatorProps) {
  const currentIndex = PHASE_ORDER.indexOf(currentPhase);
  const activePhase = PHASES[currentIndex];

  return (
    <div className="space-y-4">
      {/* Stepper */}
      <div className="flex items-center">
        {PHASES.map((phase, index) => {
          const isDone   = index < currentIndex;
          const isActive = index === currentIndex;

          return (
            <div key={phase.key} className="flex items-center flex-1 last:flex-none">
              {/* Circle */}
              <div className="flex flex-col items-center">
                <div className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-semibold border-2 transition-colors ${
                  isDone   ? 'bg-green-500 border-green-500 text-white' :
                  isActive ? 'bg-blue-500 border-blue-500 text-white' :
                             'bg-white border-gray-300 text-gray-400'
                }`}>
                  {isDone ? (
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M5 13l4 4L19 7" />
                    </svg>
                  ) : (
                    <span>{index + 1}</span>
                  )}
                </div>
                <span className={`mt-1 text-xs whitespace-nowrap ${
                  isDone   ? 'text-green-600 font-medium' :
                  isActive ? 'text-blue-600 font-semibold' :
                             'text-gray-400'
                }`}>
                  {phase.label}
                </span>
              </div>

              {/* Connector line (not after last item) */}
              {index < PHASES.length - 1 && (
                <div className={`flex-1 h-0.5 mx-2 mb-5 transition-colors ${
                  isDone ? 'bg-green-400' : 'bg-gray-200'
                }`} />
              )}
            </div>
          );
        })}
      </div>

      {/* Progress bar */}
      <div className="w-full bg-gray-100 rounded-full h-1.5">
        <div
          className="h-1.5 rounded-full bg-blue-500 transition-all duration-500"
          style={{ width: `${progress}%` }}
        />
      </div>

      {/* Active phase description */}
      <p className="text-sm text-gray-500 text-center">
        {activePhase?.description ?? 'Processing...'}
      </p>
    </div>
  );
}

export default DetectionProgressIndicator;
export type { Phase };
