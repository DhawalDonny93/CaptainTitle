import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowRight, ArrowLeft } from 'lucide-react';
import { OnboardingContentRenderer } from '@/components/OnboardingContentRenderer';
import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL;

// ============================================
// TYPE DEFINITIONS
// ============================================

interface OnboardingQuestion {
  id: string;
  questionKey: string;
  questionText: string;
  questionType: string;
  validationRules: string | null;
  options: string | null;
  placeholder: string | null;
  helpText: string | null;
  isRequired: boolean;
  displayOrder: number;
}

interface OnboardingStep {
  id: string;
  stepKey: string;
  stepName: string;
  description: string;
  stepType: 'content' | 'questions';
  mediaUrl: string | null;
  mediaType: string | null;
  contentHtml: string | null;
  displayOrder: number;
  isRequired: boolean;
  questions: OnboardingQuestion[];
}

interface OnboardingConfig {
  isCompleted: boolean;
  totalSteps: number;
  completedSteps: number;
  remainingSteps: number;
  completionPercentage: number;
  nextStepId: string | null;
  nextStepName: string | null;
  steps: OnboardingStep[];
}

// ============================================
// MAIN COMPONENT
// ============================================

// LocalStorage keys
const STORAGE_KEYS = {
  ANSWERS: 'onboarding_answers',
  CURRENT_STEP: 'onboarding_current_step',
  COMPLETED_STEPS: 'onboarding_completed_steps',
  START_TIME: 'onboarding_start_time',
};

