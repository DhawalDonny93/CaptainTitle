import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { aiSeoService, Project } from '../services/aiSeoService';
import { Button } from '@/components/ui/Button';
import { ArrowLeft, PenTool, BarChart3, Settings, MessageSquare, AlertCircle, CheckSquare } from 'lucide-react';
import ContentWriter from './ContentWriter';
import AiVisibility from './AiVisibility';
import CommunityOpportunities from './CommunityOpportunities';
import SiteAudits from './SiteAudits';
import ActionPlanWorkflow from './ActionPlanWorkflow';
import { ProjectDashboardSkeleton } from '../components/Skeleton';
import { toast } from '../utils/toast';

const ProjectDashboard = () => {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const [project, setProject] = useState<Project | null>(null);
    const [loading, setLoading] = useState(true);
    const [activeTab, setActiveTab] = useState<'content' | 'visibility' | 'community' | 'audits' | 'actionplan' | 'settings'>('content');

    useEffect(() => {
        if (id) {
            const projectId = parseInt(id, 10);
            if (!isNaN(projectId)) {
                fetchProject(projectId);
            }
        }
    }, [id]);

    const fetchProject = async (projectId: number) => {
        try {
            const data = await aiSeoService.getProject(projectId);
            setProject(data);
        } catch (error) {
            console.error("Failed to fetch project", error);
            toast.error('Failed to load project');
            // navigate('/projects');
        } finally {
            setLoading(false);
        }
    };

    if (loading) return <ProjectDashboardSkeleton />;
    if (!project) return <div className="p-10 text-center">Project not found</div>;

    return (
        <div className="space-y-6">
            <div className="flex items-center gap-4">
                <Button variant="ghost" size="sm" onClick={() => navigate('/projects')}>
                    <ArrowLeft className="h-4 w-4 mr-2" /> Back
                </Button>
                <div>
                    <h1 className="text-3xl font-bold tracking-tight">{project.name}</h1>
                    <p className="text-muted-foreground">{project.domain}</p>
                </div>
            </div>

            <div className="flex flex-col gap-6">
                <div className="flex border-b overflow-x-auto">
                    <button
                        onClick={() => setActiveTab('content')}
                        className={`flex items-center gap-2 px-4 py-2 text-sm font-medium border-b-2 transition-colors ${activeTab === 'content'
                            ? 'border-primary text-primary'
                            : 'border-transparent text-muted-foreground hover:text-foreground'
                            }`}
                    >
                        <PenTool className="h-4 w-4" /> Content Writer
                    </button>
                    <button
                        onClick={() => setActiveTab('visibility')}
                        className={`flex items-center gap-2 px-4 py-2 text-sm font-medium border-b-2 transition-colors ${activeTab === 'visibility'
                            ? 'border-primary text-primary'
                            : 'border-transparent text-muted-foreground hover:text-foreground'
                            }`}
                    >
                        <BarChart3 className="h-4 w-4" /> AI Visibility
                    </button>
                    <button
                        onClick={() => setActiveTab('community')}
                        className={`flex items-center gap-2 px-4 py-2 text-sm font-medium border-b-2 transition-colors ${activeTab === 'community'
                            ? 'border-primary text-primary'
                            : 'border-transparent text-muted-foreground hover:text-foreground'
                            }`}
                    >
                        <MessageSquare className="h-4 w-4" /> Community
                    </button>
                    <button
                        onClick={() => setActiveTab('audits')}
                        className={`flex items-center gap-2 px-4 py-2 text-sm font-medium border-b-2 transition-colors ${activeTab === 'audits'
                            ? 'border-primary text-primary'
                            : 'border-transparent text-muted-foreground hover:text-foreground'
                            }`}
                    >
                        <AlertCircle className="h-4 w-4" /> Agentic Auditor
                    </button>
                    <button
                        onClick={() => setActiveTab('actionplan')}
                        className={`flex items-center gap-2 px-4 py-2 text-sm font-medium border-b-2 transition-colors ${activeTab === 'actionplan'
                            ? 'border-primary text-primary'
                            : 'border-transparent text-muted-foreground hover:text-foreground'
                            }`}
                    >
                        <CheckSquare className="h-4 w-4" /> Action Plan
                    </button>
                    <button
                        onClick={() => setActiveTab('settings')}
                        className={`flex items-center gap-2 px-4 py-2 text-sm font-medium border-b-2 transition-colors ${activeTab === 'settings'
                            ? 'border-primary text-primary'
                            : 'border-transparent text-muted-foreground hover:text-foreground'
                            }`}
                    >
                        <Settings className="h-4 w-4" /> Settings
                    </button>
                </div>

                <div className="p-4 border rounded-lg bg-card">
                    {activeTab === 'content' && <ContentWriter projectId={project.id!} />}
                    {activeTab === 'visibility' && <AiVisibility />}
                    {activeTab === 'community' && <CommunityOpportunities />}
                    {activeTab === 'audits' && <SiteAudits />}
                    {activeTab === 'actionplan' && <ActionPlanWorkflow />}
                    {activeTab === 'settings' && (
                        <div className="space-y-4">
                            <h3 className="text-lg font-medium">Project Settings</h3>
                            <p className="text-muted-foreground">Brand Voice, Target Audience, and CMS credentials configuration coming soon.</p>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};

export default ProjectDashboard;
