import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import contentArticleService, { ContentArticle } from '../services/contentArticleService';
import { useProject } from '../contexts/ProjectContext';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { toast } from '../utils/toast';
import { ArrowLeft, Save, Eye, X, Plus } from 'lucide-react';
import ReactQuill from 'react-quill';
import 'react-quill/dist/quill.snow.css';
import '../styles/quill-custom.css';

type EditorTab = 'content' | 'metadata';

interface ContentEditorProps {
  projectId?: number;
}

const ContentEditor = ({ projectId: propProjectId }: ContentEditorProps) => {
  const { id, articleId } = useParams<{ id: string; articleId: string }>();
  const { selectedProject } = useProject();
  const navigate = useNavigate();
  const projectId = propProjectId || (id ? parseInt(id, 10) : selectedProject?.id || 0);

  const [selectedArticle, setSelectedArticle] = useState<ContentArticle | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [lastSaved, setLastSaved] = useState<Date | null>(null);
  const [editorTab, setEditorTab] = useState<EditorTab>('content');

  // Article fields
  const [articleTitle, setArticleTitle] = useState('');
  const [articleContent, setArticleContent] = useState('');
  const [articleStatus, setArticleStatus] = useState<'Draft' | 'Published' | 'Archived'>('Draft');
  const [metaDescription, setMetaDescription] = useState('');

  // Strategic Overview fields
  const [targetTopic, setTargetTopic] = useState('');
  const [targetPrompts, setTargetPrompts] = useState<string[]>([]);
  const [targetPlatforms, setTargetPlatforms] = useState<string[]>([]);
  const [citedPages, setCitedPages] = useState<string[]>([]);
  const [newPrompt, setNewPrompt] = useState('');
  const [newCitedPage, setNewCitedPage] = useState('');

  useEffect(() => {
    if (projectId && articleId) {
      fetchArticle();
    }
  }, [projectId, articleId]);


  const fetchArticle = async () => {
    setLoading(true);
    try {
      const article = await contentArticleService.getArticleById(parseInt(articleId!));
      setSelectedArticle(article);
      setArticleTitle(article.title);
      setArticleContent(article.contentHtml || '');
      setArticleStatus((article.status as 'Draft' | 'Published' | 'Archived') || 'Draft');
      setMetaDescription(article.metaDescription || '');

      if (article.generationPrompt) {
        try {
          const promptData = JSON.parse(article.generationPrompt);
          setTargetTopic(promptData.targetTopic || '');
          setTargetPrompts(promptData.targetPrompts || []);
          setTargetPlatforms(promptData.targetPlatforms || []);
          setCitedPages(promptData.citedPages || []);
        } catch (e) {
          setTargetTopic(article.generationPrompt);
        }
      }
    } catch (error) {
      console.error("Failed to load article", error);
      toast.error('Failed to load article');
    } finally {
      setLoading(false);
    }
  };

  const handleSaveArticle = async (silent = false) => {
    if (!selectedArticle || !projectId) return;

    setSaving(true);
    try {
      const promptData = {
        targetTopic,
        targetPrompts,
        targetPlatforms,
        citedPages
      };

      await contentArticleService.updateArticle(parseInt(articleId!), {
        title: articleTitle,
        contentHtml: articleContent,
        status: articleStatus,
        metaDescription: metaDescription,
        generationPrompt: JSON.stringify(promptData)
      });

      setLastSaved(new Date());
      if (!silent) {
        toast.success('Article saved successfully');
      }
    } catch (error) {
      console.error("Failed to update article", error);
      if (!silent) {
        toast.error('Failed to save article');
      }
    } finally {
      setSaving(false);
    }
  };

  const addPrompt = () => {
    if (newPrompt.trim()) {
      setTargetPrompts([...targetPrompts, newPrompt.trim()]);
      setNewPrompt('');
    }
  };

  const removePrompt = (index: number) => {
    setTargetPrompts(targetPrompts.filter((_, i) => i !== index));
  };

  const addCitedPage = () => {
    if (newCitedPage.trim()) {
      setCitedPages([...citedPages, newCitedPage.trim()]);
      setNewCitedPage('');
    }
  };

  const removeCitedPage = (index: number) => {
    setCitedPages(citedPages.filter((_, i) => i !== index));
  };

  // Quill editor modules configuration
  const quillModules = {
    toolbar: [
      [{ 'header': [1, 2, 3, false] }],
      ['bold', 'italic', 'underline'],
      [{ 'list': 'ordered'}, { 'list': 'bullet' }],
      [{ 'align': [] }],
      ['link'],
      ['clean']
    ],
  };

  const quillFormats = [
    'header',
    'bold', 'italic', 'underline',
    'list', 'bullet',
    'align',
    'link'
  ];

  const handleBack = () => {
    navigate(`/projects/${projectId}/content`);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-gray-900 mx-auto mb-4"></div>
          <p className="text-gray-500">Loading article...</p>
        </div>
      </div>
    );
  }

  if (!selectedArticle) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="text-center">
          <p className="text-gray-500 text-lg">Article not found</p>
          <Button onClick={handleBack} className="mt-4">Go Back</Button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-50 via-blue-50/20 to-gray-50">
      {/* Header */}
      <div className="bg-white/80 backdrop-blur-sm border-b border-gray-200/50 sticky top-0 z-20 shadow-lg shadow-gray-200/50">
        <div className="px-6 py-2.5">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <button
                onClick={handleBack}
                className="flex items-center gap-2 px-2 py-1 text-xs text-gray-600 hover:text-gray-900 hover:bg-gray-100 rounded-lg transition-all"
              >
                <ArrowLeft className="w-3.5 h-3.5" />
                Back
              </button>
              <div className="h-5 w-px bg-gray-300" />
              <span className="text-sm font-bold text-gray-900 truncate max-w-md">
                {articleTitle || 'Untitled Article'}
              </span>
              {lastSaved && (
                <div className="flex items-center gap-1.5">
                  <div className="w-1.5 h-1.5 bg-green-500 rounded-full animate-pulse" />
                  <span className="text-xs text-green-600 font-medium">
                    Saved {lastSaved.toLocaleTimeString()}
                  </span>
                </div>
              )}
            </div>
            <div className="flex items-center gap-2">
              <span className={`px-2.5 py-1 text-xs font-bold rounded-lg shadow-sm ${
                articleStatus === 'Published' ? 'bg-gradient-to-r from-green-500 to-emerald-500 text-white shadow-green-500/30' :
                articleStatus === 'Archived' ? 'bg-gradient-to-r from-gray-400 to-gray-500 text-white shadow-gray-500/30' :
                'bg-gradient-to-r from-orange-500 to-orange-600 text-white shadow-orange-500/30'
              }`}>
                {articleStatus}
              </span>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="flex h-[calc(100vh-56px)]">
        {/* Main Editor Column */}
        <div className="flex-1 overflow-y-auto">
          <div className="bg-white/50 backdrop-blur-sm h-full">
            {/* Tabs */}
            <div className="border-b border-gray-200/50 bg-white/80 backdrop-blur-sm sticky top-0 z-10">
              <nav className="flex space-x-4 px-6" aria-label="Tabs">
                <button
                  onClick={() => setEditorTab('content')}
                  className={`py-2.5 px-3 border-b-2 text-xs font-semibold transition-all rounded-t-lg ${
                    editorTab === 'content'
                      ? 'border-blue-600 text-blue-600 bg-blue-50/50'
                      : 'border-transparent text-gray-500 hover:text-gray-700 hover:bg-gray-50'
                  }`}
                >
                  Content Brief
                </button>
                <button
                  onClick={() => setEditorTab('metadata')}
                  className={`py-2.5 px-3 border-b-2 text-xs font-semibold transition-all rounded-t-lg ${
                    editorTab === 'metadata'
                      ? 'border-blue-600 text-blue-600 bg-blue-50/50'
                      : 'border-transparent text-gray-500 hover:text-gray-700 hover:bg-gray-50'
                  }`}
                >
                  Settings
                </button>
              </nav>
            </div>

            {/* Tab Content */}
            <div className="p-4">
              {editorTab === 'content' && (
                <div className="space-y-2.5 max-w-5xl">
                  {/* Title */}
                  <div className="pb-2.5 border-b border-gray-200">
                    <Input
                      value={articleTitle}
                      onChange={(e) => setArticleTitle(e.target.value)}
                      className="text-lg font-bold border-0 px-0 focus:ring-0 placeholder:text-gray-300 transition-all bg-transparent"
                      placeholder="Enter your article title..."
                    />
                  </div>

                  {/* Strategic Overview Section */}
                  <div className="space-y-2">

                    {/* Target Topic */}
                    <div className="bg-white rounded-lg p-2.5 border-l-2 border-l-blue-600 border-t border-r border-b border-gray-200/50 shadow-lg shadow-gray-200/50 hover:shadow-xl hover:shadow-blue-100/50 transition-all">
                      <div className="flex items-center gap-2.5">
                        <label className="text-xs text-gray-700 font-bold whitespace-nowrap min-w-[90px]">Target topic:</label>
                        <Input
                          value={targetTopic}
                          onChange={(e) => setTargetTopic(e.target.value)}
                          className="flex-1 text-xs h-7 px-2 border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all"
                          placeholder="e.g., Soccer (football)"
                        />
                      </div>
                    </div>

                    {/* Target Prompts */}
                    <div className="bg-white rounded-lg p-2.5 border-l-2 border-l-blue-600 border-t border-r border-b border-gray-200/50 shadow-lg shadow-gray-200/50 hover:shadow-xl hover:shadow-blue-100/50 transition-all">
                      <label className="block text-xs text-gray-700 font-bold mb-2">Target prompts:</label>
                      <div className="space-y-1">
                        {targetPrompts.map((prompt, index) => (
                          <div key={index} className="flex items-center gap-2 p-1.5 bg-gradient-to-r from-blue-50 to-indigo-50 rounded-lg border border-blue-200 hover:border-blue-300 transition-all group shadow-sm">
                            <span className="text-xs text-gray-800 flex-1 font-medium">{prompt}</span>
                            <button
                              onClick={() => removePrompt(index)}
                              className="text-gray-400 hover:text-red-600 transition-colors opacity-0 group-hover:opacity-100"
                            >
                              <X className="w-3 h-3" />
                            </button>
                          </div>
                        ))}
                        <div className="flex gap-1 mt-2">
                          <Input
                            value={newPrompt}
                            onChange={(e) => setNewPrompt(e.target.value)}
                            onKeyPress={(e) => e.key === 'Enter' && addPrompt()}
                            className="flex-1 text-xs h-7 px-2 border-gray-300 rounded focus:ring-1 focus:ring-blue-500 focus:border-transparent transition-all"
                            placeholder="Add a target prompt..."
                          />
                          <button
                            onClick={addPrompt}
                            className="px-3 py-1 text-xs bg-gradient-to-r from-blue-600 to-blue-500 text-white hover:from-blue-700 hover:to-blue-600 rounded-lg transition-all font-semibold shadow-lg shadow-blue-500/30 flex items-center gap-1"
                          >
                            <Plus className="w-3 h-3" />
                            Add
                          </button>
                        </div>
                      </div>
                    </div>

                    {/* Target Platforms */}
                    <div className="bg-white rounded-lg p-2.5 border-l-2 border-l-blue-600 border-t border-r border-b border-gray-200/50 shadow-lg shadow-gray-200/50 hover:shadow-xl hover:shadow-blue-100/50 transition-all">
                      <div className="flex items-center gap-2.5">
                        <label className="text-xs text-gray-700 font-bold whitespace-nowrap">Target platforms:</label>
                        <div className="flex flex-wrap gap-1.5">
                          {['ChatGPT', 'Claude', 'Gemini', 'Perplexity'].map(platform => (
                            <button
                              key={platform}
                              onClick={() => {
                                const newPlatforms = targetPlatforms.includes(platform)
                                  ? targetPlatforms.filter(p => p !== platform)
                                  : [...targetPlatforms, platform];
                                setTargetPlatforms(newPlatforms);
                              }}
                              className={`px-2.5 py-1 text-xs rounded-lg border transition-all font-semibold shadow-sm ${
                                targetPlatforms.includes(platform)
                                  ? 'bg-gradient-to-r from-blue-600 to-blue-500 text-white border-blue-600 shadow-blue-500/30'
                                  : 'bg-white text-gray-700 border-gray-300 hover:border-blue-400 hover:bg-blue-50'
                              }`}
                            >
                              {platform}
                            </button>
                          ))}
                        </div>
                      </div>
                    </div>

                    {/* Top-Cited Pages */}
                    <div className="bg-white rounded-lg p-2.5 border-l-2 border-l-blue-600 border-t border-r border-b border-gray-200/50 shadow-lg shadow-gray-200/50 hover:shadow-xl hover:shadow-blue-100/50 transition-all">
                      <label className="block text-xs text-gray-700 font-bold mb-2">Top-cited pages:</label>
                      <div className="space-y-1">
                        {citedPages.map((page, index) => (
                          <div key={index} className="flex items-center gap-2 p-1.5 bg-gradient-to-r from-blue-50 to-indigo-50 rounded-lg border border-blue-200 hover:border-blue-300 transition-all group shadow-sm">
                            <a
                              href={page}
                              target="_blank"
                              rel="noopener noreferrer"
                              className="text-xs text-blue-600 hover:text-blue-700 hover:underline truncate flex-1 font-medium"
                            >
                              {page}
                            </a>
                            <button
                              onClick={() => removeCitedPage(index)}
                              className="text-gray-400 hover:text-red-600 transition-colors opacity-0 group-hover:opacity-100"
                            >
                              <X className="w-3 h-3" />
                            </button>
                          </div>
                        ))}
                        <div className="flex gap-1 mt-2">
                          <Input
                            value={newCitedPage}
                            onChange={(e) => setNewCitedPage(e.target.value)}
                            onKeyPress={(e) => e.key === 'Enter' && addCitedPage()}
                            className="flex-1 text-xs h-7 px-2 border-gray-300 rounded focus:ring-1 focus:ring-blue-500 focus:border-transparent transition-all"
                            placeholder="https://example.com/article"
                          />
                          <button
                            onClick={addCitedPage}
                            className="px-3 py-1 text-xs bg-gradient-to-r from-blue-600 to-blue-500 text-white hover:from-blue-700 hover:to-blue-600 rounded-lg transition-all font-semibold shadow-lg shadow-blue-500/30 flex items-center gap-1"
                          >
                            <Plus className="w-3 h-3" />
                            Add
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>

                  {/* Rich Text Editor with React Quill */}
                  <div className="border-t border-gray-200 pt-4 mt-4">
                    <label className="block text-xs text-gray-700 font-bold mb-4 flex items-center gap-2">
                      <span className="w-1 h-4 bg-gradient-to-b from-blue-600 to-blue-500 rounded-full shadow-sm"></span>
                      Content Editor
                    </label>
                    {/* Centered floating editor container */}
                    <div className="flex justify-center pb-32">
                      <div className="w-full max-w-4xl">
                        <div className="quill-editor-wrapper bg-white shadow-2xl shadow-gray-300/50 rounded-2xl border border-gray-200/50">
                          <ReactQuill
                            theme="snow"
                            value={articleContent}
                            onChange={setArticleContent}
                            modules={quillModules}
                            formats={quillFormats}
                            placeholder="Start writing your content here..."
                          />
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              )}

              {editorTab === 'metadata' && (
                <div className="space-y-2 max-w-3xl">
                  <h2 className="text-sm font-bold text-gray-900 flex items-center gap-2 mb-2">
                    <span className="w-1 h-4 bg-gradient-to-b from-blue-600 to-blue-500 rounded-full shadow-sm"></span>
                    Article Settings
                  </h2>
                  <div className="bg-white rounded-lg p-2.5 border-l-2 border-l-blue-600 border-t border-r border-b border-gray-200/50 shadow-lg shadow-gray-200/50 hover:shadow-xl hover:shadow-blue-100/50 transition-all">
                    <div className="flex items-center gap-2.5">
                      <label className="text-xs font-bold text-gray-700 whitespace-nowrap min-w-[90px]">Article Status:</label>
                      <select
                        value={articleStatus}
                        onChange={(e) => setArticleStatus(e.target.value as 'Draft' | 'Published' | 'Archived')}
                        className="flex-1 px-2 py-1 text-xs border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 bg-white font-semibold transition-all"
                      >
                        <option value="Draft">Draft</option>
                        <option value="Published">Published</option>
                        <option value="Archived">Archived</option>
                      </select>
                    </div>
                  </div>
                  <div className="bg-white rounded-lg p-2.5 border-l-2 border-l-blue-600 border-t border-r border-b border-gray-200/50 shadow-lg shadow-gray-200/50 hover:shadow-xl hover:shadow-blue-100/50 transition-all">
                    <label className="block text-xs font-bold text-gray-700 mb-2">Meta Description:</label>
                    <textarea
                      value={metaDescription}
                      onChange={(e) => setMetaDescription(e.target.value)}
                      rows={3}
                      className="w-full px-2 py-1.5 text-xs border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all"
                      placeholder="Enter meta description for SEO..."
                    />
                    <p className={`mt-1 text-xs font-medium ${metaDescription.length > 160 ? 'text-red-600' : 'text-gray-500'}`}>
                      {metaDescription.length} / 160 characters
                    </p>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Right Sidebar - Workflow */}
        <div className="w-64 bg-gradient-to-br from-blue-50/30 via-purple-50/20 to-gray-50 border-l border-gray-200/50 overflow-y-auto shadow-xl">
          <div className="p-3">
            <h3 className="text-xs font-bold text-gray-900 mb-2 flex items-center gap-1.5">
              <span className="w-1 h-3 bg-gradient-to-b from-blue-600 to-blue-500 rounded-full shadow-sm"></span>
              Workflow Progress
            </h3>
            <div className="mb-3 bg-white rounded-lg p-2.5 border-l-2 border-l-green-500 border-t border-r border-b border-gray-200/50 shadow-lg shadow-green-200/50">
              <div className="flex items-center justify-between mb-2">
                <span className="text-xs text-gray-700 font-bold">9 / 9 completed</span>
                <span className="text-sm text-green-600 font-bold">100%</span>
              </div>
              <div className="w-full h-1.5 bg-gray-200 rounded-full overflow-hidden">
                <div className="h-full bg-gradient-to-r from-green-500 to-emerald-500 transition-all shadow-sm" style={{ width: '100%' }} />
              </div>
            </div>
            
            <div className="space-y-1 mb-4">
                {[
                  'Starting workflow',
                  'Gathering citations',
                  'Perplexity research',
                  'Research output',
                  'Analyze research',
                  'Extract outline',
                  'Build brief',
                  'Add metadata',
                  'Workflow completed'
                ].map((step, index) => (
                  <div key={index} className="flex items-center gap-2 p-1.5 bg-white rounded-lg border border-gray-200/50 shadow-sm hover:shadow-md transition-all hover:border-green-300">
                    <div className="w-4 h-4 rounded-full bg-gradient-to-br from-green-500 to-emerald-500 flex items-center justify-center flex-shrink-0 shadow-sm shadow-green-500/30">
                      <svg className="w-2.5 h-2.5 text-white" fill="currentColor" viewBox="0 0 20 20">
                        <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                      </svg>
                    </div>
                    <span className="text-xs text-gray-900 font-semibold">{step}</span>
                  </div>
                ))}
            </div>

            <div className="space-y-2">
              <button
                onClick={() => handleSaveArticle(false)}
                disabled={saving}
                className="w-full px-3 py-2 text-xs bg-gradient-to-r from-blue-600 to-blue-500 text-white rounded-lg hover:from-blue-700 hover:to-blue-600 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-1.5 font-semibold transition-all shadow-lg shadow-blue-500/30 hover:shadow-xl hover:shadow-blue-500/40 active:scale-95"
              >
                {saving ? (
                  <>
                    <div className="w-3 h-3 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                    Saving...
                  </>
                ) : (
                  <>
                    <Save className="w-3 h-3" />
                    Save Changes
                  </>
                )}
              </button>
              <button className="w-full px-3 py-2 text-xs border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 hover:border-gray-400 flex items-center justify-center gap-1.5 font-semibold transition-all shadow-sm hover:shadow-md active:scale-95">
                <Eye className="w-3 h-3" />
                Preview
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ContentEditor;