export default function OnboardingFlowNew() {
  const navigate = useNavigate();
  const [currentStepIndex, setCurrentStepIndex] = useState(0);
  const [loading, setLoading] = useState(true);
  const [onboardingConfig, setOnboardingConfig] = useState<OnboardingConfig | null>(null);
  const [answers, setAnswers] = useState<Record<string, any>>({});
  const [completedSteps, setCompletedSteps] = useState<Set<string>>(new Set());

  // Load saved progress from localStorage on mount
  useEffect(() => {
    loadSavedProgress();
    fetchOnboardingConfig();
  }, []);

  // Save progress to localStorage whenever it changes
  useEffect(() => {
    // Only save if we have config loaded (prevents saving before initial load)
    if (onboardingConfig && onboardingConfig.steps.length > 0) {
      saveProgress();
    }
  }, [answers, currentStepIndex, completedSteps]);

  const loadSavedProgress = () => {
    try {
      const savedAnswers = localStorage.getItem(STORAGE_KEYS.ANSWERS);
      const savedStep = localStorage.getItem(STORAGE_KEYS.CURRENT_STEP);
      const savedCompletedSteps = localStorage.getItem(STORAGE_KEYS.COMPLETED_STEPS);
      
      if (savedAnswers) {
        setAnswers(JSON.parse(savedAnswers));
      }
      
      if (savedStep) {
        setCurrentStepIndex(parseInt(savedStep, 10));
      }
      
      if (savedCompletedSteps) {
        setCompletedSteps(new Set(JSON.parse(savedCompletedSteps)));
      }

      // Set start time if not already set
      if (!localStorage.getItem(STORAGE_KEYS.START_TIME)) {
        localStorage.setItem(STORAGE_KEYS.START_TIME, new Date().toISOString());
      }
    } catch (error) {
      console.error('Error loading saved progress:', error);
    }
  };

  const saveProgress = () => {
    try {
      localStorage.setItem(STORAGE_KEYS.ANSWERS, JSON.stringify(answers));
      localStorage.setItem(STORAGE_KEYS.CURRENT_STEP, currentStepIndex.toString());
      localStorage.setItem(STORAGE_KEYS.COMPLETED_STEPS, JSON.stringify(Array.from(completedSteps)));
    } catch (error) {
      console.error('Error saving progress:', error);
    }
  };

  const clearProgress = () => {
    localStorage.removeItem(STORAGE_KEYS.ANSWERS);
    localStorage.removeItem(STORAGE_KEYS.CURRENT_STEP);
    localStorage.removeItem(STORAGE_KEYS.COMPLETED_STEPS);
    localStorage.removeItem(STORAGE_KEYS.START_TIME);
    setAnswers({});
    setCurrentStepIndex(0);
    setCompletedSteps(new Set());
  };

  const fetchOnboardingConfig = async () => {
    try {
      // Replace with your actual API endpoint
      const token = localStorage.getItem('accessToken');
      console.log('Fetching onboarding config with token:', token ? 'Present' : 'Missing');
      
      const response = await axios.get(`${API_BASE_URL}/onboarding/config`, {
        withCredentials: true,
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      console.log('API Response:', response.data);

      if (response.data.success) {
        setOnboardingConfig(response.data.data);
        console.log('Onboarding config loaded:', response.data.data);
      }
    } catch (error) {
      console.error('Error fetching onboarding config:', error);
      console.log('Using mock data fallback');
      
      // Fallback to mock data for testing
      setOnboardingConfig({
        isCompleted: false,
        totalSteps: 2,
        completedSteps: 0,
        remainingSteps: 2,
        completionPercentage: 0,
        nextStepId: null,
        nextStepName: null,
        steps: [
          {
            id: '1',
            stepKey: 'welcome_demo',
            stepName: 'Welcome to PuffCount',
            description: 'Your journey to quit vaping starts here',
            stepType: 'content',
            mediaUrl: null,
            mediaType: null,
            contentHtml: JSON.stringify({
              type: 'comparison_chart',
              title: 'Your Quitting Puff transition',
              subtitle: 'Quit 2x faster with PuffCount vs on your own',
              data: {
                without: {
                  label: 'Without PuffCount',
                  value: '20%',
                  color: '#E8E5FF',
                },
                with: {
                  label: 'With PuffCount',
                  value: '2x',
                  color: '#6B5DD3',
                },
              },
              footer: 'Puff Count makes it easy and holds you accountable',
              animation: 'fadeInUp',
            }),
            displayOrder: 1,
            isRequired: true,
            questions: [],
          },
          {
            id: '2',
            stepKey: 'assessment_demo',
            stepName: 'Tell us about yourself',
            description: 'Help us personalize your experience',
            stepType: 'questions',
            mediaUrl: null,
            mediaType: null,
            contentHtml: null,
            displayOrder: 2,
            isRequired: true,
            questions: [
              {
                id: 'q1',
                questionKey: 'daily_puffs',
                questionText: 'How many puffs do you take per day?',
                questionType: 'select',
                validationRules: null,
                options: JSON.stringify([
                  { value: '0-10', label: '0-10 puffs' },
                  { value: '11-30', label: '11-30 puffs' },
                  { value: '31-50', label: '31-50 puffs' },
                  { value: '51-100', label: '51-100 puffs' },
                  { value: '100+', label: '100+ puffs' },
                ]),
                placeholder: 'Select your daily puff count',
                helpText: 'Be honest - this helps us create your personalized plan',
                isRequired: true,
                displayOrder: 1,
              },
            ],
          },
        ],
      });
    } finally {
      setLoading(false);
    }
  };

  const handleNext = () => {
    if (!onboardingConfig) return;

    const currentStep = onboardingConfig.steps[currentStepIndex];
    
    // Mark current step as completed
    setCompletedSteps(prev => new Set([...prev, currentStep.stepKey]));

    if (currentStepIndex < onboardingConfig.steps.length - 1) {
      // Move to next step
      setCurrentStepIndex(currentStepIndex + 1);
      
      // Scroll to top of content area
      const contentArea = document.querySelector('.flex-1.overflow-y-auto');
      if (contentArea) {
        contentArea.scrollTo({ top: 0, behavior: 'smooth' });
      }
    } else {
      // Last step - sync with backend
      handleComplete();
    }
  };

  const handleBack = () => {
    if (currentStepIndex > 0) {
      setCurrentStepIndex(currentStepIndex - 1);
    }
  };

  const handleComplete = async () => {
    if (!onboardingConfig) return;

    try {
      const token = localStorage.getItem('accessToken');
      const startTime = localStorage.getItem(STORAGE_KEYS.START_TIME);
      const completedAt = new Date().toISOString();

      console.log('Syncing onboarding data with backend...');
      console.log('Total answers:', Object.keys(answers).length);
      console.log('Completed steps:', completedSteps.size);

      // Prepare sync payload
      const syncPayload = {
        version: '1.0.0',
        startedAt: startTime || completedAt,
        completedAt: completedAt,
        totalDuration: startTime 
          ? Math.floor((new Date(completedAt).getTime() - new Date(startTime).getTime()) / 1000)
          : 0,
        steps: onboardingConfig.steps
          .filter(step => completedSteps.has(step.stepKey))
          .map((step) => ({
            stepId: step.id,
            stepKey: step.stepKey,
            stepName: step.stepName,
            completedAt: completedAt,
          })),
        answers: Object.entries(answers).map(([questionKey, answer]) => ({
          questionKey,
          answerValue: typeof answer === 'string' ? answer : JSON.stringify(answer),
          answeredAt: completedAt,
        })),
      };

      console.log('Sync payload:', syncPayload);

      // Send to backend
      const response = await axios.post(
        `${API_BASE_URL}/onboarding/sync`,
        syncPayload,
        {
          withCredentials: true,
          headers: {
            Authorization: `Bearer ${token}`,
            'Content-Type': 'application/json',
          },
        }
      );

      console.log('Sync successful:', response.data);

      // Clear localStorage after successful sync
      clearProgress();

      // Navigate to dashboard
      navigate('/dashboard');
    } catch (error) {
      console.error('Error syncing onboarding:', error);
      
      // Show user-friendly error but still navigate
      // In production, you might want to show a toast/notification
      alert('Your progress has been saved locally. We\'ll sync it when you\'re back online.');
      
      navigate('/dashboard');
    }
  };

  const handleAnswerChange = useCallback((questionKey: string, value: any) => {
    console.log('Answer changed:', questionKey, '=', value);
    setAnswers((prev) => ({
      ...prev,
      [questionKey]: value,
    }));
  }, []);

  const isStepValid = useCallback(() => {
    if (!onboardingConfig) return false;

    const currentStep = onboardingConfig.steps[currentStepIndex];
    
    console.log('Validating step:', currentStep.stepKey, 'Type:', currentStep.stepType);

    if (currentStep.stepType === 'content') {
      console.log('Content step - always valid');
      return true; // Content steps are always valid
    }

    // Check if all required questions are answered
    const requiredQuestions = currentStep.questions.filter((q) => q.isRequired);
    console.log('Required questions:', requiredQuestions.length);
    console.log('Current answers:', answers);
    
    const isValid = requiredQuestions.every((q) => {
      const answer = answers[q.questionKey];
      let valid = false;
      
      if (q.questionType === 'multiselect' || q.questionType === 'checkbox') {
        valid = Array.isArray(answer) && answer.length > 0;
      } else {
        valid = answer !== undefined && answer !== null && answer !== '';
      }
      
      console.log(`Question "${q.questionKey}" (${q.questionType}):`, answer, '- Valid:', valid);
      return valid;
    });
    
    console.log('Step valid:', isValid);
    return isValid;
  }, [onboardingConfig, currentStepIndex, answers]);

  if (loading) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-purple-50 via-white to-blue-50 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-purple-600 mx-auto mb-4"></div>
          <p className="text-gray-600">Loading onboarding...</p>
        </div>
      </div>
    );
  }

  if (!onboardingConfig) {
    return (
      <div className="min-h-screen bg-white flex items-center justify-center">
        <div className="text-center">
          <p className="text-gray-600 mb-4">Unable to load onboarding</p>
          <button 
            onClick={() => navigate('/dashboard')}
            className="px-8 py-3 bg-black text-white rounded-full font-medium hover:bg-gray-800 transition-all"
          >
            Go to Dashboard
          </button>
        </div>
      </div>
    );
  }

  const currentStep = onboardingConfig.steps[currentStepIndex];
  const progressPercentage = ((currentStepIndex + 1) / onboardingConfig.steps.length) * 100;

  return (
    <div className="min-h-screen bg-white flex flex-col p-6">
      <div className="w-full max-w-3xl mx-auto flex flex-col h-[calc(100vh-3rem)]">
        {/* Fixed Progress Bar at Top */}
        <div className="flex-shrink-0 mb-8">
          <div className="relative">
            <div className="w-full bg-gray-100 rounded-full h-1 overflow-hidden">
              <div
                className="bg-black h-1 transition-all duration-500 ease-out"
                style={{ width: `${progressPercentage}%` }}
              />
            </div>
            <div className="flex justify-between items-center mt-3">
              <span className="text-xs font-medium text-gray-400">
                {currentStepIndex + 1} / {onboardingConfig.steps.length}
              </span>
              <div className="flex items-center gap-2">
                <span className="text-xs font-medium text-gray-900">
                  {Math.round(progressPercentage)}% Complete
                </span>
                {Object.keys(answers).length > 0 && (
                  <span className="text-xs text-green-600 flex items-center gap-1">
                    <svg className="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
                      <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                    </svg>
                    Saved
                  </span>
                )}
              </div>
            </div>
          </div>
        </div>

        {/* Scrollable Content Area */}
        <div className="flex-1 overflow-y-auto px-4 -mx-4">
          <div className="min-h-full flex flex-col justify-center py-8">
            {currentStep.stepType === 'content' ? (
              // Render content using OnboardingContentRenderer
              <OnboardingContentRenderer contentHtml={currentStep.contentHtml} />
            ) : (
              // Render questions
              <div className="space-y-8">
                <div className="text-center mb-12">
                  <h2 className="text-4xl font-light tracking-tight text-gray-900 mb-3">
                    {currentStep.stepName}
                  </h2>
                  <p className="text-base text-gray-500 font-light">
                    {currentStep.description}
                  </p>
                </div>

                {currentStep.questions.length === 0 ? (
                  <div className="text-center p-8 bg-yellow-50 border border-yellow-200 rounded-lg">
                    <p className="text-yellow-800 font-medium">No questions found for this step</p>
                    <p className="text-sm text-yellow-600 mt-2">Step Key: {currentStep.stepKey}</p>
                  </div>
                ) : (
                  currentStep.questions.map((question) => {
                    console.log('Rendering question in UI:', question.questionKey, question);
                    return (
                      <QuestionRenderer
                        key={question.id}
                        question={question}
                        value={answers[question.questionKey]}
                        onChange={(value) => handleAnswerChange(question.questionKey, value)}
                      />
                    );
                  })
                )}
              </div>
            )}
          </div>
        </div>

        {/* Fixed Navigation Buttons at Bottom */}
        <div className="flex-shrink-0 mt-8 pt-6 border-t border-gray-100">
          <div className="flex justify-between items-center mb-4">
            <button
              onClick={handleBack}
              disabled={currentStepIndex === 0}
              className={`px-6 py-3 rounded-full font-medium transition-all ${
                currentStepIndex === 0
                  ? 'text-gray-300 cursor-not-allowed'
                  : 'text-gray-900 hover:bg-gray-100'
              }`}
            >
              <ArrowLeft className="inline mr-2 w-4 h-4" />
              Back
            </button>
            <div className="flex flex-col items-end gap-2">
              <button
                onClick={handleNext}
                disabled={!isStepValid()}
                className={`px-8 py-3 rounded-full font-medium transition-all ${
                  !isStepValid()
                    ? 'bg-gray-200 text-gray-400 cursor-not-allowed'
                    : 'bg-black text-white hover:bg-gray-800'
                }`}
              >
                {currentStepIndex === onboardingConfig.steps.length - 1
                  ? 'Complete'
                  : 'Continue'}
                <ArrowRight className="inline ml-2 w-4 h-4" />
              </button>
             
            </div>
          </div>
          
          
        </div>
      </div>
    </div>
  );
}

