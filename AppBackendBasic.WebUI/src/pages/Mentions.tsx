import { useState, useEffect } from 'react';
import { useProject } from '../contexts/ProjectContext';
import { Search, Filter, Download } from 'lucide-react';
import { mentionsService, MentionsAnalytics } from '../services/mentionsService';
import VisibilityFilterBar from '../components/filters/VisibilityFilterBar';

const Mentions = () => {
  const { selectedProject } = useProject();
  const [timeRange, setTimeRange] = useState('7days');
  const [dateRange, setDateRange] = useState({
    startDate: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000),
    endDate: new Date()
  });
  const [selectedPlatforms, setSelectedPlatforms] = useState<string[]>([]);
  const [selectedTopics, setSelectedTopics] = useState<string[]>([]);
  const [selectedLocations, setSelectedLocations] = useState<string[]>([]);
  const [competitorFilter, setCompetitorFilter] = useState('all');
  const [showUnbranded, setShowUnbranded] = useState(false);
  const [activeTab, setActiveTab] = useState('all');
  const [performanceTab, setPerformanceTab] = useState('overtime');
  const [searchQuery, setSearchQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [data, setData] = useState<MentionsAnalytics | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Filter mentions based on activeTab, searchQuery, and unbranded toggle
  const filteredMentions = data?.mentions.filter((mention: any) => {
    // Filter by search query
    if (searchQuery && !mention.prompt.toLowerCase().includes(searchQuery.toLowerCase())) {
      return false;
    }

    // Filter by unbranded toggle
    // When showUnbranded is true, exclude own brand data (isCompetitor = false)
    if (showUnbranded && mention.isCompetitor === false) {
      return false;
    }

    // Filter by active tab
    if (activeTab === 'mentioned' && !mention.mentioned) {
      return false;
    }
    if (activeTab === 'not-mentioned' && mention.mentioned) {
      return false;
    }
    if (activeTab === 'citations' && mention.citations === 0) {
      return false;
    }

    return true;
  }) || [];

  useEffect(() => {
    const fetchData = async () => {
      if (!selectedProject?.id) {
        setLoading(false);
        return;
      }

      setLoading(true);
      setError(null);

      try {
        const filters: any = {};
        
        if (selectedPlatforms.length > 0) filters.platform = selectedPlatforms[0];
        if (selectedTopics.length > 0) filters.topic = selectedTopics[0];
        
        filters.startDate = dateRange.startDate.toISOString();
        filters.endDate = dateRange.endDate.toISOString();

        const analyticsData = await mentionsService.getMentionsAnalytics(selectedProject.id, filters);
        setData(analyticsData);
      } catch (err: any) {
        console.error('Error fetching mentions analytics:', err);
        setError(err.message || 'Failed to load mentions analytics');
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [selectedProject, timeRange, selectedPlatforms, selectedTopics, dateRange]);

  const getPositionColor = (percentage: number) => {
    if (percentage >= 67) return 'bg-red-600';
    if (percentage >= 33) return 'bg-orange-500';
    if (percentage > 0) return 'bg-orange-300';
    return 'bg-gray-100';
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <p className="text-red-600 font-medium mb-2">Error loading mentions analytics</p>
          <p className="text-gray-600 text-sm">{error}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header with Filters */}
      <VisibilityFilterBar
        selectedPeriod={timeRange}
        onPeriodChange={setTimeRange}
        dateRange={dateRange}
        onDateRangeChange={setDateRange}
        selectedPlatforms={selectedPlatforms}
        onPlatformsChange={setSelectedPlatforms}
        selectedTopics={selectedTopics}
        onTopicsChange={setSelectedTopics}
        selectedLocations={selectedLocations}
        onLocationsChange={setSelectedLocations}
        competitorFilter={competitorFilter}
        onCompetitorFilterChange={setCompetitorFilter}
        showUnbranded={showUnbranded}
        onUnbrandedChange={setShowUnbranded}
        showAutoRefresh={true}
      />

      {/* Insights & Opportunities */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900">Mentions Insights & Opportunities</h2>
          <button className="text-gray-400 hover:text-gray-600">
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
            </svg>
          </button>
        </div>

        <p className="text-sm text-gray-600 mb-4">Key findings about your brand mentions data</p>

        <div className="grid grid-cols-2 gap-6">
          {/* Insights */}
          <div>
            <h3 className="text-sm font-semibold text-gray-900 mb-3 flex items-center gap-2">
              Insights <span className="text-gray-400">ⓘ</span>
            </h3>
            <div className="space-y-3">
              <div className="flex items-start gap-3">
                <div className="w-6 h-6 rounded-full bg-blue-100 flex items-center justify-center flex-shrink-0 mt-0.5">
                  <span className="text-blue-600 text-sm">💬</span>
                </div>
                <div className="flex-1">
                  <p className="text-sm text-gray-700">
                    Zepto <span className="font-medium">outranks</span> you on <span className="font-medium">ChatGPT</span> with <span className="font-medium">4 more mentions</span> (5 vs. 1). This platform may have their content seen or sourced.
                  </p>
                </div>
              </div>

              <div className="flex items-start gap-3">
                <div className="w-6 h-6 rounded-full bg-orange-100 flex items-center justify-center flex-shrink-0 mt-0.5">
                  <span className="text-orange-600 text-sm">📊</span>
                </div>
                <div className="flex-1">
                  <p className="text-sm text-gray-700">
                    You have <span className="font-medium">6 mentions</span> but no citations. AI engines mention your brand but aren't linking to your content seen as sources.
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
              <p>Recommendations to boost your brand visibility and mentions</p>
              <p className="mt-2">No opportunities available yet. Check back after more data is collected.</p>
            </div>
          </div>
        </div>

        {/* Quick Stats */}
        <div className="grid grid-cols-3 gap-4 mt-6 pt-6 border-t border-gray-200">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-lg bg-blue-50 flex items-center justify-center">
              <span className="text-2xl">💬</span>
            </div>
            <div>
              <div className="text-2xl font-bold text-gray-900">{data?.insights.totalMentions || 0}</div>
              <div className="text-sm text-gray-600">Total Mentions</div>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-lg bg-green-50 flex items-center justify-center">
              <span className="text-2xl">📊</span>
            </div>
            <div>
              <div className="text-2xl font-bold text-gray-900">{data?.insights.totalCitations || 0}</div>
              <div className="text-sm text-gray-600">Citations</div>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-lg bg-green-50 flex items-center justify-center">
              <span className="text-green-600 text-xl">✓</span>
            </div>
            <div>
              <div className="text-sm font-medium text-gray-900">{data?.insights.topPlatform || 'N/A'}</div>
              <div className="text-sm text-gray-600">Most Platform</div>
            </div>
          </div>
        </div>
      </div>

      {/* Mentions Analytics */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Mentions Analytics</h2>
        <p className="text-sm text-gray-600 mb-6">Track your brand's mentions and sentiment across AI platforms</p>

        <div className="flex items-center gap-4 mb-6">
          <button
            onClick={() => setPerformanceTab('overtime')}
            className={`px-4 py-2 text-sm font-medium rounded-lg ${
              performanceTab === 'overtime'
                ? 'bg-gray-900 text-white'
                : 'text-gray-600 hover:bg-gray-100'
            }`}
          >
            Performance over time
          </button>
          <button
            onClick={() => setPerformanceTab('platform')}
            className={`px-4 py-2 text-sm font-medium rounded-lg ${
              performanceTab === 'platform'
                ? 'bg-gray-900 text-white'
                : 'text-gray-600 hover:bg-gray-100'
            }`}
          >
            Performance by platform
          </button>
        </div>

        <div className="grid grid-cols-2 gap-6">
          {/* Mentions Chart */}
          <div>
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-sm font-semibold text-gray-900 flex items-center gap-2">
                Mentions <span className="text-gray-400">ⓘ</span>
              </h3>
              <button className="text-sm text-gray-600 hover:text-gray-900 flex items-center gap-1">
                <Download className="w-4 h-4" />
                No change from yesterday
              </button>
            </div>

            <div className="h-64 flex items-end justify-between gap-2 border-b border-l border-gray-200 pb-2 pl-2">
              {data?.analytics.performanceOverTime.dates.map((date: string, index: number) => (
                <div key={date} className="flex-1 flex flex-col items-center gap-1">
                  <div className="w-full flex flex-col gap-0.5" style={{ height: '200px', justifyContent: 'flex-end' }}>
                    {data.analytics.performanceOverTime.brandData['You'] && data.analytics.performanceOverTime.brandData['You'][index] > 0 && (
                      <div
                        className="w-full bg-red-400 rounded-t"
                        style={{ height: `${(data.analytics.performanceOverTime.brandData['You'][index] / 15) * 100}%` }}
                      />
                    )}
                    {Object.keys(data.analytics.performanceOverTime.brandData).slice(1).map((brandKey: string, brandIndex: number) => {
                      const brandData = data.analytics.performanceOverTime.brandData[brandKey];
                      if (!brandData || brandData[index] === 0) return null;
                      const colors = ['bg-orange-400', 'bg-green-400', 'bg-yellow-400', 'bg-purple-400'];
                      return (
                        <div
                          key={brandKey}
                          className={`w-full ${colors[brandIndex % colors.length]} rounded-t`}
                          style={{ height: `${(brandData[index] / 15) * 100}%` }}
                        />
                      );
                    })}
                  </div>
                  <span className="text-xs text-gray-600">{date}</span>
                </div>
              ))}
            </div>

            <div className="flex items-center gap-4 mt-4 text-xs flex-wrap">
              {data?.analytics.performanceOverTime.brandData && Object.keys(data.analytics.performanceOverTime.brandData).map((brandKey: string, idx: number) => {
                const colors = ['bg-red-400', 'bg-orange-400', 'bg-green-400', 'bg-yellow-400', 'bg-purple-400'];
                return (
                  <div key={brandKey} className="flex items-center gap-2">
                    <div className={`w-3 h-3 ${colors[idx % colors.length]} rounded`}></div>
                    <span className="text-gray-600">{brandKey}</span>
                  </div>
                );
              })}
            </div>
          </div>

          {/* Position Distribution */}
          <div>
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-sm font-semibold text-gray-900 flex items-center gap-2">
                Position Distribution <span className="text-gray-400">ⓘ</span>
              </h3>
              <div className="flex items-center gap-2 text-xs text-gray-600">
                <span>Less Frequent</span>
                <div className="flex gap-1">
                  <div className="w-3 h-3 bg-orange-100 rounded"></div>
                  <div className="w-3 h-3 bg-orange-300 rounded"></div>
                  <div className="w-3 h-3 bg-orange-500 rounded"></div>
                  <div className="w-3 h-3 bg-red-600 rounded"></div>
                </div>
                <span>More Frequent</span>
              </div>
            </div>

            <div className="space-y-2">
              <div className="grid grid-cols-6 gap-2 text-xs text-gray-600 font-medium mb-2">
                <div className="col-span-1">Brand</div>
                <div className="text-center">1st</div>
                <div className="text-center">2</div>
                <div className="text-center">3</div>
                <div className="text-center">4</div>
                <div className="text-center">5+</div>
              </div>

              {data?.analytics.positionDistribution.map((item: any) => (
                <div key={item.brand} className="grid grid-cols-6 gap-2 items-center">
                  <div className="col-span-1 flex items-center gap-2">
                    <div className="w-5 h-5 rounded-full bg-purple-500 flex items-center justify-center text-white text-xs">
                      {item.brand.charAt(0)}
                    </div>
                    <span className="text-sm text-gray-700 truncate">{item.brand}</span>
                  </div>
                  {['1', '2', '3', '4', '5+'].map((pos) => (
                    <div key={pos} className="flex justify-center">
                      <div
                        className={`w-8 h-8 rounded flex items-center justify-center text-xs font-medium ${
                          item.positions[pos] > 0
                            ? `${getPositionColor(item.positions[pos])} text-white`
                            : 'bg-gray-100 text-gray-400'
                        }`}
                      >
                        {item.positions[pos] > 0 ? `${item.positions[pos]}%` : '-'}
                      </div>
                    </div>
                  ))}
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>

      {/* Topics */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-sm font-semibold text-gray-900 flex items-center gap-2">
            Topics <span className="text-gray-400">ⓘ</span>
          </h3>
          <button className="text-sm text-blue-600 hover:text-blue-700">Manage Topics</button>
        </div>

        <p className="text-sm text-gray-600 mb-4">Performance by topic across AI platforms</p>

        <div className="flex items-center gap-2 mb-4">
          <button className="px-3 py-1.5 text-sm bg-gray-100 text-gray-900 font-medium rounded-lg">
            All (8)
          </button>
          <button className="px-3 py-1.5 text-sm text-gray-600 hover:bg-gray-50 rounded-lg">
            Strong (0)
          </button>
          <button className="px-3 py-1.5 text-sm text-gray-600 hover:bg-gray-50 rounded-lg">
            Average (0)
          </button>
          <button className="px-3 py-1.5 text-sm text-gray-600 hover:bg-gray-50 rounded-lg">
            Weak (0)
          </button>
          <button className="px-3 py-1.5 text-sm text-gray-600 hover:bg-gray-50 rounded-lg">
            Missing (0)
          </button>
        </div>

        <div className="space-y-2">
          {data?.topics.map((topic: any) => (
            <div key={topic.name} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
              <div className="flex items-center gap-3">
                <span className="text-sm font-medium text-gray-900">{topic.name}</span>
                <span className="px-2 py-0.5 text-xs bg-orange-100 text-orange-700 rounded">
                  {topic.status}
                </span>
              </div>
              <div className="flex items-center gap-4">
                <span className="text-sm text-gray-600">{topic.count} prompts</span>
                <span className="px-3 py-1 text-sm bg-green-500 text-white rounded-lg">20%</span>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* All Mentions & Citations Table */}
      <div className="bg-white rounded-lg border border-gray-200">
        <div className="p-6 border-b border-gray-200">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">All Mentions & Citations</h2>
          <p className="text-sm text-gray-600 mb-4">All prompts where your brand is mentioned across AI platforms</p>

          <div className="flex items-center gap-4 mb-4">
            <button
              onClick={() => setActiveTab('all')}
              className={`px-4 py-2 text-sm font-medium rounded-lg ${
                activeTab === 'all'
                  ? 'bg-gray-900 text-white'
                  : 'text-gray-600 hover:bg-gray-100'
              }`}
            >
              All Mentions
            </button>
            <button
              onClick={() => setActiveTab('mentioned')}
              className={`px-4 py-2 text-sm font-medium rounded-lg ${
                activeTab === 'mentioned'
                  ? 'bg-gray-900 text-white'
                  : 'text-gray-600 hover:bg-gray-100'
              }`}
            >
              Mentioned
            </button>
            <button
              onClick={() => setActiveTab('not-mentioned')}
              className={`px-4 py-2 text-sm font-medium rounded-lg ${
                activeTab === 'not-mentioned'
                  ? 'bg-gray-900 text-white'
                  : 'text-gray-600 hover:bg-gray-100'
              }`}
            >
              Not Mentioned
            </button>
            <button
              onClick={() => setActiveTab('citations')}
              className={`px-4 py-2 text-sm font-medium rounded-lg ${
                activeTab === 'citations'
                  ? 'bg-gray-900 text-white'
                  : 'text-gray-600 hover:bg-gray-100'
              }`}
            >
              With Citations
            </button>
            <button className="ml-auto text-sm text-gray-600 hover:text-gray-900 flex items-center gap-2">
              <Filter className="w-4 h-4" />
              Gap analysis
            </button>
          </div>

          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
            <input
              type="text"
              placeholder="Search prompts..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-10 pr-4 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Prompt
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Mentioned On
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Locations
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Mentions
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Citations
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Avg Position
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Competitors
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {filteredMentions.length > 0 ? (
                filteredMentions.map((mention: any, index: number) => (
                <tr key={index} className="hover:bg-gray-50">
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-2">
                      <button className="text-gray-400 hover:text-gray-600">
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                        </svg>
                      </button>
                      <span className="text-sm text-gray-900">{mention.prompt}</span>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex items-center gap-2">
                      {mention.mentioned ? (
                        <div className="w-8 h-8 rounded-full bg-green-500 flex items-center justify-center text-white text-xs">
                          ✓
                        </div>
                      ) : (
                        <div className="w-8 h-8 rounded-full bg-gray-200 flex items-center justify-center text-gray-400 text-xs">
                          —
                        </div>
                      )}
                      <span className="text-sm text-gray-600">{mention.mentioned ? '100%' : '0%'}</span>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                    {mention.locations}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {mention.mentions}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {mention.citations}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {mention.avgPosition || '—'}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex items-center gap-1">
                      {mention.competitors.length > 0 ? (
                        mention.competitors.map((comp: any, i: number) => (
                          <span
                            key={i}
                            className="px-2 py-1 text-xs rounded"
                            style={{ backgroundColor: i === 0 ? '#ef4444' : i === 1 ? '#f59e0b' : '#3b82f6', color: 'white' }}
                          >
                            {typeof comp === 'object' ? comp.name : comp}
                          </span>
                        ))
                      ) : (
                        <span className="text-sm text-gray-400">—</span>
                      )}
                    </div>
                  </td>
                </tr>
              ))
              ) : (
                <tr>
                  <td colSpan={7} className="px-6 py-12 text-center">
                    <div className="flex flex-col items-center">
                      <div className="w-16 h-16 rounded-xl bg-gray-100 flex items-center justify-center mb-4">
                        <svg className="w-8 h-8 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z" />
                        </svg>
                      </div>
                      <h3 className="text-lg font-bold text-gray-900 mb-2">No Mentions Data</h3>
                      <p className="text-sm text-gray-600 mb-2">
                        No brand mentions found for the selected filters.
                      </p>
                    </div>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export default Mentions;
