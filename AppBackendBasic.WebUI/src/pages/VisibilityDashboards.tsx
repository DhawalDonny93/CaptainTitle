import { useState, useRef, useEffect } from 'react';
import { useProject } from '../contexts/ProjectContext';
import { ChevronDown, HelpCircle, TrendingUp, Check, AlertCircle } from 'lucide-react';
import VisibilityFilterBar from '../components/filters/VisibilityFilterBar';
import visibilityService, { DashboardOverview, CompetitiveAnalysis } from '../services/visibilityService';
import { toast } from '../utils/toast';
import { LoadingPage } from '../components/ui/LoadingSpinner';

interface VisibilityDashboardsProps {
  projectId?: number;
}

const VisibilityDashboards = ({ projectId: _propProjectId }: VisibilityDashboardsProps) => {
  const { selectedProject } = useProject();

  const [period, setPeriod] = useState('7days');
  const [dateRange, setDateRange] = useState({
    startDate: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000),
    endDate: new Date()
  });
  const [selectedTopics, setSelectedTopics] = useState<string[]>([]);
  const [selectedPlatforms, setSelectedPlatforms] = useState<string[]>([]);
  const [selectedLocations, setSelectedLocations] = useState<string[]>([]);
  const [chartConfig, setChartConfig] = useState('Default');
  const [competitorType, setCompetitorType] = useState<'all' | 'direct' | 'serp'>('all');
  const [showUnbranded, setShowUnbranded] = useState(false);
  
  const [openDropdown, setOpenDropdown] = useState<string | null>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [overview, setOverview] = useState<DashboardOverview | null>(null);
  const [competitive, setCompetitive] = useState<CompetitiveAnalysis | null>(null);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setOpenDropdown(null);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  useEffect(() => {
    if (selectedProject?.id) {
      fetchVisibilityData();
    } else {
      setLoading(false);
    }
  }, [selectedProject?.id]);

  const fetchVisibilityData = async () => {
    if (!selectedProject?.id) return;

    try {
      setLoading(true);
      setError(null);

      const [overviewData, competitiveData] = await Promise.all([
        visibilityService.getDashboardOverview(selectedProject.id),
        visibilityService.getCompetitiveAnalysis(selectedProject.id),
      ]);

      setOverview(overviewData);
      setCompetitive(competitiveData);
    } catch (err: any) {
      console.error('Failed to fetch visibility data:', err);
      setError(err.message || 'Failed to load visibility data');
      toast.error('Failed to load visibility dashboard');
    } finally {
      setLoading(false);
    }
  };

  const chartConfigOptions = ['Default', 'Show Competitors', 'Show Trend Lines', 'Show Annotations'];

  const toggleDropdown = (dropdown: string) => {
    setOpenDropdown(openDropdown === dropdown ? null : dropdown);
  };


  const allRankingData = competitive ? [
    { rank: 1, brand: competitive.yourDomain || 'Unknown', type: 'Owned', competitorType: undefined, score: competitive.yourScore, change: 0, trend: 'up' },
    ...competitive.competitors.map((comp) => ({
      rank: comp.rank,
      brand: comp.domain || 'Unknown',
      type: 'Competitor',
      competitorType: comp.competitorType || 'Direct',
      score: comp.visibilityScore,
      change: 0,
      trend: 'up' as const
    }))
  ] : [];

  // Filter ranking data based on competitor type and unbranded toggle
  const filteredRankingData = allRankingData.filter(item => {
    // Filter by unbranded toggle (opposite of includeOwnBrand)
    if (showUnbranded && item.type === 'Owned') return false;
    
    // Filter by competitor type (only applies to competitors, not own brand)
    if (item.type === 'Competitor') {
      if (competitorType === 'direct' && item.competitorType !== 'Direct') return false;
      if (competitorType === 'serp' && item.competitorType !== 'SERP') return false;
    }
    
    return true;
  });

  // Recalculate rankings based on filtered data (sorted by score descending)
  const rankingData = filteredRankingData
    .sort((a, b) => b.score - a.score)
    .map((item, index) => ({
      ...item,
      rank: index + 1
    }));

  const chartData = overview?.platformBreakdown ? 
    overview.platformBreakdown.slice(0, 6).map((platform) => ({
      date: platform.platform.substring(0, 3),
      value: platform.visibilityScore
    })) : [];

  // Calculate visibility score based on filtered data
  // If own brand is filtered out (showUnbranded), use the top competitor's score
  // Otherwise use the own brand's score
  const ownBrandData = rankingData.find(item => item.type === 'Owned');
  const visibilityScore = (ownBrandData?.score ?? (rankingData.length > 0 ? rankingData[0].score : overview?.overallVisibilityScore)) ?? 0;
  const trendChange = overview?.trendDirection === 'up' ? '+0.7' : overview?.trendDirection === 'down' ? '-0.7' : '0';
  
  // Calculate current rank (1-based)
  const currentRank = ownBrandData?.rank || (rankingData.length > 0 ? 1 : null);

  if (loading) {
    return <LoadingPage />;
  }

  if (error) {
    return (
      <div className="text-center p-10">
        <AlertCircle className="h-12 w-12 text-red-600 mx-auto mb-4" />
        <h2 className="text-xl font-bold text-gray-900 mb-2">Failed to Load Dashboard</h2>
        <p className="text-gray-600 mb-4">{error}</p>
        <button onClick={fetchVisibilityData} className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700">Try Again</button>
      </div>
    );
  }

  if (!selectedProject) {
    return (
      <div className="text-center p-10">
        <AlertCircle className="h-12 w-12 text-gray-400 mx-auto mb-4" />
        <h2 className="text-xl font-bold text-gray-900 mb-2">No Project Selected</h2>
        <p className="text-gray-600">Please select a project to view visibility dashboard.</p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="border-b border-gray-200 pb-4">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Visibility Dashboard</h1>
            <p className="text-sm text-gray-600 mt-1">Track your brand's visibility across AI platforms</p>
          </div>
          <button className="px-3 py-1.5 text-sm font-medium text-blue-600 bg-blue-50 rounded-md">
            Beta
          </button>
        </div>
      </div>

      {/* Filters */}
      <VisibilityFilterBar
        selectedPeriod={period}
        onPeriodChange={setPeriod}
        dateRange={dateRange}
        onDateRangeChange={setDateRange}
        selectedPlatforms={selectedPlatforms}
        onPlatformsChange={setSelectedPlatforms}
        selectedTopics={selectedTopics}
        onTopicsChange={setSelectedTopics}
        selectedLocations={selectedLocations}
        onLocationsChange={setSelectedLocations}
        competitorFilter={competitorType}
        onCompetitorFilterChange={(filter) => setCompetitorType(filter as 'all' | 'direct' | 'serp')}
        showUnbranded={showUnbranded}
        onUnbrandedChange={setShowUnbranded}
        onRefresh={fetchVisibilityData}
        showAutoRefresh={true}
      />

      {/* Visibility Score Section */}
      <div>
        <div className="flex items-center justify-between mb-3">
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-lg font-semibold text-gray-900">Visibility Score</h2>
              <HelpCircle className="w-4 h-4 text-gray-400" />
            </div>
            <p className="text-sm text-gray-600 mt-0.5">How often {selectedProject?.name || 'No Project'} appears in AI-generated answers</p>
          </div>
          <div className="relative">
            <button 
              onClick={() => toggleDropdown('chartConfig')}
              className="hidden px-2.5 py-1.5 text-sm border border-gray-300 rounded-md hover:bg-gray-50 flex items-center gap-1.5"
            >
              <span className="text-gray-700">Chart Config</span>
              <ChevronDown className="w-3.5 h-3.5 text-gray-500" />
            </button>
            {openDropdown === 'chartConfig' && (
              <div className="absolute top-full mt-1 right-0 bg-white border border-gray-200 rounded-md shadow-lg z-50 min-w-[180px]">
                {chartConfigOptions.map(option => (
                  <button
                    key={option}
                    onClick={() => { setChartConfig(option); setOpenDropdown(null); }}
                    className="w-full px-2.5 py-1.5 text-sm text-left hover:bg-gray-50 flex items-center justify-between"
                  >
                    <span className={chartConfig === option ? 'font-medium text-gray-900' : 'text-gray-700'}>{option}</span>
                    {chartConfig === option && <Check className="w-3.5 h-3.5 text-gray-900" />}
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
          {/* Score Card */}
          <div className="bg-white border border-gray-200 rounded-lg p-4 shadow-sm">
            <div className="flex items-center gap-2 mb-2">
              <span className="text-sm text-gray-600">Visibility Score</span>
              <HelpCircle className="w-4 h-4 text-gray-400" />
            </div>
            <div className="flex items-baseline gap-2 mb-1">
              <span className="text-4xl font-bold text-gray-900">{visibilityScore.toFixed(1)}%</span>
              {overview?.trendDirection && (
                <span className={`text-sm font-medium flex items-center gap-1 ${
                  overview.trendDirection === 'up' ? 'text-green-600' : 
                  overview.trendDirection === 'down' ? 'text-red-600' : 'text-gray-600'
                }`}>
                  <TrendingUp className="w-4 h-4" />
                  {trendChange}%
                </span>
              )}
              <HelpCircle className="w-4 h-4 text-gray-400" />
            </div>
            <div className="text-sm text-gray-500 mb-4">{overview?.averagePosition ? `Avg Position: ${overview.averagePosition.toFixed(1)}` : 'N/A'}</div>

            {/* Mini Chart */}
            <div className="h-32 flex items-end gap-1">
              {chartData.length > 0 ? chartData.map((point, i) => (
                <div key={i} className="flex-1 flex flex-col items-center gap-1">
                  <div className="w-full bg-blue-500 rounded-t" style={{ height: `${point.value}%` }} />
                  <span className="text-xs text-gray-500">{point.date}</span>
                </div>
              )) : (
                <div className="flex-1 flex items-center justify-center text-gray-400 text-sm">
                  No data available
                </div>
              )}
            </div>
            {chartData.length > 0 && (
              <div className="flex items-center justify-between mt-4 text-xs text-gray-500">
                <span>{chartData[0]?.date || ''}</span>
                <span>{chartData[chartData.length - 1]?.date || ''}</span>
              </div>
            )}
            <div className="flex items-center gap-3 mt-3 pt-3 border-t border-gray-200">
              <div className="flex items-center gap-2">
                <div className="w-3 h-3 bg-blue-500 rounded-full" />
                <span className="text-sm text-gray-600">Current Period</span>
              </div>
              <div className="flex items-center gap-2">
                <div className="w-3 h-3 bg-gray-300 rounded-full" />
                <span className="text-sm text-gray-600">Previous Period</span>
              </div>
            </div>
            <button className="w-full mt-3 text-sm text-gray-600 hover:text-gray-900 font-medium">
              Compare competitors
            </button>
          </div>

          {/* Ranking Card */}
          <div className="bg-white border border-gray-200 rounded-lg p-4 shadow-sm">
            <div className="flex items-center gap-2 mb-4">
              <span className="text-sm text-gray-600">Visibility Score Rank</span>
              <HelpCircle className="w-4 h-4 text-gray-400" />
            </div>
            <div className="mb-4">
              <div className="text-4xl font-bold text-gray-900 mb-1">#{currentRank !== null ? currentRank : 'N/A'}</div>
              <div className="w-6 h-1 bg-gray-900 rounded" />
            </div>

            {/* Ranking Table */}
            <div className="space-y-2">
              <div className="flex items-center justify-between text-xs text-gray-500 font-medium pb-1.5 border-b border-gray-200">
                <span className="w-12"></span>
                <span className="flex-1">Asset</span>
                <span className="w-24 text-right">Visibility Score</span>
              </div>
              {rankingData.map((item) => (
                <div key={item.rank} className="flex items-center justify-between py-1.5 hover:bg-gray-50 rounded px-2 -mx-2">
                  <span className="w-12 text-sm text-gray-600">{item.rank}.</span>
                  <div className="flex-1 flex items-center gap-2">
                    <div className="w-5 h-5 bg-black rounded flex items-center justify-center text-white text-xs font-bold">
                      {item.brand.charAt(0)}
                    </div>
                    <span className="text-sm font-medium text-gray-900">{item.brand}</span>
                    <span className="text-xs text-gray-500">{item.type}</span>
                  </div>
                  <div className="w-24 text-right flex items-center justify-end gap-2">
                    <span className="text-sm font-medium text-gray-900">{item.score}%</span>
                    <span className={`text-xs font-medium flex items-center ${
                      item.trend === 'up' ? 'text-green-600' : 'text-red-600'
                    }`}>
                      {item.trend === 'up' ? '+' : ''}{item.change}%
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>

      {/* Share of Voice Section */}
      <div className="bg-white border border-gray-200 rounded-lg p-4 shadow-sm">
        <div className="flex items-center gap-2 mb-2">
          <h2 className="text-lg font-semibold text-gray-900">Share of Voice</h2>
          <HelpCircle className="w-4 h-4 text-gray-400" />
        </div>
        <p className="text-sm text-gray-600 mb-4">Mentions of {selectedProject?.name || 'No Project'} in AI-generated answers in relation to competitors</p>
        <div className="h-56 flex flex-col items-center justify-center">
          <div className="w-16 h-16 rounded-xl bg-gray-100 flex items-center justify-center mb-4">
            <svg className="w-8 h-8 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
            </svg>
          </div>
          <h3 className="text-lg font-bold text-gray-900 mb-2">No Share of Voice Data</h3>
          <p className="text-sm text-gray-600 mb-2">Brand mention data is not available yet.</p>
          <p className="text-xs text-gray-500">
            Run the seed script: <code className="bg-gray-100 px-2 py-1 rounded">Database_Migrations/SEED_CITATIONS_MENTIONS_DATA.sql</code>
          </p>
        </div>
      </div>
    </div>
  );
};

export default VisibilityDashboards;
