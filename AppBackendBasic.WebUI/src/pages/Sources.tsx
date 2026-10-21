import { useState, useEffect } from 'react';
import { useProject } from '../contexts/ProjectContext';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import {
  Search,
  TrendingUp,
  TrendingDown,
  BarChart3,
  X,
  AlertCircle
} from 'lucide-react';
import VisibilityFilterBar from '../components/filters/VisibilityFilterBar';
import citationService, { TopDomain, CitationDetails } from '../services/citationService';
import { sourcesService, SourcesAnalytics } from '../services/sourcesService';
import { toast } from '../utils/toast';
import { LoadingPage } from '../components/ui/LoadingSpinner';

interface Citation {
  id: number;
  rank: number;
  domain: string;
  favicon: string;
  share: number;
  shareChange: number;
  globalShare: number;
  globalShareChange: number;
  owned: boolean;
  competitorType?: string;
  pages: CitationPage[];
}

interface CitationPage {
  id: number;
  url: string;
  title: string;
  share: number;
  shareChange: number;
  globalShare: number;
  globalShareChange: number;
  lastSeen: string;
}

const Sources = () => {
  const { selectedProject } = useProject();
  
  // Sources state (keeping only what's used)
  const [, setSourcesData] = useState<SourcesAnalytics | null>(null);
  
  // Citations state
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedPeriod, setSelectedPeriod] = useState('7days');
  const [dateRange, setDateRange] = useState({
    startDate: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000),
    endDate: new Date()
  });
  const [selectedCitation, setSelectedCitation] = useState<Citation | null>(null);
  const [citationDetails, setCitationDetails] = useState<CitationDetails | null>(null);
  const [activeTab, setActiveTab] = useState<'pages' | 'prompts' | 'topics' | 'platforms' | 'regions'>('pages');
  const [loadingDetails, setLoadingDetails] = useState(false);
  const [competitorType, setCompetitorType] = useState<'all' | 'direct' | 'serp'>('all');
  const [selectedPlatforms, setSelectedPlatforms] = useState<string[]>([]);
  const [selectedTopics, setSelectedTopics] = useState<string[]>([]);
  const [selectedLocations, setSelectedLocations] = useState<string[]>([]);
  const [showUnbranded, setShowUnbranded] = useState(false);
  const [topDomains, setTopDomains] = useState<TopDomain[]>([]);
  
  // Common state
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Fetch data from APIs
  useEffect(() => {
    if (!selectedProject?.id) {
      setLoading(false);
      return;
    }

    fetchAllData();
  }, [selectedProject?.id, selectedPeriod]);

  const fetchAllData = async () => {
    if (!selectedProject?.id) return;

    try {
      setLoading(true);
      setError(null);

      // Use dateRange from state
      const startDate = dateRange.startDate;
      const endDate = dateRange.endDate;

      // Fetch both sources and citations data
      const [sourcesAnalytics, domains] = await Promise.all([
        sourcesService.getSourcesAnalytics(selectedProject.id, {
          startDate: startDate.toISOString(),
          endDate: endDate.toISOString(),
        }),
        citationService.getTopCitedDomains(selectedProject.id, 10, startDate, endDate)
      ]);

      console.log('Sources Analytics:', sourcesAnalytics);
      console.log('Top Domains received:', domains);
      console.log('Top Domains count:', domains?.length);

      setSourcesData(sourcesAnalytics);
      setTopDomains(domains);
    } catch (err: any) {
      console.error('Error fetching data:', err);
      setError(err.message || 'Failed to load data');
      toast.error('Failed to load sources and citations');
    } finally {
      setLoading(false);
    }
  };

  const fetchCitationDetails = async (domain: string) => {
    if (!selectedProject?.id) return;

    try {
      setLoadingDetails(true);
      const details = await citationService.getCitationDetailsByDomain(
        selectedProject.id,
        domain,
        dateRange.startDate,
        dateRange.endDate
      );
      setCitationDetails(details);
    } catch (err: any) {
      console.error('Error fetching citation details:', err);
      toast.error('Failed to load citation details');
    } finally {
      setLoadingDetails(false);
    }
  };

  const handleCitationClick = (citation: Citation) => {
    setSelectedCitation(citation);
    setActiveTab('pages');
    fetchCitationDetails(citation.domain);
  };

  // Convert TopDomain data to Citation format for display
  const citations: Citation[] = topDomains
    .filter(domain => domain.domain) // Filter out entries with null/undefined domain
    .map((domain, index) => ({
      id: index + 1,
      rank: index + 1,
      domain: domain.domain,
      favicon: '🌐',
      share: domain.citationCount,
      shareChange: 0,
      globalShare: domain.citationCount,
      globalShareChange: 0,
      owned: !domain.isCompetitor,
      competitorType: domain.competitorType,
      pages: []
    }));

  console.log('Citations after mapping:', citations);
  console.log('Citations count:', citations.length);

  const filteredCitations = citations.filter(citation => {
    const matchesSearch = citation.domain?.toLowerCase().includes(searchQuery.toLowerCase()) ?? false;
    
    // Filter by unbranded toggle (opposite of includeOwnBrand)
    if (showUnbranded && citation.owned) return false;
    
    // Filter by competitor type (only applies to competitors, not own brand)
    if (!citation.owned) {
      if (competitorType === 'direct' && citation.competitorType !== 'Direct') return false;
      if (competitorType === 'serp' && citation.competitorType !== 'SERP') return false;
    }
    
    return matchesSearch;
  });

  const totalCitationShare = citations.reduce((sum, c) => sum + c.share, 0);
  const averageCitationShare = totalCitationShare / citations.length;
  const citationRank = 6;

  const getTrendIcon = (change: number) => {
    if (change > 0) return <TrendingUp className="w-3 h-3 text-green-600" />;
    if (change < 0) return <TrendingDown className="w-3 h-3 text-red-600" />;
    return null;
  };

  const getTrendColor = (change: number) => {
    if (change > 0) return 'text-green-600';
    if (change < 0) return 'text-red-600';
    return 'text-gray-500';
  };

  const formatChange = (change: number) => {
    if (change === 0) return '–';
    return change > 0 ? `+${change.toFixed(1)}%` : `${change.toFixed(1)}%`;
  };

  // Loading state
  if (loading) {
    return <LoadingPage text="Loading sources and citations..." />;
  }

  // Error state
  if (error) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-gray-50 via-blue-50/20 to-gray-50 flex items-center justify-center">
        <div className="text-center max-w-md">
          <AlertCircle className="w-12 h-12 text-red-600 mx-auto mb-4" />
          <h2 className="text-xl font-bold text-gray-900 mb-2">Failed to Load Data</h2>
          <p className="text-gray-600 mb-4">{error}</p>
          <Button onClick={fetchAllData}>Try Again</Button>
        </div>
      </div>
    );
  }

  // No project selected
  if (!selectedProject) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-gray-50 via-blue-50/20 to-gray-50 flex items-center justify-center">
        <div className="text-center max-w-md">
          <AlertCircle className="w-12 h-12 text-gray-400 mx-auto mb-4" />
          <h2 className="text-xl font-bold text-gray-900 mb-2">No Project Selected</h2>
          <p className="text-gray-600">Please select a project to view sources and citations.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-50 via-blue-50/20 to-gray-50">
      <div className="max-w-7xl mx-auto px-6 py-6">
        {/* Header */}
        <div className="mb-6">
          <div className="flex items-center justify-between mb-4">
            <div>
              <h1 className="text-2xl font-bold text-gray-900 mb-1">Sources</h1>
              <p className="text-sm text-gray-600">
                See the sources that answer engines are citing to generate responses
              </p>
            </div>
            
          </div>

          {/* Filters Bar */}
          <VisibilityFilterBar
            selectedPeriod={selectedPeriod}
            onPeriodChange={setSelectedPeriod}
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
            onRefresh={fetchAllData}
            showAutoRefresh={true}
          />

          {/* Citation Share and Rank Cards */}
          <div className="grid grid-cols-2 gap-4 mb-4">
            {/* Citation Share Card */}
            <div className="bg-white rounded-xl border border-gray-200/50 shadow-lg shadow-gray-200/50 p-5">
              <div className="flex items-center justify-between mb-3">
                <div>
                  <h3 className="text-sm font-semibold text-gray-600 mb-1">Citation Share</h3>
                  <p className="text-xs text-gray-500">How often your domain is cited by AI-generated answers</p>
                </div>
              </div>
              <div className="flex items-baseline gap-2 mb-4">
                <span className="text-3xl font-bold text-gray-900">{averageCitationShare.toFixed(1)}%</span>
                <div className="flex items-center gap-1">
                  {getTrendIcon(0.1)}
                  <span className={`text-sm font-medium ${getTrendColor(0.1)}`}>
                    +0.1%
                  </span>
                </div>
              </div>
              {/* Mini Chart Placeholder */}
              <div className="h-32 bg-gradient-to-br from-blue-50 to-indigo-50 rounded-lg flex items-center justify-center">
                <BarChart3 className="w-8 h-8 text-blue-400" />
              </div>
            </div>

            {/* Citation Rank Card */}
            <div className="bg-white rounded-xl border border-gray-200/50 shadow-lg shadow-gray-200/50 p-5">
              <div className="flex items-center justify-between mb-3">
                <div>
                  <h3 className="text-sm font-semibold text-gray-600 mb-1">Citation Rank</h3>
                  <p className="text-xs text-gray-500">Your ranking among cited domains</p>
                </div>
              </div>
              <div className="flex items-baseline gap-2 mb-4">
                <span className="text-3xl font-bold text-gray-900">#{citationRank}</span>
                <span className="text-sm text-gray-500">–</span>
              </div>
              {/* Top Domains List */}
              <div className="space-y-2">
                {filteredCitations.slice(0, 3).map((citation) => (
                  <div key={citation.id} className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <span className="text-lg">{citation.favicon}</span>
                      <span className="text-xs font-medium text-gray-700">{citation.domain}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <span className="text-xs font-bold text-gray-900">{citation.share}%</span>
                      <div className="flex items-center gap-1">
                        {getTrendIcon(citation.shareChange)}
                        <span className={`text-xs ${getTrendColor(citation.shareChange)}`}>
                          {formatChange(citation.shareChange)}
                        </span>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Sources Insights & Opportunities */}
          <div className="bg-white rounded-xl border border-gray-200/50 shadow-lg shadow-gray-200/50 p-6 mb-4">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-semibold text-gray-900">Sources Insights & Opportunities</h2>
              <button className="text-gray-400 hover:text-gray-600">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                </svg>
              </button>
            </div>

            <p className="text-sm text-gray-600 mb-4">Data-driven observations about Sources data</p>

            <div className="grid grid-cols-2 gap-6">
              {/* Insights */}
              <div>
                <h3 className="text-sm font-semibold text-gray-900 mb-3 flex items-center gap-2">
                  Insights <span className="text-gray-400">ⓘ</span>
                </h3>
                <div className="space-y-3">
                  <div className="flex items-start gap-3">
                    <div className="w-6 h-6 rounded-full bg-red-100 flex items-center justify-center flex-shrink-0 mt-0.5">
                      <span className="text-red-600 text-sm">📺</span>
                    </div>
                    <div className="flex-1">
                      <p className="text-sm text-gray-700">
                        <span className="font-medium">youtube.com</span> is the most popular social media source — appears in <span className="font-medium">23%</span> of responses with <span className="font-medium">2 cited URLs</span>
                      </p>
                    </div>
                  </div>

                  <div className="flex items-start gap-3">
                    <div className="w-6 h-6 rounded-full bg-orange-100 flex items-center justify-center flex-shrink-0 mt-0.5">
                      <span className="text-orange-600 text-sm">🔗</span>
                    </div>
                    <div className="flex-1">
                      <p className="text-sm text-gray-700">
                        <span className="font-medium">envato/tutorials.com</span> is the most cited editorial party — appears in <span className="font-medium">23%</span> of responses
                      </p>
                    </div>
                  </div>

                  <div className="flex items-start gap-3">
                    <div className="w-6 h-6 rounded-full bg-gray-100 flex items-center justify-center flex-shrink-0 mt-0.5">
                      <span className="text-gray-600 text-sm">📄</span>
                    </div>
                    <div className="flex-1">
                      <p className="text-sm text-gray-700">
                        <span className="font-medium">biz-ninty.URL</span> are being cited <span className="font-medium">96%</span> of all sources
                      </p>
                    </div>
                  </div>
                </div>
              </div>

              {/* Opportunities */}
              <div>
                <h3 className="text-sm font-semibold text-gray-900 mb-3 flex items-center gap-2">
                  Opportunities <span className="text-gray-400">ⓘ</span>
                </h3>
                <div className="text-sm text-gray-600">
                  <p>Recommendations to boost the chances of your pages being sourced by AI engines</p>
                  <p className="mt-2">No opportunities available yet. Check back after more data is collected.</p>
                </div>
              </div>
            </div>
          </div>

          {/* Citations Table */}
          <div className="bg-white rounded-xl border border-gray-200/50 shadow-lg shadow-gray-200/50 overflow-hidden">
            {/* Table Header */}
            <div className="bg-gray-50 border-b border-gray-200 px-6 py-3">
              <div className="flex items-center justify-between">
                <h3 className="text-sm font-bold text-gray-900">Top Citations</h3>
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
                  <Input
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    placeholder="Search domains..."
                    className="pl-10 h-8 w-64 text-sm border-2 border-gray-200 focus:border-gray-900 focus:ring-2 focus:ring-gray-900/20 rounded-lg"
                  />
                </div>
              </div>
            </div>

            {/* Column Headers */}
            <div className="bg-gray-50 border-b border-gray-200 px-6 py-3">
              <div className="grid grid-cols-12 gap-4 text-xs font-semibold text-gray-600 uppercase tracking-wide">
                <div className="col-span-3">Domain</div>
                <div className="col-span-2">Models</div>
                <div className="col-span-2">Usage %</div>
                <div className="col-span-2">Avg Citations</div>
                <div className="col-span-2">Popular Article Types</div>
                <div className="col-span-1">Source Types</div>
              </div>
            </div>

            {/* Citation Rows */}
            <div className="divide-y divide-gray-100">
              {filteredCitations.length > 0 ? (
                filteredCitations.map((citation, index) => (
                  <div
                    key={citation.id}
                    onClick={() => handleCitationClick(citation)}
                    className="px-6 py-4 hover:bg-gray-50 transition-colors cursor-pointer group"
                  >
                    <div className="grid grid-cols-12 gap-4 items-center">
                      {/* Domain */}
                      <div className="col-span-3">
                        <div className="flex items-center gap-3">
                          <div className="w-6 h-6 rounded bg-gray-100 flex items-center justify-center text-sm font-bold text-gray-700">
                            {citation.domain.charAt(0).toUpperCase()}
                          </div>
                          <div className="flex-1">
                            <div className="flex items-center gap-2">
                              <span className="text-sm font-medium text-gray-900">{citation.domain}</span>
                              {citation.owned && (
                                <span className="px-1.5 py-0.5 bg-blue-100 text-blue-700 rounded text-[10px] font-semibold">
                                  Owned
                                </span>
                              )}
                            </div>
                            <div className="text-xs text-gray-500">{(citation.share * 100 / (totalCitationShare || 1)).toFixed(0)}% of sources</div>
                          </div>
                        </div>
                      </div>

                      {/* Models */}
                      <div className="col-span-2">
                        <div className="flex items-center gap-1">
                          <div className="w-5 h-5 rounded-full bg-green-500 flex items-center justify-center">
                            <span className="text-white text-[10px] font-bold">AI</span>
                          </div>
                          <span className="text-sm text-gray-900">{citation.share > 10 ? '23.3%' : '20.0%'}</span>
                        </div>
                      </div>

                      {/* Usage % */}
                      <div className="col-span-2">
                        <div className="flex items-center gap-2">
                          <span className="text-sm font-semibold text-gray-900">{(citation.share * 100 / (totalCitationShare || 1)).toFixed(1)}%</span>
                          <div className="flex items-center gap-1">
                            {citation.shareChange !== 0 && getTrendIcon(citation.shareChange)}
                          </div>
                        </div>
                      </div>

                      {/* Avg Citations */}
                      <div className="col-span-2">
                        <span className="text-sm font-medium text-gray-900">{citation.share.toFixed(1)}</span>
                      </div>

                      {/* Popular Article Types */}
                      <div className="col-span-2">
                        <div className="flex flex-wrap gap-1">
                          {index % 3 === 0 && <span className="px-1.5 py-0.5 bg-gray-100 text-gray-700 rounded text-[10px] font-medium">Editorial</span>}
                          {index % 3 === 1 && <span className="px-1.5 py-0.5 bg-gray-100 text-gray-700 rounded text-[10px] font-medium">Review</span>}
                          {index % 3 === 2 && <span className="px-1.5 py-0.5 bg-gray-100 text-gray-700 rounded text-[10px] font-medium">Social</span>}
                        </div>
                      </div>

                      {/* Source Types */}
                      <div className="col-span-1">
                        <div className="flex items-center gap-1">
                          <div className="w-2 h-2 rounded-full bg-purple-500"></div>
                          <div className="w-2 h-2 rounded-full bg-blue-500"></div>
                          <div className="w-2 h-2 rounded-full bg-orange-500"></div>
                        </div>
                      </div>
                    </div>
                  </div>
                ))
              ) : (
                <div className="px-6 py-12 text-center">
                  <div className="w-16 h-16 rounded-xl bg-gray-100 flex items-center justify-center mx-auto mb-4">
                    <BarChart3 className="w-8 h-8 text-gray-400" />
                  </div>
                  <h3 className="text-lg font-bold text-gray-900 mb-2">No Citations Data</h3>
                  <p className="text-sm text-gray-600 mb-4">
                    No citation data available for the selected period.
                  </p>
                </div>
              )}
            </div>
          </div>

        </div>

        {/* Right Side Panel - Citation Details */}
        {selectedCitation && (
          <div className="fixed inset-y-0 right-0 w-[600px] bg-white border-l border-gray-200 shadow-2xl z-50 flex flex-col">
            {/* Panel Header */}
            <div className="bg-gradient-to-r from-gray-50 to-white border-b border-gray-200 px-6 py-4">
              <div className="flex items-center justify-between mb-3">
                <div className="flex items-center gap-3">
                  <div className="w-10 h-10 rounded-lg bg-gray-100 flex items-center justify-center text-xl">
                    {selectedCitation.favicon}
                  </div>
                  <div>
                    <h2 className="text-lg font-bold text-gray-900">{selectedCitation.domain}</h2>
                    <p className="text-xs text-gray-500">Rank #{citationDetails?.rank || selectedCitation.rank}</p>
                  </div>
                </div>
                <button
                  onClick={() => {
                    setSelectedCitation(null);
                    setCitationDetails(null);
                  }}
                  className="w-8 h-8 rounded-lg hover:bg-gray-100 flex items-center justify-center transition-colors"
                >
                  <X className="w-5 h-5 text-gray-600" />
                </button>
              </div>

              {/* Period Selectors */}
              <div className="flex items-center gap-2 text-xs mb-4">
                <select
                  value={selectedPeriod}
                  onChange={(e) => setSelectedPeriod(e.target.value)}
                  className="px-2 py-1 border border-gray-200 rounded text-xs bg-white"
                >
                  <option value="7days">Last 7 Days</option>
                  <option value="30days">Last 30 Days</option>
                </select>
                <span className="text-gray-500">vs.</span>
                <select className="px-2 py-1 border border-gray-200 rounded text-xs bg-white">
                  <option>Previous Period</option>
                </select>
                <div className="ml-auto flex items-center gap-2">
                  <button className="px-2 py-1 text-xs text-gray-600 hover:text-gray-900">Share</button>
                  <button className="px-2 py-1 text-xs text-gray-600 hover:text-gray-900">Count</button>
                </div>
              </div>

              {/* Citation Share Chart */}
              {citationDetails && (
                <div className="mb-4">
                  <div className="flex items-baseline gap-2 mb-2">
                    <span className="text-2xl font-bold text-gray-900">{citationDetails.citationShare.toFixed(1)}%</span>
                    <span className={`text-sm font-medium ${citationDetails.citationShareChange >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                      {citationDetails.citationShareChange >= 0 ? '+' : ''}{citationDetails.citationShareChange.toFixed(1)}%
                    </span>
                  </div>
                  <div className="h-24 bg-gray-50 rounded-lg flex items-end justify-between px-2 py-2">
                    {citationDetails.shareHistory.slice(-7).map((point, i) => (
                      <div key={i} className="flex-1 flex flex-col items-center justify-end mx-0.5">
                        <div 
                          className="w-full bg-blue-500 rounded-t"
                          style={{ height: `${(point.share / Math.max(...citationDetails.shareHistory.map(p => p.share))) * 100}%` }}
                        ></div>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>

            {/* Tabs */}
            <div className="border-b border-gray-200 px-6">
              <div className="flex gap-6">
                {(['pages', 'prompts', 'topics', 'platforms', 'regions'] as const).map((tab) => (
                  <button
                    key={tab}
                    onClick={() => setActiveTab(tab)}
                    className={`py-3 text-sm font-medium border-b-2 transition-colors ${
                      activeTab === tab
                        ? 'border-blue-600 text-blue-600'
                        : 'border-transparent text-gray-600 hover:text-gray-900'
                    }`}
                  >
                    {tab.charAt(0).toUpperCase() + tab.slice(1)}
                  </button>
                ))}
              </div>
            </div>

            {/* Panel Content */}
            <div className="flex-1 overflow-y-auto">
              {loadingDetails ? (
                <div className="flex items-center justify-center h-64">
                  <div className="text-center">
                    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-2"></div>
                    <p className="text-sm text-gray-500">Loading details...</p>
                  </div>
                </div>
              ) : citationDetails ? (
                <div className="p-6">
                  {/* Pages Tab */}
                  {activeTab === 'pages' && (
                    <div className="space-y-3">
                      <div className="flex items-center justify-between mb-4">
                        <h3 className="text-sm font-semibold text-gray-900">Page URLs</h3>
                        <span className="text-xs text-gray-500">{citationDetails.pages.length} pages</span>
                      </div>
                      <div className="overflow-x-auto">
                        <table className="w-full text-xs">
                          <thead className="bg-gray-50 border-b border-gray-200">
                            <tr>
                              <th className="px-3 py-2 text-left font-medium text-gray-600">Rank</th>
                              <th className="px-3 py-2 text-left font-medium text-gray-600">Page URL</th>
                              <th className="px-3 py-2 text-right font-medium text-gray-600">Share</th>
                              <th className="px-3 py-2 text-right font-medium text-gray-600">Global Share</th>
                            </tr>
                          </thead>
                          <tbody className="divide-y divide-gray-100">
                            {citationDetails.pages.map((page) => (
                              <tr key={page.rank} className="hover:bg-gray-50">
                                <td className="px-3 py-3 text-gray-900">{page.rank}</td>
                                <td className="px-3 py-3">
                                  <div className="max-w-xs">
                                    <p className="text-gray-900 font-medium truncate">{page.title || page.url}</p>
                                    <p className="text-gray-500 truncate text-[10px]">{page.url}</p>
                                  </div>
                                </td>
                                <td className="px-3 py-3 text-right">
                                  <div className="flex items-center justify-end gap-1">
                                    <span className="font-semibold text-gray-900">{page.share.toFixed(1)}%</span>
                                    <span className={`text-[10px] ${page.shareChange >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                                      {page.shareChange >= 0 ? '+' : ''}{page.shareChange.toFixed(1)}%
                                    </span>
                                  </div>
                                </td>
                                <td className="px-3 py-3 text-right">
                                  <div className="flex items-center justify-end gap-1">
                                    <span className="font-medium text-gray-900">{page.globalShare.toFixed(2)}%</span>
                                    <span className={`text-[10px] ${page.globalShareChange >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                                      {page.globalShareChange >= 0 ? '+' : ''}{page.globalShareChange.toFixed(2)}%
                                    </span>
                                  </div>
                                </td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    </div>
                  )}

                  {/* Prompts Tab */}
                  {activeTab === 'prompts' && (
                    <div className="space-y-3">
                      <div className="flex items-center justify-between mb-4">
                        <h3 className="text-sm font-semibold text-gray-900">Prompts</h3>
                        <span className="text-xs text-gray-500">{citationDetails.prompts.length} prompts</span>
                      </div>
                      {citationDetails.prompts.map((prompt) => (
                        <div key={prompt.id} className="bg-gray-50 rounded-lg p-4 hover:bg-gray-100 transition-colors">
                          <p className="text-sm text-gray-900 mb-2">{prompt.promptText}</p>
                          <div className="flex items-center gap-4 text-xs text-gray-600">
                            <span>{prompt.citationCount} citations</span>
                            <span>Avg position: {prompt.averagePosition.toFixed(1)}</span>
                            <span className="ml-auto">{new Date(prompt.lastSeen).toLocaleDateString()}</span>
                          </div>
                        </div>
                      ))}
                      {citationDetails.prompts.length === 0 && (
                        <p className="text-sm text-gray-500 text-center py-8">No prompt data available</p>
                      )}
                    </div>
                  )}

                  {/* Topics Tab */}
                  {activeTab === 'topics' && (
                    <div className="space-y-3">
                      <div className="flex items-center justify-between mb-4">
                        <h3 className="text-sm font-semibold text-gray-900">Topics</h3>
                        <span className="text-xs text-gray-500">{citationDetails.topics.length} topics</span>
                      </div>
                      {citationDetails.topics.map((topic, i) => (
                        <div key={i} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg hover:bg-gray-100 transition-colors">
                          <div className="flex-1">
                            <p className="text-sm font-medium text-gray-900">{topic.topic}</p>
                            <p className="text-xs text-gray-500">{topic.citationCount} citations</p>
                          </div>
                          <div className="text-right">
                            <p className="text-sm font-bold text-gray-900">{topic.share.toFixed(1)}%</p>
                          </div>
                        </div>
                      ))}
                      {citationDetails.topics.length === 0 && (
                        <p className="text-sm text-gray-500 text-center py-8">No topic data available</p>
                      )}
                    </div>
                  )}

                  {/* Platforms Tab */}
                  {activeTab === 'platforms' && (
                    <div className="space-y-3">
                      <div className="flex items-center justify-between mb-4">
                        <h3 className="text-sm font-semibold text-gray-900">Platforms</h3>
                        <span className="text-xs text-gray-500">{citationDetails.platforms.length} platforms</span>
                      </div>
                      {citationDetails.platforms.map((platform, i) => (
                        <div key={i} className="flex items-center justify-between p-4 bg-gray-50 rounded-lg hover:bg-gray-100 transition-colors">
                          <div className="flex-1">
                            <p className="text-sm font-medium text-gray-900">{platform.platform}</p>
                            <p className="text-xs text-gray-500">{platform.citationCount} citations • Avg pos: {platform.averagePosition.toFixed(1)}</p>
                          </div>
                          <div className="text-right">
                            <p className="text-sm font-bold text-gray-900">{platform.share.toFixed(1)}%</p>
                          </div>
                        </div>
                      ))}
                      {citationDetails.platforms.length === 0 && (
                        <p className="text-sm text-gray-500 text-center py-8">No platform data available</p>
                      )}
                    </div>
                  )}

                  {/* Regions Tab */}
                  {activeTab === 'regions' && (
                    <div className="space-y-3">
                      <div className="flex items-center justify-between mb-4">
                        <h3 className="text-sm font-semibold text-gray-900">Regions</h3>
                        <span className="text-xs text-gray-500">{citationDetails.regions.length} regions</span>
                      </div>
                      {citationDetails.regions.map((region, i) => (
                        <div key={i} className="flex items-center justify-between p-4 bg-gray-50 rounded-lg hover:bg-gray-100 transition-colors">
                          <div className="flex-1">
                            <p className="text-sm font-medium text-gray-900">{region.region}</p>
                            <p className="text-xs text-gray-500">{region.citationCount} citations</p>
                          </div>
                          <div className="text-right">
                            <p className="text-sm font-bold text-gray-900">{region.share.toFixed(1)}%</p>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              ) : (
                <div className="flex items-center justify-center h-64">
                  <p className="text-sm text-gray-500">No data available</p>
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  )
};

export default Sources;
