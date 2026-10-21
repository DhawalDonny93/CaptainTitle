import { useState, useEffect } from 'react';
import { aiSeoService, ContentArticle } from '../services/aiSeoService';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Loader2, Plus, FileText } from 'lucide-react';

const ContentWriter = ({ projectId }: { projectId: number }) => {
    const [articles, setArticles] = useState<ContentArticle[]>([]);
    const [loading, setLoading] = useState(true);
    const [isCreateOpen, setIsCreateOpen] = useState(false);

    // New Article State
    const [newArticle, setNewArticle] = useState<Partial<ContentArticle>>({ title: '', generationPrompt: '' });
    const [generating, setGenerating] = useState(false);

    useEffect(() => {
        fetchArticles();
    }, [projectId]);

    const fetchArticles = async () => {
        setLoading(true);
        try {
            const data = await aiSeoService.getArticlesByProject(projectId);
            setArticles(data);
        } catch (error) {
            console.error("Failed to load articles");
        } finally {
            setLoading(false);
        }
    };

    const handleGenerateArticle = async () => {
        if (!newArticle.title) return;
        setGenerating(true);
        try {
            // MVP: Just create a draft with minimal content
            await aiSeoService.createArticle({
                projectId,
                title: newArticle.title!,
                generationPrompt: newArticle.generationPrompt,
                status: 'Draft',
                contentHtml: `<p>Generated content for <strong>${newArticle.title}</strong>...</p>`,
                contentMarkdown: `Generated content for **${newArticle.title}**...`,
                wordCount: 100,
                seoScore: 0
            });

            setIsCreateOpen(false);
            setNewArticle({ title: '', generationPrompt: '' });
            fetchArticles();
        } catch (error) {
            console.error("Failed to create article");
        } finally {
            setGenerating(false);
        }
    };

    return (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <h2 className="text-2xl font-bold">Content Library</h2>
                <Button onClick={() => setIsCreateOpen(true)}>
                    <Plus className="mr-2 h-4 w-4" /> New Article
                </Button>
            </div>

            {/* Simple Modal Replacement */}
            {isCreateOpen && (
                <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
                    <div className="bg-white dark:bg-slate-900 p-6 rounded-lg shadow-lg w-full max-w-xl space-y-4">
                        <h2 className="text-xl font-bold">Generate New Article</h2>
                        <div className="space-y-2">
                            <label className="text-sm font-medium">Title / Topic</label>
                            <Input
                                value={newArticle.title}
                                onChange={(e) => setNewArticle({ ...newArticle, title: e.target.value })}
                                placeholder="e.g. Top 10 SEO Strategies for 2026"
                            />
                        </div>
                        <div className="space-y-2">
                            <label className="text-sm font-medium">Content Prompt (Optional)</label>
                            <textarea
                                className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                                value={newArticle.generationPrompt}
                                onChange={(e) => setNewArticle({ ...newArticle, generationPrompt: e.target.value })}
                                placeholder="Give specific instructions to the AI..."
                            />
                        </div>
                        <div className="flex justify-end gap-2 pt-4">
                            <Button variant="ghost" onClick={() => setIsCreateOpen(false)}>Cancel</Button>
                            <Button onClick={handleGenerateArticle} disabled={generating}>
                                {generating ? <><Loader2 className="mr-2 h-4 w-4 animate-spin" /> Generating...</> : 'Generate Draft'}
                            </Button>
                        </div>
                    </div>
                </div>
            )}

            {loading ? <Loader2 className="animate-spin" /> : (
                <div className="rounded-md border overflow-hidden">
                    <table className="w-full text-sm text-left">
                        <thead className="bg-muted/50 text-muted-foreground font-medium border-b">
                            <tr>
                                <th className="p-4">Title</th>
                                <th className="p-4">Status</th>
                                <th className="p-4">SEO Score</th>
                                <th className="p-4">Created</th>
                                <th className="p-4 text-right">Actions</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y">
                            {articles.map((article) => (
                                <tr key={article.id} className="hover:bg-muted/50 transition-colors">
                                    <td className="p-4 font-medium flex items-center gap-2">
                                        <FileText className="h-4 w-4 text-muted-foreground" />
                                        {article.title}
                                    </td>
                                    <td className="p-4">
                                        <span className={`px-2 py-1 rounded-full text-xs font-medium ${article.status === 'Published' ? 'bg-green-100 text-green-800' :
                                            article.status === 'Draft' ? 'bg-yellow-100 text-yellow-800' : 'bg-gray-100 text-gray-800'
                                            }`}>
                                            {article.status}
                                        </span>
                                    </td>
                                    <td className="p-4">{article.seoScore || '-'}/100</td>
                                    <td className="p-4">{new Date(article.createdAt!).toLocaleDateString()}</td>
                                    <td className="p-4 text-right">
                                        <Button variant="ghost" className="h-8 px-2">Edit</Button>
                                    </td>
                                </tr>
                            ))}
                            {articles.length === 0 && (
                                <tr>
                                    <td colSpan={5} className="text-center py-8 text-muted-foreground">
                                        No articles found. Generate one to get started.
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            )}
        </div>
    );
};

export default ContentWriter;
