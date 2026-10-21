import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { aiSeoService } from '../services/aiSeoService';
import contentCreationService from '../services/contentCreationService';
import { useProject } from '../contexts/ProjectContext';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Loader2, ArrowLeft, Sparkles, ChevronDown } from 'lucide-react';
import { toast } from '../utils/toast';
import type { CreateContentArticleDto } from '../services/contentArticleDto';
import type { ContentCreationData, TopicOption, PromptOption, CitationOption, BrandKitOption, AudienceSegmentOption } from '../types/contentCreation';

interface ContentCreationProps {
  projectId?: number;
}

const ContentCreation = ({ projectId: propProjectId }: ContentCreationProps) => {
  const { id } = useParams<{ id: string }>();
  const { selectedProject } = useProject();
  const navigate = useNavigate();
  const projectId = propProjectId || (id ? parseInt(id, 10) : selectedProject?.id || 0);

  const [generating, setGenerating] = useState(false);
  const [, setLoadingData] = useState(true);
  const [creationData, setCreationData] = useState<ContentCreationData | null>(null);
  
  const [topicsSearch, setTopicsSearch] = useState('');
  const [selectedTopic, setSelectedTopic] = useState<TopicOption | null>(null);
  const [filteredTopics, setFilteredTopics] = useState<TopicOption[]>([]);
  
  const [promptsSearch, setPromptsSearch] = useState('');
  const [selectedPrompts, setSelectedPrompts] = useState<PromptOption[]>([]);
  const [filteredPrompts, setFilteredPrompts] = useState<PromptOption[]>([]);
  
  const [selectedPlatforms, setSelectedPlatforms] = useState<string[]>([]);
  
  const [citationsSearch, setCitationsSearch] = useState('');
  const [selectedCitations, setSelectedCitations] = useState<CitationOption[]>([]);
  const [filteredCitations, setFilteredCitations] = useState<CitationOption[]>([]);
  
  const [brandKit, setBrandKit] = useState<BrandKitOption | null>(null);
  const [audienceSegment, setAudienceSegment] = useState<AudienceSegmentOption | null>(null);
  const [additionalInstructions, setAdditionalInstructions] = useState('');
  const [openDropdown, setOpenDropdown] = useState<string | null>(null);
  
  const platformOptions = ['ChatGPT', 'Claude', 'Gemini', 'Perplexity', 'SearchGPT'];

  const handleGenerateArticle = async () => {
    if (!selectedTopic && !topicsSearch || !projectId) {
      toast.error('Please select or enter a topic');
      return;
    }
    setGenerating(true);
    try {
      console.log('Creating article with data:', {
        projectId,
        title: topicsSearch,
        status: 'Draft'
      });
      
      const topicTitle = selectedTopic?.topic || topicsSearch;
      const promptTexts = selectedPrompts.map(p => p.promptText).join(', ');
      const citationUrls = selectedCitations.map(c => c.url).join(', ');
      
      const articleData: CreateContentArticleDto = {
        projectId: projectId,
        title: topicTitle,
        status: 'Draft' as const,
        contentHtml: `<h1>${topicTitle}</h1><p>Target topic: ${topicTitle}</p><p>Target prompts: ${promptTexts || 'General content'}</p><p>Target platforms: ${selectedPlatforms.join(', ') || 'All platforms'}</p><p>Citations: ${citationUrls || 'None'}</p>`,
        contentMarkdown: `# ${topicTitle}\n\nTarget topic: ${topicTitle}\n\nTarget prompts: ${promptTexts || 'General content'}\n\nTarget platforms: ${selectedPlatforms.join(', ') || 'All platforms'}\n\nCitations: ${citationUrls || 'None'}`,
        wordCount: 500,
        seoScore: 75,
        generationPrompt: additionalInstructions || undefined
      };
      
      console.log('Article payload:', articleData);
      const newArticle = await aiSeoService.createArticle(articleData);

      console.log('Article created successfully:', newArticle);
      toast.success('Content brief generated successfully!');
      navigate(`/projects/${projectId}/content`);
    } catch (error: any) {
      console.error("Failed to create article:", error);
      console.error("Error response:", error.response?.data);
      console.error("Error status:", error.response?.status);
      
      const errorMessage = error.response?.data?.message || 
                          error.response?.data?.title ||
                          JSON.stringify(error.response?.data) ||
                          error.message || 
                          'Failed to create article';
      
      toast.error(errorMessage);
    } finally {
      setGenerating(false);
    }
  };
  
  const togglePlatform = (platform: string) => {
    setSelectedPlatforms(prev => 
      prev.includes(platform) ? prev.filter(p => p !== platform) : [...prev, platform]
    );
  };

  useEffect(() => {
    if (projectId) {
      fetchCreationData();
    }
  }, [projectId]);

  useEffect(() => {
    if (creationData) {
      setFilteredTopics(
        creationData.topics.filter(t => 
          t.topic.toLowerCase().includes(topicsSearch.toLowerCase())
        )
      );
    }
  }, [topicsSearch, creationData]);

  useEffect(() => {
    if (creationData) {
      setFilteredPrompts(
        creationData.prompts.filter(p => 
          p.promptText.toLowerCase().includes(promptsSearch.toLowerCase())
        )
      );
    }
  }, [promptsSearch, creationData]);

  useEffect(() => {
    if (creationData) {
      setFilteredCitations(
        creationData.citations.filter(c => 
          c.title.toLowerCase().includes(citationsSearch.toLowerCase()) ||
          c.url.toLowerCase().includes(citationsSearch.toLowerCase())
        )
      );
    }
  }, [citationsSearch, creationData]);

  useEffect(() => {
    const handleClickOutside = () => setOpenDropdown(null);
    if (openDropdown) {
      document.addEventListener('click', handleClickOutside);
      return () => document.removeEventListener('click', handleClickOutside);
    }
  }, [openDropdown]);

  const fetchCreationData = async () => {
    if (!projectId) return;
    
    try {
      setLoadingData(true);
      const data = await contentCreationService.getCreationData(projectId);
      setCreationData(data);
      setFilteredTopics(data.topics);
      setFilteredPrompts(data.prompts);
      setFilteredCitations(data.citations);
    } catch (error: any) {
      console.error('Failed to load creation data:', error);
      toast.error('Failed to load dropdown data');
    } finally {
      setLoadingData(false);
    }
  };

  const handleBack = () => {
    navigate(`/projects/${projectId}/content`);
  };

  if (!projectId) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <p className="text-gray-500">Please select a project to create content</p>
        </div>
      </div>
    );
  }

  return (
    <div className="h-full flex flex-col bg-gradient-to-br  via-blue-50/20 to-gray-50">
      {/* Header */}
      <div className="mb-4">
        <button
          onClick={handleBack}
          className="flex items-center gap-2 px-2 py-1 text-xs text-gray-600 hover:text-gray-900 hover:bg-white rounded-lg transition-all shadow-sm"
        >
          <ArrowLeft className="w-3.5 h-3.5" />
          Back
        </button>
      </div>

      {/* Main Content - Two Column Layout */}
      <div className="flex-1 grid grid-cols-2 gap-5">
        {/* Left Sidebar - Form */}
        <div className="bg-white rounded-xl border border-gray-200/50 p-5 shadow-xl shadow-gray-200/50 overflow-y-auto backdrop-blur-sm">
          <div className="flex items-center gap-2 mb-2">
            <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-blue-600 to-blue-500 flex items-center justify-center shadow-lg shadow-blue-500/30">
              <Sparkles className="w-4 h-4 text-white" />
            </div>
            <h2 className="text-lg font-bold text-gray-900">Content Configuration</h2>
          </div>
          <p className="text-xs text-gray-600 mb-5">
            Write long-form AEO content based on an analysis of the current top-cited pages.
          </p>
          <div className="space-y-3">
            {/* Topics */}
            <div>
              <label className="block text-xs font-bold text-gray-700 mb-2">Topics</label>
              <div className="relative">
                <Input
                  value={topicsSearch}
                  onChange={(e) => setTopicsSearch(e.target.value)}
                  onFocus={() => setOpenDropdown('topics')}
                  placeholder="Search or select topics"
                  className="w-full text-sm h-9 rounded-lg border-2 border-gray-200 focus:border-blue-500 focus:ring-2 focus:ring-blue-500/20 transition-all"
                />
                {openDropdown === 'topics' && filteredTopics.length > 0 && (
                  <div className="absolute top-full mt-1 left-0 right-0 bg-white border border-gray-200 rounded-lg shadow-xl z-50 max-h-48 overflow-y-auto">
                    {filteredTopics.slice(0, 10).map(topic => (
                      <button
                        key={topic.id}
                        onClick={() => { setSelectedTopic(topic); setTopicsSearch(topic.topic); setOpenDropdown(null); }}
                        className="w-full px-3 py-2 text-xs text-left hover:bg-blue-50 transition-all border-b border-gray-100 last:border-0"
                      >
                        <div className="font-medium text-gray-900">{topic.topic}</div>
                        {topic.description && <div className="text-gray-500 text-[10px]">{topic.description}</div>}
                      </button>
                    ))}
                  </div>
                )}
              </div>
              {selectedTopic && (
                <div className="mt-1 text-[10px] text-blue-600">Selected: {selectedTopic.topic}</div>
              )}
            </div>
            
            {/* Prompts */}
            <div>
              <label className="block text-xs font-bold text-gray-700 mb-2">Prompts</label>
              <div className="relative">
                <Input
                  value={promptsSearch}
                  onChange={(e) => setPromptsSearch(e.target.value)}
                  onFocus={() => setOpenDropdown('prompts')}
                  placeholder="Search or select prompts"
                  className="w-full text-sm h-9 rounded-lg border-2 border-gray-200 focus:border-blue-500 focus:ring-2 focus:ring-blue-500/20 transition-all"
                />
                {openDropdown === 'prompts' && filteredPrompts.length > 0 && (
                  <div className="absolute top-full mt-1 left-0 right-0 bg-white border border-gray-200 rounded-lg shadow-xl z-50 max-h-48 overflow-y-auto">
                    {filteredPrompts.slice(0, 10).map(prompt => (
                      <button
                        key={prompt.id}
                        onClick={() => {
                          if (!selectedPrompts.find(p => p.id === prompt.id)) {
                            setSelectedPrompts([...selectedPrompts, prompt]);
                          }
                          setPromptsSearch('');
                          setOpenDropdown(null);
                        }}
                        className="w-full px-3 py-2 text-xs text-left hover:bg-blue-50 transition-all border-b border-gray-100 last:border-0"
                      >
                        <div className="font-medium text-gray-900">{prompt.promptText}</div>
                        {prompt.category && <div className="text-gray-500 text-[10px]">{prompt.category}</div>}
                      </button>
                    ))}
                  </div>
                )}
              </div>
              {selectedPrompts.length > 0 && (
                <div className="mt-2 flex flex-wrap gap-1">
                  {selectedPrompts.map(prompt => (
                    <span key={prompt.id} className="inline-flex items-center gap-1 px-2 py-1 bg-blue-100 text-blue-700 rounded text-[10px]">
                      {prompt.promptText.substring(0, 30)}...
                      <button onClick={() => setSelectedPrompts(selectedPrompts.filter(p => p.id !== prompt.id))} className="hover:text-blue-900">×</button>
                    </span>
                  ))}
                </div>
              )}
            </div>
            
            {/* Platforms */}
            <div>
              <label className="block text-xs font-bold text-gray-700 mb-2">Platforms</label>
              <div className="flex flex-wrap gap-2">
                {platformOptions.map(platform => (
                  <button
                    key={platform}
                    onClick={() => togglePlatform(platform)}
                    className={`px-3 py-1.5 rounded-lg text-xs border transition-all font-semibold shadow-sm ${
                      selectedPlatforms.includes(platform)
                        ? 'bg-gradient-to-r from-blue-600 to-blue-500 text-white border-blue-600 shadow-blue-500/30'
                        : 'bg-white text-gray-700 border-gray-300 hover:border-blue-400 hover:bg-blue-50'
                    }`}
                  >
                    {platform}
                  </button>
                ))}
              </div>
            </div>
            
            {/* Top-Cited Pages */}
            <div>
              <label className="block text-xs font-bold text-gray-700 mb-2">
                Top-Cited Pages (optional) Max. 20
              </label>
              <div className="relative">
                <Input
                  value={citationsSearch}
                  onChange={(e) => setCitationsSearch(e.target.value)}
                  onFocus={() => setOpenDropdown('citations')}
                  placeholder="Search or select citations"
                  className="w-full text-sm h-9 rounded-lg border-2 border-gray-200 focus:border-blue-500 focus:ring-2 focus:ring-blue-500/20 transition-all"
                />
                {openDropdown === 'citations' && filteredCitations.length > 0 && (
                  <div className="absolute top-full mt-1 left-0 right-0 bg-white border border-gray-200 rounded-lg shadow-xl z-50 max-h-48 overflow-y-auto">
                    {filteredCitations.slice(0, 20).map(citation => (
                      <button
                        key={citation.id}
                        onClick={() => {
                          if (selectedCitations.length < 20 && !selectedCitations.find(c => c.id === citation.id)) {
                            setSelectedCitations([...selectedCitations, citation]);
                          }
                          setCitationsSearch('');
                          setOpenDropdown(null);
                        }}
                        className="w-full px-3 py-2 text-xs text-left hover:bg-blue-50 transition-all border-b border-gray-100 last:border-0"
                      >
                        <div className="font-medium text-gray-900">{citation.title}</div>
                        <div className="text-gray-500 text-[10px]">{citation.domain} • {citation.citationCount} citations</div>
                      </button>
                    ))}
                  </div>
                )}
              </div>
              {selectedCitations.length > 0 && (
                <div className="mt-2 space-y-1">
                  {selectedCitations.map(citation => (
                    <div key={citation.id} className="flex items-center justify-between px-2 py-1 bg-gray-50 rounded text-[10px]">
                      <span className="truncate flex-1">{citation.title}</span>
                      <button onClick={() => setSelectedCitations(selectedCitations.filter(c => c.id !== citation.id))} className="ml-2 text-gray-500 hover:text-gray-900">×</button>
                    </div>
                  ))}
                </div>
              )}
            </div>
            
            {/* Brand Kit */}
            <div>
              <label className="block text-xs font-bold text-gray-700 mb-2">
                Brand Kit (optional)
              </label>
              <div className="relative">
                <button
                  onClick={(e) => { e.stopPropagation(); setOpenDropdown(openDropdown === 'brand' ? null : 'brand'); }}
                  className="w-full px-3 py-2 text-left border-2 border-gray-200 rounded-lg hover:bg-gray-50 hover:border-blue-400 flex items-center justify-between transition-all shadow-sm"
                >
                  <span className="text-xs text-gray-600 font-medium">{brandKit?.name || 'Select brand kit'}</span>
                  <ChevronDown className="w-4 h-4 text-gray-400" />
                </button>
                {openDropdown === 'brand' && creationData && creationData.brandKits.length > 0 && (
                  <div className="absolute top-full mt-1 left-0 right-0 bg-white border border-gray-200 rounded-lg shadow-xl z-50 overflow-hidden">
                    {creationData.brandKits.map(kit => (
                      <button
                        key={kit.id}
                        onClick={() => { setBrandKit(kit); setOpenDropdown(null); }}
                        className="w-full px-3 py-2 text-xs text-left hover:bg-blue-50 transition-all font-medium"
                      >
                        {kit.name}
                        {kit.description && <div className="text-gray-500 text-[10px] mt-0.5">{kit.description.substring(0, 50)}</div>}
                      </button>
                    ))}
                  </div>
                )}
              </div>
            </div>
            
            {/* Audience Segment */}
            <div>
              <label className="block text-xs font-bold text-gray-700 mb-2">
                Audience segment (optional)
              </label>
              <div className="relative">
                <button
                  onClick={(e) => { e.stopPropagation(); setOpenDropdown(openDropdown === 'audience' ? null : 'audience'); }}
                  className="w-full px-3 py-2 text-left border-2 border-gray-200 rounded-lg hover:bg-gray-50 hover:border-blue-400 flex items-center justify-between transition-all shadow-sm"
                >
                  <span className="text-xs text-gray-600 font-medium">{audienceSegment?.name || 'Select audience segment'}</span>
                  <ChevronDown className="w-4 h-4 text-gray-400" />
                </button>
                {openDropdown === 'audience' && creationData && creationData.audienceSegments.length > 0 && (
                  <div className="absolute top-full mt-1 left-0 right-0 bg-white border border-gray-200 rounded-lg shadow-xl z-50 overflow-hidden">
                    {creationData.audienceSegments.map(segment => (
                      <button
                        key={segment.id}
                        onClick={() => { setAudienceSegment(segment); setOpenDropdown(null); }}
                        className="w-full px-3 py-2 text-xs text-left hover:bg-blue-50 transition-all font-medium"
                      >
                        {segment.name}
                      </button>
                    ))}
                  </div>
                )}
              </div>
            </div>
            
            {/* Additional Instructions */}
            <div>
              <button
                onClick={() => setAdditionalInstructions(additionalInstructions ? '' : ' ')}
                className="text-xs text-blue-600 hover:text-blue-700 mb-2 font-semibold transition-colors"
              >
                + Add additional instructions
              </button>
              {additionalInstructions !== '' && (
                <textarea
                  value={additionalInstructions}
                  onChange={(e) => setAdditionalInstructions(e.target.value)}
                  className="w-full min-h-[100px] px-3 py-2 border-2 border-gray-200 rounded-lg text-xs focus:border-blue-500 focus:ring-2 focus:ring-blue-500/20 transition-all"
                  placeholder="Add any specific instructions for content generation..."
                />
              )}
            </div>
            
            <div className="pt-3">
              <Button
                onClick={handleGenerateArticle}
                disabled={generating || !topicsSearch}
                className="w-full bg-gradient-to-r from-blue-600 to-blue-500 hover:from-blue-700 hover:to-blue-600 text-white font-semibold shadow-lg shadow-blue-500/30 hover:shadow-xl hover:shadow-blue-500/40 transition-all active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed disabled:transform-none h-10 text-sm rounded-lg"
              >
                {generating ? (
                  <><Loader2 className="mr-2 h-4 w-4 animate-spin" /> Generating Content Brief...</>
                ) : (
                  <><Sparkles className="mr-2 h-4 w-4" /> Generate Content Brief</>
                )}
              </Button>
            </div>
          </div>
        </div>

        {/* Right Side - Preview/Instructions */}
        <div className="bg-white rounded-xl border border-gray-200/50 p-8 shadow-xl shadow-gray-200/50 flex flex-col items-center justify-center text-center backdrop-blur-sm">
          <div className="max-w-md">
            <h3 className="text-lg font-bold text-gray-900 mb-2">
              Begin by setting up your content configuration
            </h3>
            <p className="text-sm text-gray-600">
              Enter key details in the left sidebar to optimize your content for AEO.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ContentCreation;
