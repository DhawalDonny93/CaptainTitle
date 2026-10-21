import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useProject } from '../contexts/ProjectContext';
import { useAuth } from '../contexts/AuthContext';
import {
  FolderPlus,
  Lightbulb,
  Eye,
  CheckCircle,
  Wrench,
  PenTool,
  TrendingUp,
  ArrowRight,
  Target,
  AlertCircle,
  Loader2,
  Sparkles,
} from 'lucide-react';
import visibilityService from '../services/visibilityService';
import { toast } from '../utils/toast';

interface StepStats {
  promptCount?: number;
  articleCount?: number;
  visibilityScore?: number;
  visibilityChange?: number;
  opportunitiesCount?: number;
  completedOpportunities?: number;
  fixedIssues?: number;
  totalIssues?: number;
  citationCount?: number;
}

const ActionPlanWorkflow = () => {
  const navigate = useNavigate();
  const { selectedProject } = useProject();
  const { user } = useAuth();
  const [stats, setStats] = useState<StepStats>({});
  const [loading, setLoading] = useState(true);
  const [completedSteps, setCompletedSteps] = useState(0);

  useEffect(() => {
    if (selectedProject?.id) {
      loadStats();
    }
  }, [selectedProject]);

  const loadStats = async () => {
    if (!selectedProject?.id) return;
    
    try {
      setLoading(true);
      const data = await visibilityService.getDashboardOverview(selectedProject.id);
      const newStats = {
        promptCount: data.totalPrompts || 0,
        articleCount: 0,
        visibilityScore: data.overallVisibilityScore || 0,
        visibilityChange: 0,
        opportunitiesCount: data.totalOpportunities || 0,
        completedOpportunities: 0,
        citationCount: data.totalCitations || 0,
        fixedIssues: 0,
        totalIssues: 0,
      };
      setStats(newStats);
      
      // Calculate completed steps
      let completed = 1; // Project created
      if (newStats.promptCount > 0) completed++;
      if (newStats.visibilityScore > 0) completed++;
      if (newStats.articleCount > 0) completed++;
      setCompletedSteps(completed);
    } catch (err) {
      console.error('Error loading stats:', err);
      toast.error('Failed to load statistics');
    } finally {
      setLoading(false);
    }
  };

  const steps = [
    {
      id: 1,
      stepLabel: 'Step 1 of 5',
      title: 'Create Project',
      description: 'Set up your brand project with essential details, competitors, and target topics to get started.',
      icon: FolderPlus,
      color: 'bg-blue-500',
      action: () => navigate('/projects'),
      buttonText: 'Manage Projects',
      stats: null,
      isCompleted: true,
    },
    {
      id: 2,
      stepLabel: 'Step 2 of 5',
      title: 'Create Prompts for Research',
      description: 'Generate AI prompts to discover how your brand appears in AI responses and identify visibility opportunities.',
      icon: Lightbulb,
      color: 'bg-purple-500',
      action: () => {
        if (selectedProject?.id) {
          navigate(`/projects/${selectedProject.id}/your-prompts`);
        } else {
          navigate('/your-prompts');
        }
      },
      buttonText: 'Generate Prompts',
      stats: stats.promptCount !== undefined ? `${stats.promptCount} prompts created` : null,
      isCompleted: (stats.promptCount || 0) > 0,
    },
    {
      id: 3,
      stepLabel: 'Step 3 of 5',
      title: 'Check your AI Visibility',
      description: 'Take a guided tour of your AI Visibility dashboard. See how your brand appears across ChatGPT, Claude, and Perplexity.',
      icon: Eye,
      color: 'bg-orange-500',
      action: () => {
        if (selectedProject?.id) {
          navigate(`/projects/${selectedProject.id}/visibility`);
        } else {
          navigate('/visibility-dashboards');
        }
      },
      buttonText: 'Take the tour',
      stats: stats.visibilityScore !== undefined ? `Visibility Score: ${stats.visibilityScore}%` : null,
      secondaryStats: stats.citationCount !== undefined ? `${stats.citationCount} citations` : null,
      isCompleted: (stats.visibilityScore || 0) > 0,
    },
    {
      id: 4,
      stepLabel: 'Step 4 of 5',
      title: 'Optimize Site for LLMs',
      description: 'Ensure your site is AI-readable with proper structure, schema markup, and LLM-friendly content.',
      icon: Wrench,
      color: 'bg-gray-700',
      action: () => {
        if (selectedProject?.id) {
          navigate(`/projects/${selectedProject.id}/audits`);
        } else {
          navigate('/projects');
        }
      },
      buttonText: 'Run Site Audit',
      stats: stats.fixedIssues !== undefined && stats.totalIssues !== undefined
        ? `${stats.fixedIssues}/${stats.totalIssues} issues fixed`
        : null,
      isCompleted: false,
    },
    {
      id: 5,
      stepLabel: 'Step 5 of 5',
      title: 'Create & Publish Articles',
      description: 'Generate AI-optimized articles based on opportunities and publish them on cited sources and relevant portals.',
      icon: PenTool,
      color: 'bg-green-500',
      action: () => {
        if (selectedProject?.id) {
          navigate(`/projects/${selectedProject.id}/content/create`);
        } else {
          navigate('/content');
        }
      },
      buttonText: 'Create Article',
      stats: stats.articleCount !== undefined ? `${stats.articleCount} articles published` : null,
      secondaryStats: stats.completedOpportunities !== undefined
        ? `${stats.completedOpportunities} opportunities completed`
        : null,
      isCompleted: (stats.articleCount || 0) > 0,
    },
  ];

  if (!selectedProject) {
    return (
      <div className="flex items-center justify-center min-h-[60vh]">
        <div className="text-center">
          <AlertCircle className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <h2 className="text-xl font-bold text-gray-900 mb-2">No Project Selected</h2>
          <p className="text-gray-600 mb-4">Please select a project to view your action plan</p>
          <button
            onClick={() => navigate('/projects')}
            className="px-4 py-2 bg-gray-900 text-white rounded-lg hover:bg-gray-800 transition-colors"
          >
            Go to Projects
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-5xl mx-auto px-4 py-6">
      {/* Welcome Header */}
      <div className="mb-6">
        <div className="flex items-center gap-3 mb-2">
          <div className="w-11 h-11 bg-gray-900 rounded-xl flex items-center justify-center">
            <Sparkles className="w-5 h-5 text-white" />
          </div>
          <div>
            <h1 className="text-xl font-bold text-gray-900">Welcome, {user?.firstName || 'there'}.</h1>
            <p className="text-xs text-gray-600">
              Your AI search optimization is almost ready. Complete these steps to get the most out of the platform.
            </p>
          </div>
        </div>
      </div>

      {/* Progress Counter */}
      <div className="flex justify-end mb-4">
        <div className="text-xs font-medium text-gray-500">
          {completedSteps}/{steps.length}
        </div>
      </div>

      {/* Loading State */}
      {loading && (
        <div className="flex justify-center py-8">
          <Loader2 className="w-6 h-6 animate-spin text-gray-400" />
        </div>
      )}

      {/* Steps Grid */}
      {!loading && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          {steps.map((step) => (
            <div
              key={step.id}
              className="bg-white rounded-xl border border-gray-200 shadow-sm hover:shadow-md transition-all duration-200 overflow-hidden"
            >
              {/* Card Content */}
              <div className="p-4">
                {/* Step Label & Icons */}
                <div className="flex items-start justify-between mb-3">
                  <div className="flex items-center gap-2">
                    <div className={`w-8 h-8 ${step.color} rounded-lg flex items-center justify-center`}>
                      <step.icon className="w-4 h-4 text-white" />
                    </div>
                    <span className="text-[10px] font-medium text-gray-500">{step.stepLabel}</span>
                  </div>
                  {step.isCompleted && (
                    <div className="w-5 h-5 bg-green-100 rounded-full flex items-center justify-center">
                      <CheckCircle className="w-3 h-3 text-green-600" />
                    </div>
                  )}
                </div>

                {/* Title */}
                <h3 className="text-sm font-bold text-gray-900 mb-1.5">{step.title}</h3>

                {/* Description */}
                <p className="text-xs text-gray-600 mb-3 leading-relaxed line-clamp-2">{step.description}</p>

                {/* Stats */}
                {(step.stats || step.secondaryStats) && (
                  <div className="flex flex-wrap gap-1.5 mb-3">
                    {step.stats && (
                      <div className="inline-flex items-center gap-1 px-2 py-0.5 bg-gray-100 rounded">
                        <TrendingUp className="w-3 h-3 text-gray-600" />
                        <span className="text-[10px] font-medium text-gray-700">{step.stats}</span>
                      </div>
                    )}
                    {step.secondaryStats && (
                      <div className="inline-flex items-center gap-1 px-2 py-0.5 bg-gray-100 rounded">
                        <Target className="w-3 h-3 text-gray-600" />
                        <span className="text-[10px] font-medium text-gray-700">{step.secondaryStats}</span>
                      </div>
                    )}
                  </div>
                )}

                {/* Action Button */}
                <button
                  onClick={step.action}
                  className="inline-flex items-center gap-1.5 text-xs font-medium text-gray-900 hover:text-gray-700 transition-colors group"
                >
                  <span>{step.buttonText}</span>
                  <ArrowRight className="w-3 h-3 group-hover:translate-x-0.5 transition-transform" />
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Help Section */}
      <div className="mt-8 bg-gray-50 rounded-xl border border-gray-200 p-4">
        <div className="flex items-start gap-3">
          <div className="w-8 h-8 bg-white rounded-lg border border-gray-200 flex items-center justify-center flex-shrink-0">
            <AlertCircle className="w-4 h-4 text-gray-600" />
          </div>
          <div>
            <h3 className="text-sm font-bold text-gray-900 mb-0.5">Need help? Check out our articles</h3>
            <p className="text-xs text-gray-600">
              Learn more about optimizing your AI visibility and getting the most out of the platform.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ActionPlanWorkflow;
