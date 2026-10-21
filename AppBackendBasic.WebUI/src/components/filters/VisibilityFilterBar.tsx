import { useState, useRef, useEffect } from 'react';
import { Calendar, ChevronDown, RefreshCw, Sparkles } from 'lucide-react';

interface DateRange {
  startDate: Date;
  endDate: Date;
}

interface VisibilityFilterBarProps {
  selectedPeriod: string;
  onPeriodChange: (period: string) => void;
  dateRange: DateRange;
  onDateRangeChange: (range: DateRange) => void;
  selectedPlatforms: string[];
  onPlatformsChange: (platforms: string[]) => void;
  selectedTopics: string[];
  onTopicsChange: (topics: string[]) => void;
  selectedLocations: string[];
  onLocationsChange: (locations: string[]) => void;
  competitorFilter: string;
  onCompetitorFilterChange: (filter: string) => void;
  showUnbranded: boolean;
  onUnbrandedChange: (show: boolean) => void;
  onRefresh?: () => void;
  showAutoRefresh?: boolean;
}

const VisibilityFilterBar = ({
  selectedPeriod,
  onPeriodChange,
  dateRange,
  onDateRangeChange,
  selectedPlatforms,
  onPlatformsChange,
  selectedTopics,
  onTopicsChange,
  selectedLocations,
  onLocationsChange,
  competitorFilter,
  onCompetitorFilterChange,
  showUnbranded,
  onUnbrandedChange,
  onRefresh,
  showAutoRefresh = true
}: VisibilityFilterBarProps) => {
  const [showDatePicker, setShowDatePicker] = useState(false);
  const [showPlatforms, setShowPlatforms] = useState(false);
  const [showTopics, setShowTopics] = useState(false);
  const [showLocations, setShowLocations] = useState(false);
  const [showCompetitors, setShowCompetitors] = useState(false);
  
  const datePickerRef = useRef<HTMLDivElement>(null);
  const platformsRef = useRef<HTMLDivElement>(null);
  const topicsRef = useRef<HTMLDivElement>(null);
  const locationsRef = useRef<HTMLDivElement>(null);
  const competitorsRef = useRef<HTMLDivElement>(null);

  const platforms = ['ChatGPT', 'Perplexity', 'Claude', 'Gemini', 'SearchGPT'];
  const topics = ['SEO', 'Content Marketing', 'AI Tools', 'Analytics', 'E-commerce'];
  const locations = ['United States', 'United Kingdom', 'Canada', 'Australia', 'India'];

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (datePickerRef.current && !datePickerRef.current.contains(event.target as Node)) {
        setShowDatePicker(false);
      }
      if (platformsRef.current && !platformsRef.current.contains(event.target as Node)) {
        setShowPlatforms(false);
      }
      if (topicsRef.current && !topicsRef.current.contains(event.target as Node)) {
        setShowTopics(false);
      }
      if (locationsRef.current && !locationsRef.current.contains(event.target as Node)) {
        setShowLocations(false);
      }
      if (competitorsRef.current && !competitorsRef.current.contains(event.target as Node)) {
        setShowCompetitors(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const quickPeriods = [
    { label: '7 days', value: '7days' },
    { label: '30 days', value: '30days' },
    { label: '90 days', value: '90days' },
    { label: 'Last 365 days', value: '365days' }
  ];

  const togglePlatform = (platform: string) => {
    if (selectedPlatforms.includes(platform)) {
      onPlatformsChange(selectedPlatforms.filter(p => p !== platform));
    } else {
      onPlatformsChange([...selectedPlatforms, platform]);
    }
  };

  const toggleTopic = (topic: string) => {
    if (selectedTopics.includes(topic)) {
      onTopicsChange(selectedTopics.filter(t => t !== topic));
    } else {
      onTopicsChange([...selectedTopics, topic]);
    }
  };

  const toggleLocation = (location: string) => {
    if (selectedLocations.includes(location)) {
      onLocationsChange(selectedLocations.filter(l => l !== location));
    } else {
      onLocationsChange([...selectedLocations, location]);
    }
  };

  return (
    <div className="bg-white border-b border-gray-200 px-6 py-3">
      <div className="flex items-center justify-between gap-3">
        {/* Left Side Filters */}
        <div className="flex items-center gap-2">
          {/* Date Range Picker */}
          <div className="relative" ref={datePickerRef}>
            <button
              onClick={() => setShowDatePicker(!showDatePicker)}
              className="flex items-center gap-2 px-3 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50 bg-white"
            >
              <Calendar className="w-4 h-4 text-gray-500" />
              <span className="text-gray-700">{selectedPeriod === 'custom' ? 'Custom Range' : selectedPeriod.replace('days', ' days')}</span>
              <ChevronDown className="w-4 h-4 text-gray-500" />
            </button>

            {showDatePicker && (
              <div className="absolute top-full mt-2 left-0 bg-white border border-gray-200 rounded-lg shadow-xl z-50 p-4 w-96">
                {/* Quick Period Buttons */}
                <div className="flex gap-2 mb-4">
                  {quickPeriods.map(period => (
                    <button
                      key={period.value}
                      onClick={() => {
                        onPeriodChange(period.value);
                        const endDate = new Date();
                        const startDate = new Date();
                        const days = parseInt(period.value);
                        startDate.setDate(endDate.getDate() - days);
                        onDateRangeChange({ startDate, endDate });
                      }}
                      className={`px-3 py-1.5 text-sm rounded-lg ${
                        selectedPeriod === period.value
                          ? 'bg-gray-900 text-white'
                          : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                      }`}
                    >
                      {period.label}
                    </button>
                  ))}
                </div>

                {/* Date Inputs */}
                <div className="grid grid-cols-2 gap-4 mb-4">
                  <div>
                    <label className="block text-xs text-gray-600 mb-1">Start Date</label>
                    <input
                      type="date"
                      value={dateRange.startDate.toISOString().split('T')[0]}
                      onChange={(e) => {
                        const newStartDate = new Date(e.target.value);
                        onDateRangeChange({ ...dateRange, startDate: newStartDate });
                        onPeriodChange('custom');
                      }}
                      className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-gray-900"
                    />
                  </div>
                  <div>
                    <label className="block text-xs text-gray-600 mb-1">End Date</label>
                    <input
                      type="date"
                      value={dateRange.endDate.toISOString().split('T')[0]}
                      onChange={(e) => {
                        const newEndDate = new Date(e.target.value);
                        onDateRangeChange({ ...dateRange, endDate: newEndDate });
                        onPeriodChange('custom');
                      }}
                      className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-gray-900"
                    />
                  </div>
                </div>

                {/* Action Buttons */}
                <div className="flex justify-end gap-2">
                  <button
                    onClick={() => setShowDatePicker(false)}
                    className="px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 rounded-lg"
                  >
                    Clear
                  </button>
                  <button
                    onClick={() => setShowDatePicker(false)}
                    className="px-4 py-2 text-sm bg-gray-900 text-white rounded-lg hover:bg-gray-800"
                  >
                    Apply
                  </button>
                </div>
              </div>
            )}
          </div>

          {/* Platforms Filter */}
          <div className="relative" ref={platformsRef}>
            <button
              onClick={() => setShowPlatforms(!showPlatforms)}
              className="flex items-center gap-2 px-3 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50 bg-white"
            >
              <Sparkles className="w-4 h-4 text-gray-500" />
              <span className="text-gray-700">
                {selectedPlatforms.length === 0 ? 'All Platforms' : 
                 selectedPlatforms.length === 1 ? selectedPlatforms[0] :
                 `${selectedPlatforms.length} Platforms`}
              </span>
              <ChevronDown className="w-4 h-4 text-gray-500" />
            </button>

            {showPlatforms && (
              <div className="absolute top-full mt-2 left-0 bg-white border border-gray-200 rounded-lg shadow-xl z-50 min-w-[200px]">
                <div className="p-2">
                  {platforms.map(platform => (
                    <button
                      key={platform}
                      onClick={() => togglePlatform(platform)}
                      className="w-full flex items-center gap-2 px-3 py-2 text-sm hover:bg-gray-50 rounded-lg"
                    >
                      <div className={`w-4 h-4 border rounded flex items-center justify-center ${
                        selectedPlatforms.includes(platform) ? 'bg-gray-900 border-gray-900' : 'border-gray-300'
                      }`}>
                        {selectedPlatforms.includes(platform) && (
                          <svg className="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                          </svg>
                        )}
                      </div>
                      <span className="text-gray-700">{platform}</span>
                    </button>
                  ))}
                </div>
              </div>
            )}
          </div>

          {/* Topics Filter */}
          <div className="relative" ref={topicsRef}>
            <button
              onClick={() => setShowTopics(!showTopics)}
              className="flex items-center gap-2 px-3 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50 bg-white"
            >
              <svg className="w-4 h-4 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z" />
              </svg>
              <span className="text-gray-700">
                {selectedTopics.length === 0 ? 'All Topics' : 
                 selectedTopics.length === 1 ? selectedTopics[0] :
                 `${selectedTopics.length} Topics`}
              </span>
              <ChevronDown className="w-4 h-4 text-gray-500" />
            </button>

            {showTopics && (
              <div className="absolute top-full mt-2 left-0 bg-white border border-gray-200 rounded-lg shadow-xl z-50 min-w-[200px]">
                <div className="p-2">
                  {topics.map(topic => (
                    <button
                      key={topic}
                      onClick={() => toggleTopic(topic)}
                      className="w-full flex items-center gap-2 px-3 py-2 text-sm hover:bg-gray-50 rounded-lg"
                    >
                      <div className={`w-4 h-4 border rounded flex items-center justify-center ${
                        selectedTopics.includes(topic) ? 'bg-gray-900 border-gray-900' : 'border-gray-300'
                      }`}>
                        {selectedTopics.includes(topic) && (
                          <svg className="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                          </svg>
                        )}
                      </div>
                      <span className="text-gray-700">{topic}</span>
                    </button>
                  ))}
                </div>
              </div>
            )}
          </div>

          {/* Locations Filter */}
          <div className="relative" ref={locationsRef}>
            <button
              onClick={() => setShowLocations(!showLocations)}
              className="flex items-center gap-2 px-3 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50 bg-white"
            >
              <svg className="w-4 h-4 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
              </svg>
              <span className="text-gray-700">Locations</span>
              <ChevronDown className="w-4 h-4 text-gray-500" />
            </button>

            {showLocations && (
              <div className="absolute top-full mt-2 left-0 bg-white border border-gray-200 rounded-lg shadow-xl z-50 min-w-[200px]">
                <div className="p-2">
                  {locations.map(location => (
                    <button
                      key={location}
                      onClick={() => toggleLocation(location)}
                      className="w-full flex items-center gap-2 px-3 py-2 text-sm hover:bg-gray-50 rounded-lg"
                    >
                      <div className={`w-4 h-4 border rounded flex items-center justify-center ${
                        selectedLocations.includes(location) ? 'bg-gray-900 border-gray-900' : 'border-gray-300'
                      }`}>
                        {selectedLocations.includes(location) && (
                          <svg className="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                          </svg>
                        )}
                      </div>
                      <span className="text-gray-700">{location}</span>
                    </button>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Right Side Actions */}
        <div className="flex items-center gap-2">
          {/* Competitors Filter */}
          <div className="relative" ref={competitorsRef}>
            <button
              onClick={() => setShowCompetitors(!showCompetitors)}
              className="flex items-center gap-2 px-3 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50 bg-white"
            >
              <svg className="w-4 h-4 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
              </svg>
              <span className="text-gray-700">Competitors</span>
              <ChevronDown className="w-4 h-4 text-gray-500" />
            </button>

            {showCompetitors && (
              <div className="absolute top-full mt-2 right-0 bg-white border border-gray-200 rounded-lg shadow-xl z-50 min-w-[180px]">
                <div className="p-2">
                  <button
                    onClick={() => {
                      onCompetitorFilterChange('all');
                      setShowCompetitors(false);
                    }}
                    className={`w-full flex items-center justify-between px-3 py-2 text-sm hover:bg-gray-50 rounded-lg ${
                      competitorFilter === 'all' ? 'bg-gray-50' : ''
                    }`}
                  >
                    <span className="text-gray-700">All Competitors</span>
                    {competitorFilter === 'all' && (
                      <svg className="w-4 h-4 text-gray-900" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                      </svg>
                    )}
                  </button>
                  <button
                    onClick={() => {
                      onCompetitorFilterChange('direct');
                      setShowCompetitors(false);
                    }}
                    className={`w-full flex items-center justify-between px-3 py-2 text-sm hover:bg-gray-50 rounded-lg ${
                      competitorFilter === 'direct' ? 'bg-gray-50' : ''
                    }`}
                  >
                    <span className="text-gray-700">Direct</span>
                    {competitorFilter === 'direct' && (
                      <svg className="w-4 h-4 text-gray-900" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                      </svg>
                    )}
                  </button>
                  <button
                    onClick={() => {
                      onCompetitorFilterChange('serp');
                      setShowCompetitors(false);
                    }}
                    className={`w-full flex items-center justify-between px-3 py-2 text-sm hover:bg-gray-50 rounded-lg ${
                      competitorFilter === 'serp' ? 'bg-gray-50' : ''
                    }`}
                  >
                    <span className="text-gray-700">SERP</span>
                    {competitorFilter === 'serp' && (
                      <svg className="w-4 h-4 text-gray-900" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                      </svg>
                    )}
                  </button>
                </div>
              </div>
            )}
          </div>

          {/* Unbranded Toggle */}
          <button
            onClick={() => onUnbrandedChange(!showUnbranded)}
            className="flex items-center gap-2 px-3 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50 bg-white"
          >
            <div className={`relative w-10 h-5 rounded-full transition-colors ${
              showUnbranded ? 'bg-orange-500' : 'bg-gray-300'
            }`}>
              <div className={`absolute top-0.5 left-0.5 w-4 h-4 bg-white rounded-full transition-transform ${
                showUnbranded ? 'translate-x-5' : 'translate-x-0'
              }`} />
            </div>
            <span className="text-gray-700">Unbranded</span>
          </button>

          {/* Refresh Button */}
          {onRefresh && (
            <button
              onClick={onRefresh}
              className="hidden flex items-center gap-2 px-3 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50 bg-white"
            >
              <RefreshCw className="w-4 h-4 text-gray-500" />
              <span className="text-gray-700">Refresh</span>
            </button>
          )}

          {/* Auto Refresh */}
          {showAutoRefresh && (
            <button className="hidden flex items-center gap-2 px-3 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50 bg-white">
              <Sparkles className="w-4 h-4 text-gray-500" />
              <span className="text-gray-700">Auto</span>
            </button>
          )}
        </div>
      </div>
    </div>
  );
};

export default VisibilityFilterBar;
