import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import UserDropdown from '../UserDropdown';
import ProjectSelector from '../ProjectSelector';
import NotificationDropdown from '../NotificationDropdown';
import { useProject } from '../../contexts/ProjectContext';
import {
  LayoutDashboard,
  Eye,
  PenTool,
  CheckSquare,
  BarChart3,
  Users,
  AlertCircle,
  Target,
  MessageSquare,
  FileSearch,
  Workflow,
} from 'lucide-react';

const MainLayout = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { selectedProject } = useProject();

  const isActive = (path: string) => location.pathname === path;

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Sidebar */}
      <aside className="fixed left-0 top-0 h-full w-64 bg-gray-50 border-r border-gray-200 px-2.5 py-3 flex flex-col">
        {/* Project Selector Dropdown */}
        <div className="mb-4">
          <ProjectSelector />
        </div>

        {/* Main Navigation */}
        <nav className="flex-1 overflow-y-auto">
          <div className="space-y-0.5">
            <button
              onClick={() => navigate('/dashboard')}
              className={`w-full flex items-center gap-2.5 px-2.5 py-1.5 rounded-md text-sm transition-all ${
                isActive('/dashboard')
                  ? 'bg-gray-900 text-white font-medium'
                  : 'text-gray-700 hover:bg-gray-100'
              }`}
            >
              <LayoutDashboard className="w-4 h-4" />
              <span>Overview</span>
            </button>

            <button
              onClick={() => {
                if (selectedProject?.id) {
                  navigate(`/projects/${selectedProject.id}/action-plan-workflow`);
                } else {
                  navigate('/action-plan-workflow');
                }
              }}
              className={`w-full flex items-center gap-2.5 px-2.5 py-1.5 rounded-md text-sm transition-all ${
                location.pathname.includes('/action-plan-workflow')
                  ? 'bg-gray-900 text-white font-medium'
                  : 'text-gray-700 hover:bg-gray-100'
              }`}
            >
              <Workflow className="w-4 h-4" />
              <span>Action Plan</span>
            </button>

            <button
              onClick={() => navigate('/projects')}
              className={`w-full flex items-center gap-2.5 px-2.5 py-1.5 rounded-md text-sm transition-all ${
                isActive('/projects')
                  ? 'bg-gray-900 text-white font-medium'
                  : 'text-gray-700 hover:bg-gray-100'
              }`}
            >
              <Users className="w-4 h-4" />
              <span>Agent</span>
            </button>

            <button
              onClick={() => navigate('/todo-tasks')}
              className={`w-full flex items-center gap-2.5 px-2.5 py-1.5 rounded-md text-sm transition-all ${
                isActive('/todo-tasks')
                  ? 'bg-gray-900 text-white font-medium'
                  : 'text-gray-700 hover:bg-gray-100'
              }`}
            >
              <CheckSquare className="w-4 h-4" />
              <span>Tasks</span>
            </button>

            {/* Analytics Section */}
            <div className="pt-3 pb-1">
              <p className="px-2.5 text-xs font-semibold text-gray-400 uppercase tracking-wider">
                Analytics
              </p>
            </div>

            <button
              onClick={() => {
                if (selectedProject?.id) {
                  navigate(`/projects/${selectedProject.id}/visibility`);
                } else {
                  navigate('/visibility-dashboards');
                }
              }}
              className={`w-full flex items-center gap-2.5 px-2.5 py-1.5 rounded-md text-sm transition-all ${
                isActive('/visibility-dashboards') || location.pathname.includes('/visibility')
                  ? 'bg-gray-900 text-white font-medium'
                  : 'text-gray-700 hover:bg-gray-100'
              }`}
            >
              <Eye className="w-4 h-4" />
              <span>Visibility</span>
            </button>

            <button
              onClick={() => {
                if (selectedProject?.id) {
                  navigate(`/projects/${selectedProject.id}/mentions`);
                } else {
                  navigate('/mentions');
                }
              }}
              className={`w-full flex items-center gap-2.5 px-2.5 py-1.5 rounded-md text-sm transition-all ${
                location.pathname.includes('/mentions')
                  ? 'bg-gray-900 text-white font-medium'
                  : 'text-gray-700 hover:bg-gray-100'
              }`}
            >
              <Target className="w-4 h-4" />
              <span>Mentions</span>
            </button>
            
            <button
              onClick={() => {
                if (selectedProject?.id) {
                  navigate(`/projects/${selectedProject.id}/sources`);
                } else {
                  navigate('/sources');
                }
              }}
              className={`w-full flex items-center gap-2.5 px-2.5 py-1.5 rounded-md text-sm transition-all ${
                location.pathname.includes('/sources')
                  ? 'bg-gray-900 text-white font-medium'
                  : 'text-gray-700 hover:bg-gray-100'
              }`}
            >
              <MessageSquare className="w-4 h-4" />
              <span>Sources</span>
            </button>


            {/* Actions Section */}
            <div className="pt-3 pb-1">
              <p className="px-2.5 text-xs font-semibold text-gray-400 uppercase tracking-wider">
                Actions
              </p>
            </div>

            <button
              onClick={() => {
                if (selectedProject?.id) {
                  navigate(`/projects/${selectedProject.id}/opportunities/results`);
                } else {
                  navigate('/opportunities/results');
                }
              }}
              className={`w-full flex items-center gap-2.5 px-2.5 py-1.5 rounded-md text-sm transition-all ${
                location.pathname.includes('/opportunities')
                  ? 'bg-gray-900 text-white font-medium'
                  : 'text-gray-700 hover:bg-gray-100'
              }`}
            >
              <AlertCircle className="w-4 h-4" />
              <span>Opportunities</span>
            </button>

            <button
              onClick={() => {
                if (selectedProject?.id) {
                  navigate(`/projects/${selectedProject.id}/content`);
                } else {
                  navigate('/article-generator');
                }
              }}
              className={`w-full flex items-center gap-2.5 px-2.5 py-1.5 rounded-md text-sm transition-all ${
                isActive('/article-generator') || location.pathname.includes('/content')
                  ? 'bg-gray-900 text-white font-medium'
                  : 'text-gray-700 hover:bg-gray-100'
              }`}
            >
              <PenTool className="w-4 h-4" />
              <span>Articles</span>
            </button>

            <button
              onClick={() => {
                if (selectedProject?.id) {
                  navigate(`/projects/${selectedProject.id}/audits`);
                } else {
                  navigate('/projects');
                }
              }}
              className={`w-full flex items-center gap-2.5 px-2.5 py-1.5 rounded-md text-sm transition-all ${
                location.pathname.includes('/audits')
                  ? 'bg-gray-900 text-white font-medium'
                  : 'text-gray-700 hover:bg-gray-100'
              }`}
            >
              <FileSearch className="w-4 h-4" />
              <span>Audits</span>
            </button>
            {/* Prompts Section */}
            <div className="pt-3 pb-1">
              <p className="px-2.5 text-xs font-semibold text-gray-400 uppercase tracking-wider">
                Prompts
              </p>
            </div>

            <button
              onClick={() => {
                if (selectedProject?.id) {
                  navigate(`/projects/${selectedProject.id}/your-prompts`);
                } else {
                  navigate('/your-prompts');
                }
              }}
              className={`w-full flex items-center gap-2.5 px-2.5 py-1.5 rounded-md text-sm transition-all ${
                location.pathname.includes('/your-prompts')
                  ? 'bg-gray-900 text-white font-medium'
                  : 'text-gray-700 hover:bg-gray-100'
              }`}
            >
              <PenTool className="w-4 h-4" />
              <span>Your Prompts</span>
            </button>

            <button
              onClick={(e) => {
                e.preventDefault();
                // Disabled - Coming Soon
              }}
              disabled
              className="w-full flex items-center gap-2.5 px-2.5 py-1.5 rounded-md text-sm transition-all relative group cursor-not-allowed opacity-50"
            >
              <BarChart3 className="w-4 h-4 text-gray-400" />
              <span className="text-gray-400">Prompt Research</span>
              <span className="ml-auto px-2 py-0.5 bg-gradient-to-r from-purple-100 to-blue-100 text-purple-700 text-[10px] font-bold rounded-full border border-purple-200">
                Soon
              </span>
            </button>

          </div>
        </nav>

        {/* User Dropdown at Bottom */}
        <div className="border-t border-gray-200 pt-3 mt-3">
          <UserDropdown />
        </div>
      </aside>

      {/* Main Content */}
      <main className="ml-64">
        {/* Header */}
        <header className="sticky top-0 z-40 bg-white border-b border-gray-100 px-5 py-2.5">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2.5">
              <div className="w-7 h-7 bg-black rounded flex items-center justify-center text-white font-bold text-sm">
                {selectedProject?.name?.charAt(0) || 'N'}
              </div>
              <h1 className="text-base font-semibold text-gray-900">{selectedProject?.name || 'No Project'}</h1>
            </div>
            <NotificationDropdown />
          </div>
        </header>

        {/* Page Content */}
        <div className="p-5 bg-white min-h-screen">
          <Outlet />
        </div>
      </main>
    </div>
  );
};

export default MainLayout;
