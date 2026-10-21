import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useProject } from '../contexts/ProjectContext';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { CheckCircle, Loader2, Search, Sparkles } from 'lucide-react';

interface ScanStep {
  id: string;
  label: string;
  completed: boolean;
}

const OpportunitiesScanner = () => {
  const navigate = useNavigate();
  const { selectedProject } = useProject();
  const [url, setUrl] = useState('');
  const [isScanning, setIsScanning] = useState(false);
  const [scanSteps, setScanSteps] = useState<ScanStep[]>([
    { id: 'audit', label: 'Run your first site audit', completed: false },
    { id: 'prompts', label: 'Add prompts to track', completed: false },
    { id: 'console', label: 'Connect Google Search Console — Get search traffic data', completed: false },
    { id: 'analytics', label: 'Connect Google Analytics — Understand traffic sources', completed: false },
    { id: 'competitors', label: 'Add competitors to track', completed: false },
  ]);
  const [currentStep, setCurrentStep] = useState(0);

  const handleStartScan = async () => {
    if (!url.trim()) return;

    setIsScanning(true);
    setCurrentStep(0);

    // Simulate scanning process
    for (let i = 0; i < scanSteps.length; i++) {
      await new Promise(resolve => setTimeout(resolve, 1500));
      setCurrentStep(i + 1);
      setScanSteps(prev => prev.map((step, idx) => 
        idx <= i ? { ...step, completed: true } : step
      ));
    }

    // After scanning completes, navigate to results
    setTimeout(() => {
      navigate(`/projects/${selectedProject?.id}/opportunities/results`, { 
        state: { scannedUrl: url } 
      });
    }, 1000);
  };

  const completedCount = scanSteps.filter(s => s.completed).length;

  return (
    <div className="min-h-screen bg-gradient-to-br via-blue-50/30 to-gray-50">
      

      {/* Main Content */}
      <div className="max-w-5xl mx-auto px-6 py-8">
        <div className="text-center mb-8">
          {!isScanning && (
            <div className="inline-flex items-center gap-2 px-3 py-1.5 bg-gray-900 text-white rounded-full text-xs font-semibold mb-4 animate-pulse">
              <Sparkles className="w-3 h-3" />
              AI-Powered Analysis
            </div>
          )}
          <h2 className="text-2xl font-bold text-gray-900 mb-3">
            {isScanning ? "We're gathering data for your opportunities" : "Scan Your Website for Opportunities"}
          </h2>
          <p className="text-sm text-gray-600 max-w-2xl mx-auto">
            {isScanning 
              ? "Complete your setup to get personalized recommendations."
              : "Enter your website URL to discover SEO opportunities, technical issues, and growth potential."
            }
          </p>
        </div>

        {!isScanning ? (
          <div className="bg-white rounded-xl border border-gray-200/50 shadow-xl shadow-gray-200/50 p-6 max-w-3xl mx-auto backdrop-blur-sm">
            <div className="flex items-center gap-2 mb-4">
              <div className="w-10 h-10 rounded-lg bg-gray-100 flex items-center justify-center">
                <Search className="w-5 h-5 text-gray-900" />
              </div>
              <div>
                <label className="block text-sm font-bold text-gray-900">
                  Website URL
                </label>
                <p className="text-xs text-gray-500">Enter your domain to begin analysis</p>
              </div>
            </div>
            <div className="flex gap-2">
              <div className="flex-1 relative">
                <Input
                  type="url"
                  value={url}
                  onChange={(e) => setUrl(e.target.value)}
                  onKeyPress={(e) => e.key === 'Enter' && url.trim() && handleStartScan()}
                  placeholder="https://example.com"
                  className="h-10 pl-3 pr-3 text-sm border-2 border-gray-200 focus:border-gray-900 focus:ring-2 focus:ring-gray-900/20 rounded-lg transition-all"
                />
              </div>
              <Button
                onClick={handleStartScan}
                disabled={!url.trim()}
                className="px-6 h-10 bg-gray-900 hover:bg-gray-800 text-white font-semibold rounded-lg shadow-lg shadow-gray-900/30 hover:shadow-xl hover:shadow-gray-900/40 transition-all transform hover:scale-105 active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed disabled:transform-none text-sm"
              >
                <Search className="w-3.5 h-3.5 mr-1.5" />
                Start Scan
              </Button>
            </div>
            <div className="mt-4 pt-4 border-t border-gray-100">
              <div className="flex items-center justify-center gap-4 text-xs text-gray-500">
                <div className="flex items-center gap-1.5">
                  <CheckCircle className="w-3.5 h-3.5 text-green-500" />
                  <span>Technical SEO</span>
                </div>
                <div className="flex items-center gap-1.5">
                  <CheckCircle className="w-3.5 h-3.5 text-green-500" />
                  <span>Content Analysis</span>
                </div>
                <div className="flex items-center gap-1.5">
                  <CheckCircle className="w-3.5 h-3.5 text-green-500" />
                  <span>Performance Check</span>
                </div>
              </div>
            </div>
          </div>
        ) : (
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
        )}
      </div>
    </div>
  );
};

export default OpportunitiesScanner;
