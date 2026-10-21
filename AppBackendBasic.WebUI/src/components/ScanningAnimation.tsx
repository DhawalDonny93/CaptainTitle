import { useState, useEffect } from 'react';
import { CheckCircle, Loader2 } from 'lucide-react';

interface ScanStep {
  id: string;
  label: string;
  completed: boolean;
}

interface ScanningAnimationProps {
  onComplete: () => void;
  steps?: ScanStep[];
}

const defaultSteps: ScanStep[] = [
  { id: 'audit', label: 'Run your first site audit', completed: false },
  { id: 'prompts', label: 'Add prompts to track', completed: false },
  { id: 'console', label: 'Connect Google Search Console — Get search traffic data', completed: false },
  { id: 'analytics', label: 'Connect Google Analytics — Understand traffic sources', completed: false },
  { id: 'competitors', label: 'Add competitors to track', completed: false },
];

const ScanningAnimation = ({ onComplete, steps = defaultSteps }: ScanningAnimationProps) => {
  const [scanSteps, setScanSteps] = useState<ScanStep[]>(steps);
  const [currentStep, setCurrentStep] = useState(0);

  useEffect(() => {
    const runScan = async () => {
      // Simulate scanning process
      for (let i = 0; i < scanSteps.length; i++) {
        await new Promise(resolve => setTimeout(resolve, 1500));
        setCurrentStep(i + 1);
        setScanSteps(prev => prev.map((step, idx) => 
          idx <= i ? { ...step, completed: true } : step
        ));
      }

      // After scanning completes, call onComplete
      setTimeout(() => {
        onComplete();
      }, 1000);
    };

    runScan();
  }, []);

  const completedCount = scanSteps.filter(s => s.completed).length;

  return (
    <div className="max-w-3xl mx-auto">
      <div className="bg-white rounded-xl border border-gray-200/50 shadow-xl shadow-gray-200/50 p-6 mb-6 backdrop-blur-sm">
        <div className="flex items-center gap-2 mb-5">
          <div className="w-10 h-10 rounded-lg bg-gray-100 flex items-center justify-center">
            <Loader2 className="w-5 h-5 text-gray-900 animate-spin" />
          </div>
          <div>
            <h3 className="text-base font-bold text-gray-900">
              Analyzing Your Website
            </h3>
            <p className="text-xs text-gray-500">Complete these steps to unlock opportunities</p>
          </div>
        </div>
        
        <div className="space-y-2">
          {scanSteps.map((step, index) => (
            <div
              key={step.id}
              className={`flex items-center gap-3 p-3 rounded-lg border-2 transition-all duration-500 transform ${
                step.completed
                  ? 'bg-gradient-to-r from-green-50 to-emerald-50 border-green-300 scale-[0.98]'
                  : currentStep === index
                  ? 'bg-gray-100 border-gray-400 scale-100 shadow-lg shadow-gray-200'
                  : 'bg-gray-50/50 border-gray-200 scale-[0.98] opacity-60'
              }`}
            >
              <div className={`w-7 h-7 rounded-lg flex items-center justify-center flex-shrink-0 transition-all duration-300 ${
                step.completed
                  ? 'bg-gradient-to-br from-green-500 to-emerald-500 shadow-lg shadow-green-500/30'
                  : currentStep === index
                  ? 'bg-gray-900 shadow-lg shadow-gray-900/30'
                  : 'bg-gray-300'
              }`}>
                {step.completed ? (
                  <CheckCircle className="w-4 h-4 text-white" />
                ) : currentStep === index ? (
                  <Loader2 className="w-4 h-4 text-white animate-spin" />
                ) : (
                  <div className="w-2 h-2 bg-white rounded-full" />
                )}
              </div>
              <div className="flex-1">
                <span className={`text-xs font-semibold block ${
                  step.completed ? 'text-green-900' : currentStep === index ? 'text-gray-900' : 'text-gray-600'
                }`}>
                  {step.label}
                </span>
                {currentStep === index && (
                  <span className="text-xs text-gray-900 mt-0.5 block animate-pulse">Processing...</span>
                )}
              </div>
            </div>
          ))}
        </div>

        <div className="mt-5 pt-4 border-t border-gray-200">
          <div className="flex items-center justify-between">
            <div>
              <div className="text-xs font-semibold text-gray-900 mb-0.5">
                {completedCount} / {scanSteps.length} steps complete
              </div>
              <div className="text-xs text-gray-500">
                Finish setup to unlock recommendations
              </div>
            </div>
            <div className="text-right">
              <div className="text-xl font-bold text-gray-900">
                {Math.round((completedCount / scanSteps.length) * 100)}%
              </div>
            </div>
          </div>
          <div className="mt-3 h-1.5 bg-gray-200 rounded-full overflow-hidden">
            <div 
              className="h-full bg-gray-900 transition-all duration-500 ease-out"
              style={{ width: `${(completedCount / scanSteps.length) * 100}%` }}
            />
          </div>
        </div>
      </div>

      <div className="text-center">
        <button className="text-xs text-gray-500 hover:text-gray-900 transition-colors font-medium inline-flex items-center gap-2 group">
          <span>Generate opportunities anyway</span>
          <span className="transform group-hover:translate-x-1 transition-transform">→</span>
        </button>
      </div>
    </div>
  );
};

export default ScanningAnimation;
