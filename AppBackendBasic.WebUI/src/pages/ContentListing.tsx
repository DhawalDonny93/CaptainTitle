import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import contentArticleService, { ContentArticle } from '../services/contentArticleService';
import { useProject } from '../contexts/ProjectContext';
import { Button } from '@/components/ui/Button';
import { Loader2, Plus, FileText, Sparkles, MoreHorizontal } from 'lucide-react';

interface ContentListingProps {
  projectId?: number;
}

const ContentListing = ({ projectId: propProjectId }: ContentListingProps) => {
  const { id } = useParams<{ id: string }>();
  const { selectedProject } = useProject();
  const navigate = useNavigate();
  const projectId = propProjectId || (id ? parseInt(id, 10) : selectedProject?.id || 0);

  const [articles, setArticles] = useState<ContentArticle[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (projectId) {
      fetchArticles();
    } else {
      setLoading(false);
    }
  }, [projectId]);

  const fetchArticles = async () => {
    setLoading(true);
    try {
      const data = await contentArticleService.getArticles(projectId);
      setArticles(data);
    } catch (error) {
      console.error("Failed to load articles");
    } finally {
      setLoading(false);
    }
  };

  const handleCreateNew = () => {
    navigate(`/projects/${projectId}/content/create`);
  };

  const handleEditArticle = (article: ContentArticle) => {
    navigate(`/projects/${projectId}/content/edit/${article.id}`);
  };

  if (!projectId) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <p className="text-gray-500">Please select a project to manage articles</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      
        <>
          {/* Action Cards */}
          <div>
            <div className="grid grid-cols-2 gap-4">
              <div className="bg-white border border-gray-200 rounded-lg p-6 shadow-sm">
                <div className="flex items-start gap-4">
                  <div className="w-12 h-12 bg-gray-100 rounded-lg flex items-center justify-center flex-shrink-0">
                    <Plus className="w-6 h-6 text-gray-600" />
                  </div>
                  <div className="flex-1">
                    <h3 className="text-base font-semibold text-gray-900 mb-1">Create</h3>
                    <p className="text-sm text-gray-600 mb-4">
                      Generate high-performing AEO content in minutes.
                    </p>
                    <Button
                      onClick={handleCreateNew}
                      className="bg-gray-900 text-white hover:bg-gray-800"
                    >
                      <Plus className="w-4 h-4 mr-2" />
                      Create New Content
                    </Button>
                  </div>
                </div>
              </div>
              
              <div className="relative bg-white border border-gray-200 rounded-lg p-6 shadow-sm opacity-60 cursor-not-allowed">
                <div className="absolute -inset-0.5 bg-gradient-to-r from-purple-100 via-blue-100 to-purple-100 rounded-lg opacity-50 blur-sm pointer-events-none"></div>
                <div className="relative flex items-start gap-4">
                  <div className="w-12 h-12 bg-gray-100 rounded-lg flex items-center justify-center flex-shrink-0">
                    <Sparkles className="w-6 h-6 text-gray-400" />
                  </div>
                  <div className="flex-1">
                    <h3 className="text-base font-semibold text-gray-500 mb-1 flex items-center gap-2">
                      Optimize Content
                      <span className="px-2.5 py-1 bg-gradient-to-r from-purple-500 to-blue-500 text-white text-[10px] font-bold rounded-full shadow-sm">
                        Coming Soon 🚀
                      </span>
                    </h3>
                    <p className="text-sm text-gray-500 mb-3">
                      AI-powered content optimization will analyze your articles and suggest improvements for better AEO performance.
                    </p>
                    <div className="space-y-1.5 mb-4">
                      <div className="flex items-center gap-2 text-xs text-gray-400">
                        <div className="w-1.5 h-1.5 rounded-full bg-purple-400"></div>
                        <span>SEO & AEO Score Analysis</span>
                      </div>
                      <div className="flex items-center gap-2 text-xs text-gray-400">
                        <div className="w-1.5 h-1.5 rounded-full bg-blue-400"></div>
                        <span>Smart Citation Suggestions</span>
                      </div>
                      <div className="flex items-center gap-2 text-xs text-gray-400">
                        <div className="w-1.5 h-1.5 rounded-full bg-purple-400"></div>
                        <span>Readability Optimization</span>
                      </div>
                    </div>
                    <Button
                      disabled
                      variant="outline"
                      className="border-gray-300 text-gray-400 cursor-not-allowed opacity-50"
                    >
                      <Sparkles className="w-4 h-4 mr-2" />
                      Optimize Existing Content
                    </Button>
                  </div>
                </div>
              </div>
            </div>
          </div>
          
          {/* Articles Table */}
          <div className="bg-white rounded-lg border border-gray-200 shadow-sm">
            <div className="px-4 py-3 border-b border-gray-200 flex items-center justify-between">
              <div className="flex items-center gap-2">
                <span className="text-sm text-gray-600">
                  Showing 1-{articles.length} of {articles.length} items
                </span>
              </div>
              <button 
                onClick={fetchArticles}
                className="flex items-center gap-2 px-3 py-1.5 text-sm border border-gray-300 rounded-md hover:bg-gray-50"
              >
                <span>Refresh table</span>
              </button>
            </div>
            
            {loading ? (
              <div className="flex justify-center p-10">
                <Loader2 className="animate-spin" />
              </div>
            ) : (
              <table className="w-full text-sm">
                <thead className="bg-gray-50 border-b border-gray-200">
                  <tr>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      <input type="checkbox" className="rounded" />
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Title</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Template</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Updated</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200">
                  {articles.map((article) => (
                    <tr key={article.id} className="hover:bg-gray-50 transition-colors">
                      <td className="px-4 py-3">
                        <input type="checkbox" className="rounded" />
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-2">
                          <FileText className="w-4 h-4 text-gray-400" />
                          <span className="font-medium text-gray-900">{article.title}</span>
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <span className="px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-700">
                          Completed
                        </span>
                      </td>
                      <td className="px-4 py-3 text-gray-600">Blog Post</td>
                      <td className="px-4 py-3 text-gray-600">
                        {article.updatedAt ? new Date(article.updatedAt).toLocaleDateString() : new Date(article.createdAt!).toLocaleDateString()}
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-1">
                          <button
                            className="px-2 py-1 text-xs border border-gray-300 rounded hover:bg-gray-50 flex items-center gap-1"
                          >
                            <span className="w-2 h-2 rounded-full bg-orange-500"></span>
                            Draft
                          </button>
                          <button
                            onClick={() => handleEditArticle(article)}
                            className="p-1 hover:bg-gray-100 rounded"
                          >
                            <MoreHorizontal className="w-4 h-4 text-gray-400" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                  {articles.length === 0 && (
                    <tr>
                      <td colSpan={6} className="text-center py-8 text-gray-500">
                        No articles found. Create one to get started.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            )}
          </div>
        </>
      
    </div>
  );
};

export default ContentListing;
