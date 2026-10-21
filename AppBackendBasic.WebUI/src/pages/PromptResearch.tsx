import { useState, useEffect, useRef } from 'react';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { promptService } from '../services/promptService';
import { 
  Search, 
  TrendingUp, 
  TrendingDown, 
  Filter,
  Download,
  ChevronDown,
  BarChart3,
  Target,
  Eye,
  ArrowUpDown
} from 'lucide-react';

interface Prompt {
  id: number;
  text: string;
  topic: string;
  visibilityRank: number;
  visibilityScore: number;
  visibilityChange: number;
  averagePosition: number;
  positionChange: number;
  citationShare: number;
  citationChange: number;
}

interface TopicGroup {
  name: string;
  promptCount: number;
  prompts: Prompt[];
}

const PromptResearch = () => {
  // const navigate = useNavigate();
  // const { selectedProject } = useProject();
  
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedPeriod, setSelectedPeriod] = useState('Last 7 Days');
  const [groupBy] = useState('topic');
  const [showPlatformsDropdown, setShowPlatformsDropdown] = useState(false);
  const [showTopicsDropdown, setShowTopicsDropdown] = useState(false);
  const [selectedPlatforms, setSelectedPlatforms] = useState<string[]>(['All']);
  const [selectedTopics, setSelectedTopics] = useState<string[]>(['All']);
  const [sortBy, setSortBy] = useState<'visibilityRank' | 'visibilityScore' | 'averagePosition' | 'citationShare'>('visibilityScore');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');
  const [viewAllTopic, setViewAllTopic] = useState<TopicGroup | null>(null);
  const [, setLoading] = useState(true);
  const [topicGroups, setTopicGroups] = useState<TopicGroup[]>([]);
  
  const platformsRef = useRef<HTMLDivElement>(null);
  const topicsRef = useRef<HTMLDivElement>(null);

  // Fetch research data from API
  useEffect(() => {
    const fetchResearchData = async () => {
      setLoading(true);
      try {
        const topicFilter = selectedTopics.includes('All') || selectedTopics.length === 0 
          ? undefined 
          : selectedTopics[0];
        
        const data = await promptService.getResearchData(topicFilter, 200);
        
        // Transform API data to match UI format
        const transformedGroups: TopicGroup[] = data.map(group => ({
          name: group.topic,
          promptCount: group.promptCount,
          prompts: group.prompts.map((p, index) => ({
            id: p.id,
            text: p.text,
            topic: group.topic,
            visibilityRank: index + 1,
            visibilityScore: Math.round((p.relevanceScore || 0) * 100),
            visibilityChange: Math.random() * 10 - 5, // Mock change data
            averagePosition: 1 + Math.random() * 3,
            positionChange: Math.random() * 0.5 - 0.25,
            citationShare: Math.random() * 2,
            citationChange: Math.random() * 5 - 2.5
          }))
        }));
        
        setTopicGroups(transformedGroups);
      } catch (error) {
        console.error('Error fetching research data:', error);
        // Fallback to empty state
        setTopicGroups([]);
      } finally {
        setLoading(false);
      }
    };

    fetchResearchData();
  }, [selectedTopics]);

  // Close dropdowns when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (platformsRef.current && !platformsRef.current.contains(event.target as Node)) {
        setShowPlatformsDropdown(false);
      }
      if (topicsRef.current && !topicsRef.current.contains(event.target as Node)) {
        setShowTopicsDropdown(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const filteredGroups = topicGroups.filter(group => {
    const matchesSearch = group.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
                         group.prompts.some(p => p.text.toLowerCase().includes(searchQuery.toLowerCase()));
    
    const matchesTopic = selectedTopics.includes('All') || selectedTopics.includes(group.name);
    
    return matchesSearch && matchesTopic;
  }).map(group => ({
    ...group,
    prompts: group.prompts.sort((a, b) => {
      const aVal = a[sortBy];
      const bVal = b[sortBy];
      return sortOrder === 'asc' ? aVal - bVal : bVal - aVal;
    })
  }));

  const getTrendIcon = (change: number) => {
    if (change > 0) return <TrendingUp className="w-3 h-3 text-green-600" />;
    if (change < 0) return <TrendingDown className="w-3 h-3 text-red-600" />;
    return <span className="w-3 h-3 inline-block">–</span>;
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

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-50 via-blue-50/20 to-gray-50">
      <div className="max-w-7xl mx-auto px-6 py-6">
        {/* Header */}
        <div className="mb-6">
          <div className="flex items-center justify-between mb-4">
            <div>
              <h1 className="text-2xl font-bold text-gray-900 mb-1">Prompt Research</h1>
              <p className="text-sm text-gray-600">
                Track and analyze prompt performance across AI platforms
              </p>
            </div>
            <div className="flex items-center gap-2">
              <Button
                size="sm"
                className="bg-white border border-gray-300 text-gray-700 hover:bg-gray-50 shadow-sm"
              >
                <Download className="w-4 h-4 mr-2" />
                Export 6.2k answers
              </Button>
            </div>
          </div>

          {/* Stats Bar */}
          <div className="bg-white rounded-xl border border-gray-200/50 shadow-lg shadow-gray-200/50 p-4 mb-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-6">
                <div>
                  <div className="text-2xl font-bold text-gray-900">110</div>
                  <div className="text-xs text-gray-500">Total Prompts</div>
                </div>
                <div className="h-10 w-px bg-gray-200" />
                <div className="flex items-center gap-2">
                  <div className="w-8 h-8 rounded-lg bg-blue-100 flex items-center justify-center">
                    <Eye className="w-4 h-4 text-blue-600" />
                  </div>
                  <div>
                    <div className="text-sm font-bold text-gray-900">Visibility</div>
                    <div className="text-xs text-gray-500">Track rankings</div>
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  <div className="w-8 h-8 rounded-lg bg-purple-100 flex items-center justify-center">
                    <Target className="w-4 h-4 text-purple-600" />
                  </div>
                  <div>
                    <div className="text-sm font-bold text-gray-900">Prompts</div>
                    <div className="text-xs text-gray-500">AI tracking</div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          {/* Filters and Controls */}
          <div className="bg-white rounded-xl border border-gray-200/50 shadow-lg shadow-gray-200/50 p-4">
            <div className="flex items-center gap-3 flex-wrap">
              {/* Search */}
              <div className="flex-1 min-w-[300px]">
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
                  <Input
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    placeholder="Search Topics"
                    className="pl-10 h-9 text-sm border-2 border-gray-200 focus:border-gray-900 focus:ring-2 focus:ring-gray-900/20 rounded-lg"
                  />
                </div>
              </div>

              {/* Period Selector */}
              <select
                value={selectedPeriod}
                onChange={(e) => setSelectedPeriod(e.target.value)}
                className="px-3 py-2 text-xs font-medium border-2 border-gray-200 rounded-lg hover:border-gray-300 focus:border-gray-900 focus:ring-2 focus:ring-gray-900/20 bg-white"
              >
                <option>Last 7 Days</option>
                <option>Last 30 Days</option>
                <option>Last 90 Days</option>
                <option>Previous Period</option>
              </select>

              {/* Group By */}
              <button
                className="px-3 py-2 text-xs font-medium bg-gray-900 text-white rounded-lg hover:bg-gray-800 transition-all flex items-center gap-2"
              >
                <BarChart3 className="w-3.5 h-3.5" />
                Group by: {groupBy}
              </button>

              {/* Platforms Filter */}
              <div className="relative" ref={platformsRef}>
                <button
                  onClick={() => setShowPlatformsDropdown(!showPlatformsDropdown)}
                  className="px-3 py-2 text-xs font-medium border-2 border-gray-200 rounded-lg hover:border-gray-300 bg-white flex items-center gap-2"
                >
                  <Filter className="w-3.5 h-3.5" />
                  Platforms
                  <ChevronDown className="w-3.5 h-3.5" />
                </button>
                {showPlatformsDropdown && (
                  <div className="absolute top-full mt-2 right-0 w-48 bg-white rounded-lg border border-gray-200 shadow-xl z-50 py-2">
                    {['All', 'ChatGPT', 'Claude', 'Gemini', 'Perplexity'].map(platform => (
                      <button
                        key={platform}
                        onClick={() => {
                          if (platform === 'All') {
                            setSelectedPlatforms(['All']);
                          } else {
                            const newPlatforms = selectedPlatforms.includes('All')
                              ? [platform]
                              : selectedPlatforms.includes(platform)
                              ? selectedPlatforms.filter(p => p !== platform)
                              : [...selectedPlatforms, platform];
                            setSelectedPlatforms(newPlatforms.length === 0 ? ['All'] : newPlatforms);
                          }
                        }}
                        className="w-full px-4 py-2 text-left text-sm hover:bg-gray-50 flex items-center gap-2"
                      >
                        <div className={`w-4 h-4 rounded border-2 flex items-center justify-center ${
                          selectedPlatforms.includes(platform) ? 'bg-gray-900 border-gray-900' : 'border-gray-300'
                        }`}>
                          {selectedPlatforms.includes(platform) && (
                            <svg className="w-3 h-3 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                            </svg>
                          )}
                        </div>
                        {platform}
                      </button>
                    ))}
                  </div>
                )}
              </div>

              {/* Topics Filter */}
              <div className="relative" ref={topicsRef}>
                <button
                  onClick={() => setShowTopicsDropdown(!showTopicsDropdown)}
                  className="px-3 py-2 text-xs font-medium border-2 border-gray-200 rounded-lg hover:border-gray-300 bg-white flex items-center gap-2"
                >
                  # Topics
                  <ChevronDown className="w-3.5 h-3.5" />
                </button>
                {showTopicsDropdown && (
                  <div className="absolute top-full mt-2 right-0 w-64 bg-white rounded-lg border border-gray-200 shadow-xl z-50 py-2 max-h-80 overflow-y-auto">
                    {['All', ...topicGroups.map(g => g.name)].map(topic => (
                      <button
                        key={topic}
                        onClick={() => {
                          if (topic === 'All') {
                            setSelectedTopics(['All']);
                          } else {
                            const newTopics = selectedTopics.includes('All')
                              ? [topic]
                              : selectedTopics.includes(topic)
                              ? selectedTopics.filter(t => t !== topic)
                              : [...selectedTopics, topic];
                            setSelectedTopics(newTopics.length === 0 ? ['All'] : newTopics);
                          }
                        }}
                        className="w-full px-4 py-2 text-left text-sm hover:bg-gray-50 flex items-center gap-2"
                      >
                        <div className={`w-4 h-4 rounded border-2 flex items-center justify-center ${
                          selectedTopics.includes(topic) ? 'bg-gray-900 border-gray-900' : 'border-gray-300'
                        }`}>
                          {selectedTopics.includes(topic) && (
                            <svg className="w-3 h-3 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                            </svg>
                          )}
                        </div>
                        {topic}
                      </button>
                    ))}
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>

        {/* Prompts Table */}
        <div className="space-y-4">
          {filteredGroups.map((group) => (
            <div key={group.name} className="bg-white rounded-xl border border-gray-200/50 shadow-lg shadow-gray-200/50 overflow-hidden">
              {/* Topic Header */}
              <div className="bg-gradient-to-r from-gray-50 to-white border-b border-gray-200 px-6 py-3">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <div className="w-8 h-8 rounded-lg bg-gray-900 flex items-center justify-center">
                      <Target className="w-4 h-4 text-white" />
                    </div>
                    <div>
                      <h3 className="text-sm font-bold text-gray-900">{group.name}</h3>
                      <p className="text-xs text-gray-500">{group.promptCount} prompts</p>
                    </div>
                  </div>
                  <Button
                    size="sm"
                    onClick={() => setViewAllTopic(group)}
                    className="bg-gray-900 hover:bg-gray-800 text-white text-xs px-3 py-1.5"
                  >
                    View All
                  </Button>
                </div>
              </div>

              {/* Table Header */}
              <div className="bg-gray-50 border-b border-gray-200 px-6 py-2">
                <div className="grid grid-cols-12 gap-4 text-xs font-semibold text-gray-600">
                  <div className="col-span-5">Topic</div>
                  <div 
                    className="col-span-2 flex items-center gap-1 cursor-pointer hover:text-gray-900"
                    onClick={() => {
                      if (sortBy === 'visibilityRank') {
                        setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc');
                      } else {
                        setSortBy('visibilityRank');
                        setSortOrder('asc');
                      }
                    }}
                  >
                    Visibility Rank
                    <ArrowUpDown className={`w-3 h-3 ${sortBy === 'visibilityRank' ? 'text-gray-900' : ''}`} />
                  </div>
                  <div 
                    className="col-span-2 flex items-center gap-1 cursor-pointer hover:text-gray-900"
                    onClick={() => {
                      if (sortBy === 'visibilityScore') {
                        setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc');
                      } else {
                        setSortBy('visibilityScore');
                        setSortOrder('desc');
                      }
                    }}
                  >
                    Visibility Score
                    <ArrowUpDown className={`w-3 h-3 ${sortBy === 'visibilityScore' ? 'text-gray-900' : ''}`} />
                  </div>
                  <div 
                    className="col-span-2 flex items-center gap-1 cursor-pointer hover:text-gray-900"
                    onClick={() => {
                      if (sortBy === 'averagePosition') {
                        setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc');
                      } else {
                        setSortBy('averagePosition');
                        setSortOrder('asc');
                      }
                    }}
                  >
                    Average Position
                    <ArrowUpDown className={`w-3 h-3 ${sortBy === 'averagePosition' ? 'text-gray-900' : ''}`} />
                  </div>
                  <div 
                    className="col-span-1 flex items-center gap-1 cursor-pointer hover:text-gray-900"
                    onClick={() => {
                      if (sortBy === 'citationShare') {
                        setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc');
                      } else {
                        setSortBy('citationShare');
                        setSortOrder('desc');
                      }
                    }}
                  >
                    Citation Share
                    <ArrowUpDown className={`w-3 h-3 ${sortBy === 'citationShare' ? 'text-gray-900' : ''}`} />
                  </div>
                </div>
              </div>

              {/* Prompt Rows */}
              <div className="divide-y divide-gray-100">
                {group.prompts.map((prompt) => (
                  <div
                    key={prompt.id}
                    className="px-6 py-3 hover:bg-gray-50 transition-colors cursor-pointer group"
                  >
                    <div className="grid grid-cols-12 gap-4 items-center">
                      {/* Prompt Text */}
                      <div className="col-span-5">
                        <p className="text-sm text-gray-900 font-medium group-hover:text-gray-900">
                          {prompt.text}
                        </p>
                      </div>

                      {/* Visibility Rank */}
                      <div className="col-span-2">
                        <div className="flex items-center gap-2">
                          <span className="text-sm font-bold text-gray-900">#{prompt.visibilityRank}</span>
                          <span className="text-xs text-gray-500">–</span>
                        </div>
                      </div>

                      {/* Visibility Score */}
                      <div className="col-span-2">
                        <div className="flex items-center gap-2">
                          <span className="text-sm font-bold text-gray-900">{prompt.visibilityScore}%</span>
                          <div className="flex items-center gap-1">
                            {getTrendIcon(prompt.visibilityChange)}
                            <span className={`text-xs font-medium ${getTrendColor(prompt.visibilityChange)}`}>
                              {formatChange(prompt.visibilityChange)}
                            </span>
                          </div>
                        </div>
                      </div>

                      {/* Average Position */}
                      <div className="col-span-2">
                        <div className="flex items-center gap-2">
                          <span className="text-sm font-bold text-gray-900">{prompt.averagePosition}</span>
                          <div className="flex items-center gap-1">
                            {getTrendIcon(prompt.positionChange)}
                            <span className={`text-xs font-medium ${getTrendColor(prompt.positionChange)}`}>
                              {prompt.positionChange !== 0 ? prompt.positionChange.toFixed(1) : '–'}
                            </span>
                          </div>
                        </div>
                      </div>

                      {/* Citation Share */}
                      <div className="col-span-1">
                        <div className="flex items-center gap-2">
                          <span className="text-sm font-bold text-gray-900">{prompt.citationShare}%</span>
                          <div className="flex items-center gap-1">
                            {getTrendIcon(prompt.citationChange)}
                            <span className={`text-xs font-medium ${getTrendColor(prompt.citationChange)}`}>
                              {formatChange(prompt.citationChange)}
                            </span>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>

        {/* View All Modal */}
        {viewAllTopic && (
          <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center p-6">
            <div className="bg-white rounded-2xl shadow-2xl max-w-6xl w-full max-h-[90vh] overflow-hidden flex flex-col">
              {/* Modal Header */}
              <div className="bg-gradient-to-r from-gray-50 to-white border-b border-gray-200 px-6 py-4">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 rounded-lg bg-gray-900 flex items-center justify-center">
                      <Target className="w-5 h-5 text-white" />
                    </div>
                    <div>
                      <h2 className="text-xl font-bold text-gray-900">{viewAllTopic.name}</h2>
                      <p className="text-sm text-gray-500">{viewAllTopic.promptCount} prompts</p>
                    </div>
                  </div>
                  <button
                    onClick={() => setViewAllTopic(null)}
                    className="w-8 h-8 rounded-lg hover:bg-gray-100 flex items-center justify-center transition-colors"
                  >
                    <svg className="w-5 h-5 text-gray-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </div>
              </div>

              {/* Modal Content */}
              <div className="flex-1 overflow-y-auto">
                <div className="p-6">
                  {/* Table Header */}
                  <div className="bg-gray-50 rounded-lg px-4 py-3 mb-3">
                    <div className="grid grid-cols-12 gap-4 text-xs font-semibold text-gray-600">
                      <div className="col-span-5">Topic</div>
                      <div className="col-span-2">Visibility Rank</div>
                      <div className="col-span-2">Visibility Score</div>
                      <div className="col-span-2">Average Position</div>
                      <div className="col-span-1">Citation Share</div>
                    </div>
                  </div>

                  {/* Prompt Rows */}
                  <div className="space-y-2">
                    {viewAllTopic.prompts.map((prompt) => (
                      <div
                        key={prompt.id}
                        className="bg-white border border-gray-200 rounded-lg px-4 py-3 hover:shadow-md transition-all"
                      >
                        <div className="grid grid-cols-12 gap-4 items-center">
                          {/* Prompt Text */}
                          <div className="col-span-5">
                            <p className="text-sm text-gray-900 font-medium">
                              {prompt.text}
                            </p>
                          </div>

                          {/* Visibility Rank */}
                          <div className="col-span-2">
                            <div className="flex items-center gap-2">
                              <span className="text-sm font-bold text-gray-900">#{prompt.visibilityRank}</span>
                              <span className="text-xs text-gray-500">–</span>
                            </div>
                          </div>

                          {/* Visibility Score */}
                          <div className="col-span-2">
                            <div className="flex items-center gap-2">
                              <span className="text-sm font-bold text-gray-900">{prompt.visibilityScore}%</span>
                              <div className="flex items-center gap-1">
                                {getTrendIcon(prompt.visibilityChange)}
                                <span className={`text-xs font-medium ${getTrendColor(prompt.visibilityChange)}`}>
                                  {formatChange(prompt.visibilityChange)}
                                </span>
                              </div>
                            </div>
                          </div>

                          {/* Average Position */}
                          <div className="col-span-2">
                            <div className="flex items-center gap-2">
                              <span className="text-sm font-bold text-gray-900">{prompt.averagePosition}</span>
                              <div className="flex items-center gap-1">
                                {getTrendIcon(prompt.positionChange)}
                                <span className={`text-xs font-medium ${getTrendColor(prompt.positionChange)}`}>
                                  {prompt.positionChange !== 0 ? prompt.positionChange.toFixed(1) : '–'}
                                </span>
                              </div>
                            </div>
                          </div>

                          {/* Citation Share */}
                          <div className="col-span-1">
                            <div className="flex items-center gap-2">
                              <span className="text-sm font-bold text-gray-900">{prompt.citationShare}%</span>
                              <div className="flex items-center gap-1">
                                {getTrendIcon(prompt.citationChange)}
                                <span className={`text-xs font-medium ${getTrendColor(prompt.citationChange)}`}>
                                  {formatChange(prompt.citationChange)}
                                </span>
                              </div>
                            </div>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              </div>

              {/* Modal Footer */}
              <div className="border-t border-gray-200 px-6 py-4 bg-gray-50">
                <div className="flex items-center justify-end gap-3">
                  <Button
                    onClick={() => setViewAllTopic(null)}
                    className="bg-white border border-gray-300 text-gray-700 hover:bg-gray-50"
                  >
                    Close
                  </Button>
                  <Button
                    className="bg-gray-900 hover:bg-gray-800 text-white"
                  >
                    <Download className="w-4 h-4 mr-2" />
                    Export Data
                  </Button>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Empty State */}
        {filteredGroups.length === 0 && (
          <div className="bg-white rounded-xl border border-gray-200/50 shadow-lg shadow-gray-200/50 p-12 text-center">
            <div className="w-16 h-16 rounded-xl bg-gray-100 flex items-center justify-center mx-auto mb-4">
              <Search className="w-8 h-8 text-gray-400" />
            </div>
            <h3 className="text-lg font-bold text-gray-900 mb-2">No prompts found</h3>
            <p className="text-sm text-gray-600">
              Try adjusting your search or filters
            </p>
          </div>
        )}
      </div>
    </div>
  );
};

export default PromptResearch;
