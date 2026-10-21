import { useState, useEffect } from 'react';
import { useProject } from '../contexts/ProjectContext';
import { Button } from '@/components/ui/Button';
import { ExternalLink, AlertCircle, AlertTriangle, Info, Sparkles, Loader2, RefreshCw, Plus } from 'lucide-react';
import opportunityService from '../services/opportunityService';
import { toast } from '../utils/toast';
import ScanningAnimation from '../components/ScanningAnimation';

interface Opportunity {
  id: number;
  title: string;
  description: string;
  impact: 'Critical' | 'High' | 'Medium' | 'Low';
  type: string;
  priority: number;
  affectedPages?: number;
}

const OpportunitiesFound = () => {
  const { selectedProject } = useProject();
  
  const [opportunities, setOpportunities] = useState<Opportunity[]>([]);
  const [selectedImpact, setSelectedImpact] = useState<string>('all');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isScanning, setIsScanning] = useState(false);
  const [selectedOpportunity, setSelectedOpportunity] = useState<Opportunity | null>(null);

  useEffect(() => {
    if (selectedProject?.id) {
      fetchOpportunities();
    } else {
      setLoading(false);
    }
  }, [selectedProject?.id]);

  const fetchOpportunities = async () => {
    if (!selectedProject?.id) return;

    try {
      setLoading(true);
      setError(null);

      const data = await opportunityService.getOpportunitiesByProject(selectedProject.id);
      
      // Map backend data to frontend format
      const mappedOpportunities: Opportunity[] = data.map((opp, index) => ({
        id: opp.id,
        title: opp.title,
        description: opp.description || '',
        impact: opp.priority as 'Critical' | 'High' | 'Medium' | 'Low',
        type: opp.opportunityType,
        priority: index + 1,
        affectedPages: 0
      }));

      setOpportunities(mappedOpportunities);
      // Auto-select first opportunity if none selected
      if (mappedOpportunities.length > 0 && !selectedOpportunity) {
        setSelectedOpportunity(mappedOpportunities[0]);
      }
    } catch (err: any) {
      console.error('Failed to fetch opportunities:', err);
      setError(err.message || 'Failed to load opportunities');
      toast.error('Failed to load opportunities');
    } finally {
      setLoading(false);
    }
  };

  const handleRescan = () => {
    setIsScanning(true);
  };

  const handleScanComplete = async () => {
    await fetchOpportunities();
    setIsScanning(false);
    toast.success('Rescan completed!');
  };

  const filteredOpportunities = selectedImpact === 'all' 
    ? opportunities 
    : opportunities.filter(opp => opp.impact.toLowerCase() === selectedImpact.toLowerCase());

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="w-8 h-8 animate-spin text-blue-600" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-center p-10">
        <AlertCircle className="h-12 w-12 text-red-600 mx-auto mb-4" />
        <h2 className="text-xl font-bold text-gray-900 mb-2">Failed to Load Opportunities</h2>
        <p className="text-gray-600 mb-4">{error}</p>
        <Button onClick={fetchOpportunities}>Try Again</Button>
      </div>
    );
  }

  if (!selectedProject) {
    return (
      <div className="text-center p-10">
        <AlertCircle className="h-12 w-12 text-gray-400 mx-auto mb-4" />
        <h2 className="text-xl font-bold text-gray-900 mb-2">No Project Selected</h2>
        <p className="text-gray-600">Please select a project to view opportunities.</p>
      </div>
    );
  }

  const criticalCount = opportunities.filter(o => o.impact === 'Critical').length;
  const highCount = opportunities.filter(o => o.impact === 'High').length;
  const totalCount = opportunities.length;

  const getImpactIcon = (impact: string) => {
    switch (impact) {
      case 'Critical':
        return <AlertCircle className="w-4 h-4 text-red-600" />;
      case 'High':
        return <AlertTriangle className="w-4 h-4 text-orange-600" />;
      default:
        return <Info className="w-4 h-4 text-blue-600" />;
    }
  };

  const getImpactBadgeClass = (impact: string) => {
    switch (impact) {
      case 'Critical':
        return 'bg-gradient-to-r from-red-500 to-red-600 text-white shadow-lg shadow-red-500/30';
      case 'High':
        return 'bg-gradient-to-r from-orange-500 to-orange-600 text-white shadow-lg shadow-orange-500/30';
      case 'Medium':
        return 'bg-gradient-to-r from-yellow-500 to-yellow-600 text-white shadow-lg shadow-yellow-500/30';
      default:
        return 'bg-gradient-to-r from-blue-500 to-blue-600 text-white shadow-lg shadow-blue-500/30';
    }
  };

  if (isScanning) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-gray-50 via-blue-50/30 to-gray-50">
        <div className="max-w-5xl mx-auto px-6 py-8">
          <div className="text-center mb-8">
            <h2 className="text-2xl font-bold text-gray-900 mb-3">
              Rescanning Your Website
            </h2>
            <p className="text-sm text-gray-600 max-w-2xl mx-auto">
              Analyzing your website for new opportunities and updates.
            </p>
          </div>
          <ScanningAnimation onComplete={handleScanComplete} />
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-50 via-blue-50/30 to-gray-50">
      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-6 py-6">
        {/* Summary Section */}
        <div className="mb-6">
          <div className="flex items-center justify-between mb-3">
            <div className="inline-flex items-center gap-1.5 px-3 py-1 bg-green-100 text-green-700 rounded-full text-xs font-semibold">
              <Sparkles className="w-3 h-3" />
              Scan Complete
            </div>
            <Button
              onClick={handleRescan}
              size="sm"
              className="flex items-center gap-2 bg-gray-900 hover:bg-gray-800 text-white"
            >
              <RefreshCw className="w-3.5 h-3.5" />
              Rescan
            </Button>
          </div>
          <h2 className="text-2xl font-bold mb-2 bg-gradient-to-r from-gray-900 via-green-900 to-gray-900 bg-clip-text text-transparent">
            I found {totalCount} new opportunities for you
          </h2>
          <p className="text-sm text-gray-600 mb-2 max-w-4xl">
            Connect Google Search Console and Analytics 4 immediately to unlock performance data. Then fix the {criticalCount} critical homepage issues blocking AI mentions before tackling remaining technical problems.
          </p>
          <div className="flex items-center gap-2 text-xs text-gray-500">
            <div className="w-1.5 h-1.5 bg-green-500 rounded-full animate-pulse" />
            <span>Last updated: {new Date().toLocaleDateString('en-US', { month: '2-digit', day: '2-digit', year: 'numeric' })}</span>
          </div>
        </div>

        {/* Insights Section */}
        <div className="bg-white rounded-xl border border-gray-200/50 shadow-xl shadow-gray-200/50 p-5 mb-5 backdrop-blur-sm">
          <div className="flex items-center gap-2 mb-4">
            <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-purple-100 to-purple-50 flex items-center justify-center">
              <Sparkles className="w-4 h-4 text-purple-600" />
            </div>
            <h3 className="text-sm font-bold text-gray-900">Key Insights</h3>
          </div>
          <ol className="space-y-2">
            <li className="flex gap-3 p-2.5 bg-gradient-to-r from-red-50 to-orange-50 rounded-lg border border-red-100">
              <span className="flex-shrink-0 w-6 h-6 rounded-lg bg-red-500 text-white font-bold text-xs flex items-center justify-center">1</span>
              <span className="text-xs text-gray-800 font-medium">{criticalCount} critical issues on your homepage are preventing AI from mentioning you</span>
            </li>
            <li className="flex gap-3 p-2.5 bg-gradient-to-r from-blue-50 to-indigo-50 rounded-lg border border-blue-100">
              <span className="flex-shrink-0 w-6 h-6 rounded-lg bg-blue-500 text-white font-bold text-xs flex items-center justify-center">2</span>
              <span className="text-xs text-gray-800 font-medium">Google Search Console not connected — unlock traffic data and search queries</span>
            </li>
            <li className="flex gap-3 p-2.5 bg-gradient-to-r from-orange-50 to-yellow-50 rounded-lg border border-orange-100">
              <span className="flex-shrink-0 w-6 h-6 rounded-lg bg-orange-500 text-white font-bold text-xs flex items-center justify-center">3</span>
              <span className="text-xs text-gray-800 font-medium">{highCount} high-priority technical and content issues affecting 1 pages</span>
            </li>
          </ol>
        </div>

        {/* Filter Tabs */}
        <div className="flex items-center gap-2 mb-5">
          <span className="text-sm font-semibold text-gray-700 mr-2">Filter:</span>
          <button
            onClick={() => setSelectedImpact('all')}
            className={`px-4 py-2 text-xs font-semibold rounded-lg transition-all ${
              selectedImpact === 'all'
                ? 'bg-gradient-to-r from-blue-600 to-blue-500 text-white shadow-lg shadow-blue-500/30'
                : 'bg-white text-gray-600 hover:bg-gray-50 border border-gray-200 hover:border-gray-300'
            }`}
          >
            All ({totalCount})
          </button>
          <button
            onClick={() => setSelectedImpact('critical')}
            className={`px-4 py-2 text-xs font-semibold rounded-lg transition-all ${
              selectedImpact === 'critical'
                ? 'bg-gradient-to-r from-red-600 to-red-500 text-white shadow-lg shadow-red-500/30'
                : 'bg-white text-gray-600 hover:bg-gray-50 border border-gray-200 hover:border-gray-300'
            }`}
          >
            Critical ({criticalCount})
          </button>
          <button
            onClick={() => setSelectedImpact('high')}
            className={`px-4 py-2 text-xs font-semibold rounded-lg transition-all ${
              selectedImpact === 'high'
                ? 'bg-gradient-to-r from-orange-600 to-orange-500 text-white shadow-lg shadow-orange-500/30'
                : 'bg-white text-gray-600 hover:bg-gray-50 border border-gray-200 hover:border-gray-300'
            }`}
          >
            High ({highCount})
          </button>
        </div>

        {/* Split View: Opportunities List + Detail Panel */}
        <div className="flex gap-6">
          {/* Left: Opportunities List */}
          <div className="flex-1 space-y-2.5">
          {filteredOpportunities.map((opportunity, index) => (
            <div
              key={opportunity.id}
              onClick={() => setSelectedOpportunity(opportunity)}
              className={`group bg-white rounded-xl border transition-all duration-300 p-4 cursor-pointer ${
                selectedOpportunity?.id === opportunity.id
                  ? 'border-gray-900 shadow-xl shadow-gray-900/20 scale-[1.02]'
                  : 'border-gray-200/50 shadow-lg shadow-gray-200/50 hover:shadow-xl hover:shadow-gray-300/50 hover:scale-[1.01]'
              }`}
            >
              <div className="flex items-start justify-between gap-4">
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-2">
                    <div className="w-6 h-6 rounded-lg bg-gradient-to-br from-gray-100 to-gray-50 flex items-center justify-center">
                      <span className="text-xs font-bold text-gray-600">#{index + 1}</span>
                    </div>
                    <span className={`px-2 py-0.5 text-xs font-bold rounded-lg ${getImpactBadgeClass(opportunity.impact)}`}>
                      {opportunity.impact}
                    </span>
                    {opportunity.affectedPages && opportunity.affectedPages > 0 && (
                      <span className="px-2 py-0.5 text-xs font-medium bg-gray-100 text-gray-700 rounded-lg">
                        {opportunity.affectedPages} page{opportunity.affectedPages > 1 ? 's' : ''}
                      </span>
                    )}
                  </div>
                  <h3 className="text-sm font-bold text-gray-900 mb-1 flex items-center gap-2">
                    {getImpactIcon(opportunity.impact)}
                    {opportunity.title}
                  </h3>
                  <p className="text-xs text-gray-600 leading-relaxed line-clamp-2">
                    {opportunity.description}
                  </p>
                </div>
              </div>
            </div>
          ))}
          </div>

          {/* Right: Detail Panel */}
          {selectedOpportunity && (
            <div className="w-96 flex-shrink-0">
              <div className="bg-white rounded-xl border border-gray-200/50 shadow-xl shadow-gray-200/50 p-6 sticky top-6">
                {/* Header */}
                <div className="mb-6">
                  <h3 className="text-lg font-bold text-gray-900 mb-3">
                    {selectedOpportunity.title}
                  </h3>
                  <div className="flex items-center gap-2 mb-4">
                    <span className={`px-2.5 py-1 text-xs font-bold rounded-lg ${getImpactBadgeClass(selectedOpportunity.impact)}`}>
                      {selectedOpportunity.impact}
                    </span>
                    {selectedOpportunity.affectedPages && selectedOpportunity.affectedPages > 0 && (
                      <span className="px-2.5 py-1 text-xs font-medium bg-gray-100 text-gray-700 rounded-lg">
                        {selectedOpportunity.affectedPages} page{selectedOpportunity.affectedPages > 1 ? 's' : ''}
                      </span>
                    )}
                  </div>
                </div>

                {/* Details */}
                <div className="space-y-4 mb-6">
                  <div>
                    <label className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1 block">
                      Category
                    </label>
                    <p className="text-sm text-gray-900">{selectedOpportunity.type}</p>
                  </div>

                  <div>
                    <label className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1 block">
                      Impact
                    </label>
                    <div className="flex items-center gap-2">
                      {getImpactIcon(selectedOpportunity.impact)}
                      <p className="text-sm text-gray-900">{selectedOpportunity.impact === 'Critical' ? 'Urgent' : selectedOpportunity.impact}</p>
                    </div>
                  </div>

                  <div>
                    <label className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1 block">
                      Source
                    </label>
                    <p className="text-sm text-gray-900">Site Health</p>
                  </div>

                  <div>
                    <label className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1 block">
                      Action
                    </label>
                    <p className="text-sm text-gray-900">Fix</p>
                  </div>

                  <div>
                    <label className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1 block">
                      Description
                    </label>
                    <p className="text-sm text-gray-700 leading-relaxed">
                      {selectedOpportunity.description}
                    </p>
                  </div>

                  <div>
                    <label className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1 block">
                      Updated
                    </label>
                    <p className="text-sm text-gray-900">{new Date().toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}</p>
                  </div>
                </div>

                {/* Action Buttons */}
                <div className="space-y-2 pt-4 border-t border-gray-200">
                  <Button
                    className="w-full bg-white hover:bg-gray-50 text-gray-900 border-2 border-gray-200 hover:border-gray-300 font-semibold text-sm py-2.5 flex items-center justify-center gap-2"
                    onClick={() => toast.info('View Audit functionality coming soon')}
                  >
                    <ExternalLink className="w-4 h-4" />
                    View Audit
                  </Button>
                  <Button
                    className="w-full bg-gray-900 hover:bg-gray-800 text-white font-semibold text-sm py-2.5 flex items-center justify-center gap-2"
                    onClick={() => toast.success('Added to tasks!')}
                  >
                    <Plus className="w-4 h-4" />
                    Add to Tasks
                  </Button>
                </div>
              </div>
            </div>
          )}
        </div>

        {filteredOpportunities.length === 0 && (
          <div className="text-center py-10 bg-white rounded-xl border border-gray-200/50 shadow-xl shadow-gray-200/50">
            <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-gray-100 to-gray-50 flex items-center justify-center mx-auto mb-3">
              <Info className="w-6 h-6 text-gray-400" />
            </div>
            <p className="text-sm text-gray-500 font-medium">No opportunities found in this category.</p>
            <p className="text-xs text-gray-400 mt-1">Try selecting a different filter</p>
          </div>
        )}
      </div>
    </div>
  );
};

export default OpportunitiesFound;
