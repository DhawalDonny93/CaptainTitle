import React, { useState } from 'react';
import { aiSeoService, Project } from '../services/aiSeoService';
import { useProject } from '../contexts/ProjectContext';
import { useSubscription } from '../contexts/SubscriptionContext';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Loader2, Plus, Trash2, Lock } from 'lucide-react';
import { toast } from '../utils/toast';

const Projects = () => {
    const { projects, loading, refreshProjects } = useProject();
    const { canCreateProject, projectLimit, openUpgradeModal, currentPlan } = useSubscription();
    const [isCreateOpen, setIsCreateOpen] = useState(false);
    const [newProject, setNewProject] = useState<Project>({ name: '', domain: '' });
    const [creating, setCreating] = useState(false);

    const atProjectLimit = !canCreateProject(projects.length);
    const limitLabel = projectLimit === -1 ? 'Unlimited' : projectLimit;

    const handleNewProjectClick = () => {
        if (atProjectLimit) {
            openUpgradeModal(
                `You've reached the ${limitLabel}-project limit on your ${currentPlan?.displayName ?? 'current'} plan. Upgrade to add more projects.`
            );
            return;
        }
        setIsCreateOpen(true);
    };

    const handleCreateProject = async () => {
        if (!newProject.name) {
            toast.warning('Please enter a project name');
            return;
        }
        // Double-check limit before submitting
        if (atProjectLimit) {
            openUpgradeModal(`You've reached the ${limitLabel}-project limit on your ${currentPlan?.displayName ?? 'current'} plan.`);
            return;
        }
        setCreating(true);
        try {
            await aiSeoService.createProject(newProject);
            toast.success(`Project "${newProject.name}" created successfully!`);
            setIsCreateOpen(false);
            setNewProject({ name: '', domain: '' });
            await refreshProjects();
        } catch (error: any) {
            console.error("Failed to create project");
            if (error?.response?.status === 402 || error?.response?.data?.code === 'QUOTA_EXCEEDED') {
                openUpgradeModal('You have reached your project limit. Please upgrade your plan.');
            } else {
                toast.error('Failed to create project. Please try again.');
            }
        } finally {
            setCreating(false);
        }
    };

    const handleDeleteProject = async (id: number, e: React.MouseEvent) => {
        e.stopPropagation();
        if (!confirm("Are you sure you want to delete this project?")) return;

        try {
            await aiSeoService.deleteProject(id);
            await refreshProjects();
        } catch (error) {
            console.error("Failed to delete project");
        }
    }

    if (loading) return <div className="flex justify-center p-10"><Loader2 className="animate-spin" /></div>;

    return (
        <div className="container mx-auto p-6 space-y-6">
            <div className="flex justify-between items-center">
                <div>
                    <h1 className="text-3xl font-bold tracking-tight">Projects</h1>
                    <p className="text-muted-foreground">
                        Manage your SEO workspaces
                        {projectLimit !== -1 && (
                            <span className={`ml-2 text-xs font-medium px-2 py-0.5 rounded-full ${
                                atProjectLimit
                                    ? 'bg-red-100 text-red-700'
                                    : 'bg-gray-100 text-gray-600'
                            }`}>
                                {projects.length} / {limitLabel} projects
                            </span>
                        )}
                    </p>
                </div>
                <Button
                    onClick={handleNewProjectClick}
                    variant={atProjectLimit ? 'outline' : 'default'}
                    className={atProjectLimit ? 'border-amber-400 text-amber-700 hover:bg-amber-50' : ''}
                >
                    {atProjectLimit
                        ? <><Lock className="mr-2 h-4 w-4" /> Upgrade to Add More</>
                        : <><Plus className="mr-2 h-4 w-4" /> New Project</>
                    }
                </Button>
            </div>

            {/* Simple Modal Replacement */}
            {isCreateOpen && (
                <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
                    <div className="bg-white dark:bg-slate-900 p-6 rounded-lg shadow-lg w-full max-w-md space-y-4">
                        <h2 className="text-xl font-bold">Create New Project</h2>
                        <div className="space-y-2">
                            <label htmlFor="name" className="text-sm font-medium">Project Name</label>
                            <Input
                                id="name"
                                value={newProject.name}
                                onChange={(e) => setNewProject({ ...newProject, name: e.target.value })}
                                placeholder="My Awesome Brand"
                            />
                        </div>
                        <div className="space-y-2">
                            <label htmlFor="domain" className="text-sm font-medium">Domain (Optional)</label>
                            <Input
                                id="domain"
                                value={newProject.domain}
                                onChange={(e) => setNewProject({ ...newProject, domain: e.target.value })}
                                placeholder="example.com"
                            />
                        </div>
                        <div className="flex justify-end gap-2 pt-4">
                            <Button variant="ghost" onClick={() => setIsCreateOpen(false)}>Cancel</Button>
                            <Button onClick={handleCreateProject} disabled={creating}>
                                {creating && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                                Create
                            </Button>
                        </div>
                    </div>
                </div>
            )}

            <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
                {projects.map((project) => (
                    <div
                        key={project.id}
                        className="bg-card text-card-foreground rounded-xl border shadow cursor-pointer hover:border-primary transition-colors p-6">
                        <div className="flex flex-row items-center justify-between space-y-0 pb-2 mb-4">
                            <h3 className="text-xl font-medium leading-none tracking-tight">{project.name}</h3>
                            <Button variant="ghost" className="h-8 w-8 p-0" onClick={(e) => handleDeleteProject(project.id!, e)}>
                                <Trash2 className="h-4 w-4 text-muted-foreground hover:text-destructive" />
                            </Button>
                        </div>
                        <div className="text-sm text-muted-foreground mb-4">
                            {project.domain || "No domain configured"}
                        </div>
                    </div>
                ))}

                {projects.length === 0 && (
                    <div className="col-span-full text-center py-12 text-muted-foreground border-2 border-dashed rounded-lg bg-slate-50 dark:bg-slate-900/50">
                        <p>No projects yet. Create one to get started!</p>
                    </div>
                )}
            </div>
        </div>
    );
};

export default Projects;