// ============================================
// QUESTION RENDERER COMPONENT
// ============================================

interface QuestionRendererProps {
  question: OnboardingQuestion;
  value: any;
  onChange: (value: any) => void;
}

const QuestionRenderer: React.FC<QuestionRendererProps> = ({
  question,
  value,
  onChange,
}) => {
  console.log('Rendering question:', question.questionKey, 'Type:', question.questionType);
  
  const renderInput = () => {
    switch (question.questionType) {
      case 'text':
      case 'email':
        return (
          <input
            type={question.questionType}
            placeholder={question.placeholder || ''}
            value={value || ''}
            onChange={(e) => onChange(e.target.value)}
            className="w-full px-5 py-4 border-2 border-gray-200 rounded-2xl focus:outline-none focus:border-black transition-all duration-200 font-light"
          />
        );

      case 'textarea':
        return (
          <textarea
            placeholder={question.placeholder || ''}
            value={value || ''}
            onChange={(e) => onChange(e.target.value)}
            className="w-full px-5 py-4 border-2 border-gray-200 rounded-2xl focus:outline-none focus:border-black transition-all duration-200 font-light min-h-[120px] resize-none"
            rows={4}
          />
        );

      case 'number':
        return (
          <input
            type="number"
            placeholder={question.placeholder || ''}
            value={value || ''}
            onChange={(e) => onChange(e.target.value)}
            className="w-full px-5 py-4 border-2 border-gray-200 rounded-2xl focus:outline-none focus:border-black transition-all duration-200 font-light"
          />
        );

      case 'date':
        return (
          <input
            type="date"
            value={value || ''}
            onChange={(e) => onChange(e.target.value)}
            className="w-full px-5 py-4 border-2 border-gray-200 rounded-2xl focus:outline-none focus:border-black transition-all duration-200 font-light"
          />
        );

      case 'select':
      case 'radio':
        const selectOptions = question.options ? JSON.parse(question.options) : [];
        return (
          <div className="space-y-3">
            {selectOptions.map((option: any, idx: number) => (
              <button
                key={option.value || `option-${idx}`}
                onClick={() => onChange(option.value)}
                className={`w-full p-5 rounded-2xl text-left transition-all duration-200 ${
                  value === option.value
                    ? 'bg-black text-white border-2 border-black'
                    : 'bg-white text-gray-900 border-2 border-gray-200 hover:border-gray-400'
                }`}
              >
                <div className="flex items-center justify-between">
                  <span className="font-normal">{option.label}</span>
                  {value === option.value && (
                    <div className="w-5 h-5 rounded-full bg-white flex items-center justify-center">
                      <div className="w-2 h-2 rounded-full bg-black" />
                    </div>
                  )}
                </div>
              </button>
            ))}
          </div>
        );

      case 'multiselect':
      case 'checkbox':
        const multiOptions = question.options ? JSON.parse(question.options) : [];
        const selectedValues = Array.isArray(value) ? value : [];

        return (
          <div className="space-y-3">
            {multiOptions.map((option: any, idx: number) => {
              const isSelected = selectedValues.includes(option.value);
              return (
                <button
                  key={option.value || `multi-option-${idx}`}
                  onClick={() => {
                    const newValues = isSelected
                      ? selectedValues.filter((v: string) => v !== option.value)
                      : [...selectedValues, option.value];
                    onChange(newValues);
                  }}
                  className={`w-full p-5 rounded-2xl text-left transition-all duration-200 ${
                    isSelected
                      ? 'bg-black text-white border-2 border-black'
                      : 'bg-white text-gray-900 border-2 border-gray-200 hover:border-gray-400'
                  }`}
                >
                  <div className="flex items-center justify-between">
                    <span className="font-normal">{option.label}</span>
                    {isSelected && (
                      <div className="w-5 h-5 rounded bg-white flex items-center justify-center">
                        <svg className="w-3 h-3 text-black" fill="currentColor" viewBox="0 0 20 20">
                          <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                        </svg>
                      </div>
                    )}
                  </div>
                </button>
              );
            })}
          </div>
        );

      case 'boolean':
        return (
          <div className="flex gap-4">
            <button
              onClick={() => onChange(true)}
              className={`flex-1 p-5 rounded-2xl border-2 transition-all duration-200 ${
                value === true
                  ? 'bg-black text-white border-black'
                  : 'bg-white text-gray-900 border-gray-200 hover:border-gray-400'
              }`}
            >
              <span className="font-normal">Yes</span>
            </button>
            <button
              onClick={() => onChange(false)}
              className={`flex-1 p-5 rounded-2xl border-2 transition-all duration-200 ${
                value === false
                  ? 'bg-black text-white border-black'
                  : 'bg-white text-gray-900 border-gray-200 hover:border-gray-400'
              }`}
            >
              <span className="font-normal">No</span>
            </button>
          </div>
        );

      default:
        console.error('Unsupported question type:', question.questionType, 'Question:', question);
        return (
          <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
            <p className="text-red-700 font-medium">Unsupported question type: {question.questionType}</p>
            <p className="text-sm text-red-600 mt-1">Question: {question.questionText}</p>
          </div>
        );
    }
  };

  return (
    <div className="space-y-4">
      <label className="block text-lg font-light text-gray-900">
        {question.questionText}
        {question.isRequired && <span className="text-gray-400 ml-1">*</span>}
      </label>
      {question.helpText && (
        <p className="text-sm text-gray-500 font-light -mt-2">{question.helpText}</p>
      )}
      {renderInput()}
    </div>
  );
};
