import React, { useState, useEffect } from 'react';
import { X, CheckCircle, Circle, Sparkles, Loader2, ChevronRight } from 'lucide-react';
import { toast } from '../utils/toast';
import axios from 'axios';

interface GeneratedPrompt {
  promptText: string;
  topic: string;
  rationale: string;
  suggestedPlatforms: string[];
  isSelected: boolean;
}

interface OnboardingPromptsModalProps {
  isOpen: boolean;
  onClose: () => void;
  projectId: number;
  industry?: string;
  companyName?: string;
  targetAudience?: string;
}

export const OnboardingPromptsModal: React.FC<OnboardingPromptsModalProps> = ({
  isOpen,
  onClose,
  projectId,
  industry,
  companyName,
  targetAudience
}) => {
  const [prompts, setPrompts] = useState<GeneratedPrompt[]>([]);
  const [saving, setSaving] = useState(false);
  const [step, setStep] = useState<'generating' | 'selecting'>('generating');

  useEffect(() => {
    if (isOpen && step === 'generating') {
      generatePrompts();
    }
  }, [isOpen]);

  const generatePrompts = async () => {
    try {
      const token = localStorage.getItem('accessToken');
      const response = await axios.post(
        '/api/onboarding/generate-prompts',
        {
          projectId,
          industry,
          companyName,
          targetAudience
        },
        {
          headers: { Authorization: `Bearer ${token}` }
        }
      );

      if (response.data.success) {
        setPrompts(response.data.prompts.map((p: GeneratedPrompt) => ({ ...p, isSelected: true })));
        setStep('selecting');
      } else {
        toast.error(response.data.errorMessage || 'Failed to generate prompts');
      }
    } catch (error: any) {
      console.error('Error generating prompts:', error);
      toast.error(error.response?.data?.errorMessage || 'Failed to generate prompts');
    }
  };

  const togglePrompt = (index: number) => {
    setPrompts(prev => prev.map((p, i) => 
      i === index ? { ...p, isSelected: !p.isSelected } : p
    ));
  };

  const handleSave = async () => {
    const selectedPrompts = prompts.filter(p => p.isSelected);
    
    if (selectedPrompts.length === 0) {
      toast.error('Please select at least one prompt');
      return;
    }

    setSaving(true);
    try {
      const token = localStorage.getItem('accessToken');
      const response = await axios.post(
        '/api/onboarding/save-prompts',
        {
          projectId,
          selectedPrompts: selectedPrompts.map(p => ({
            promptText: p.promptText,
            topic: p.topic,
            platforms: p.suggestedPlatforms
          }))
        },
        {
          headers: { Authorization: `Bearer ${token}` }
        }
      );

      if (response.data.success) {
        toast.success(`${response.data.promptsCreated} prompts created! Visibility checks are running.`);
        onClose();
        // Refresh the prompts page
        window.location.href = '/prompts';
      } else {
        toast.error(response.data.errorMessage || 'Failed to save prompts');
      }
    } catch (error: any) {
      console.error('Error saving prompts:', error);
      toast.error(error.response?.data?.errorMessage || 'Failed to save prompts');
    } finally {
      setSaving(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-[9999] flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-2xl max-w-4xl w-full max-h-[90vh] overflow-hidden flex flex-col">
        {/* Header */}
        <div className="bg-gradient-to-r from-blue-600 to-indigo-600 px-6 py-5 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 bg-white/20 rounded-lg flex items-center justify-center">
              <Sparkles className="w-6 h-6 text-white" />
            </div>
            <div>
              <h2 className="text-xl font-bold text-white">Welcome to AI Visibility Tracking!</h2>
              <p className="text-sm text-blue-100">Let's get you started with custom prompts</p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="w-8 h-8 rounded-lg hover:bg-white/20 flex items-center justify-center transition-colors"
          >
            <X className="w-5 h-5 text-white" />
          </button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-6">
          {step === 'generating' && (
            <div className="flex flex-col items-center justify-center py-12">
              <Loader2 className="w-16 h-16 text-blue-600 animate-spin mb-4" />
              <h3 className="text-xl font-semibold text-gray-900 mb-2">Generating Your Custom Prompts</h3>
              <p className="text-sm text-gray-600 text-center max-w-md">
                Our AI is creating 5 personalized prompts based on your industry and target audience...
              </p>
            </div>
          )}

          {step === 'selecting' && (
            <div className="space-y-4">
              <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6">
                <h3 className="text-sm font-semibold text-blue-900 mb-1">Select Prompts to Track</h3>
                <p className="text-xs text-blue-700">
                  We've generated 5 prompts for you. Select the ones you'd like to track across AI platforms. 
                  We'll automatically check how ChatGPT, Claude, and Gemini respond to these prompts.
                </p>
              </div>

              {prompts.map((prompt, index) => (
                <div
                  key={index}
                  className={`border-2 rounded-xl p-5 transition-all cursor-pointer ${
                    prompt.isSelected
                      ? 'border-blue-500 bg-blue-50/50'
                      : 'border-gray-200 bg-white hover:border-gray-300'
                  }`}
                  onClick={() => togglePrompt(index)}
                >
                  <div className="flex items-start gap-4">
                    <div className="flex-shrink-0 mt-1">
                      {prompt.isSelected ? (
                        <CheckCircle className="w-6 h-6 text-blue-600 fill-current" />
                      ) : (
                        <Circle className="w-6 h-6 text-gray-400" />
                      )}
                    </div>
                    <div className="flex-1">
                      <div className="flex items-start justify-between gap-4 mb-2">
                        <h4 className="text-base font-semibold text-gray-900 flex-1">
                          {prompt.promptText}
                        </h4>
                        <span className="px-2 py-1 bg-indigo-100 text-indigo-700 text-xs font-medium rounded">
                          {prompt.topic}
                        </span>
                      </div>
                      <p className="text-sm text-gray-600 mb-3">{prompt.rationale}</p>
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className="text-xs text-gray-500 font-medium">Platforms:</span>
                        {prompt.suggestedPlatforms.map((platform, i) => (
                          <span
                            key={i}
                            className="px-2 py-1 bg-gray-100 text-gray-700 text-xs rounded"
                          >
                            {platform}
                          </span>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Footer */}
        {step === 'selecting' && (
          <div className="border-t border-gray-200 px-6 py-4 bg-gray-50 flex items-center justify-between">
            <div className="text-sm text-gray-600">
              <span className="font-semibold text-gray-900">
                {prompts.filter(p => p.isSelected).length}
              </span>{' '}
              of {prompts.length} prompts selected
            </div>
            <div className="flex items-center gap-3">
              <button
                onClick={onClose}
                className="px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-100 rounded-lg transition-colors"
              >
                Skip for Now
              </button>
              <button
                onClick={handleSave}
                disabled={saving || prompts.filter(p => p.isSelected).length === 0}
                className="px-6 py-2 bg-blue-600 text-white text-sm font-semibold rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center gap-2"
              >
                {saving ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin" />
                    Saving...
                  </>
                ) : (
                  <>
                    Start Tracking
                    <ChevronRight className="w-4 h-4" />
                  </>
                )}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
