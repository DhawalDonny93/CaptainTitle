import { useState, useRef, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { ChevronDown, Search, Plus, Check } from 'lucide-react';
import { useProject } from '../contexts/ProjectContext';
import { Project } from '../services/aiSeoService';

const ProjectSelector = () => {
  const { projects, selectedProject, setSelectedProject } = useProject();
  const [isOpen, setIsOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const dropdownRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
        setSearchQuery('');
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const filteredProjects = projects.filter(project =>
    project.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    project.domain?.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const handleProjectSelect = (project: Project) => {
    setSelectedProject(project);
    setIsOpen(false);
    setSearchQuery('');
    
    // Navigate to the same page but with the new project context
    if (location.pathname.includes('/visibility')) {
      navigate(`/projects/${project.id}/visibility`);
    } else if (location.pathname.includes('/content') || location.pathname.includes('/article-generator')) {
      navigate(`/projects/${project.id}/content`);
    } else if (location.pathname.includes('/action-plan') || location.pathname.includes('/action-plans')) {
      navigate(`/projects/${project.id}/action-plan`);
    } else if (location.pathname === '/dashboard' || location.pathname === '/') {
      navigate(`/projects/${project.id}`);
    } else {
      navigate(`/projects/${project.id}`);
    }
  };

  const handleAddProject = () => {
    setIsOpen(false);
    navigate('/projects');
  };

  const getProjectIcon = (project: Project) => {
    const initial = project.name.charAt(0).toUpperCase();
    return (
      <div className="w-8 h-8 bg-gradient-to-br from-blue-500 to-purple-500 rounded-lg flex items-center justify-center text-white text-sm font-medium">
        {initial}
      </div>
    );
  };

  return (
    <div className="relative" ref={dropdownRef}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="w-full flex items-center gap-2 px-3 py-2 hover:bg-gray-50 rounded-lg transition-all"
      >
        {selectedProject ? getProjectIcon(selectedProject) : (
          <div className="w-8 h-8 bg-gray-200 rounded-lg flex items-center justify-center">
            <Search className="w-4 h-4 text-gray-500" />
          </div>
        )}
        <div className="flex-1 text-left">
          <p className="text-sm font-medium text-gray-900 truncate">
            {selectedProject?.name || 'Select Project'}
          </p>
          {selectedProject?.domain && (
            <p className="text-xs text-gray-500 truncate">{selectedProject.domain}</p>
          )}
        </div>
        <ChevronDown className={`w-4 h-4 text-gray-500 transition-transform ${isOpen ? 'rotate-180' : ''}`} />
      </button>

      {isOpen && (
        <div className="absolute top-full left-0 right-0 mt-2 bg-white rounded-xl shadow-lg border border-gray-100 py-2 z-50 max-h-96 overflow-hidden flex flex-col">
          {/* Search Input */}
          <div className="px-3 pb-2">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search"
                className="w-full pl-9 pr-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-gray-900"
                autoFocus
              />
            </div>
          </div>

          {/* Projects List */}
          <div className="overflow-y-auto flex-1">
            {filteredProjects.length > 0 ? (
              <>
                <div className="px-3 py-1">
                  <p className="text-xs font-medium text-gray-500 uppercase tracking-wider">Your Domains</p>
                </div>
                {filteredProjects.map((project) => (
                  <button
                    key={project.id}
                    onClick={() => handleProjectSelect(project)}
                    className="w-full flex items-center gap-3 px-3 py-2 hover:bg-gray-50 transition-all"
                  >
                    {getProjectIcon(project)}
                    <div className="flex-1 text-left">
                      <p className="text-sm font-medium text-gray-900">{project.name}</p>
                      {project.domain && (
                        <p className="text-xs text-gray-500">{project.domain}</p>
                      )}
                    </div>
                    {selectedProject?.id === project.id && (
                      <Check className="w-4 h-4 text-green-600" />
                    )}
                  </button>
                ))}
              </>
            ) : (
              <div className="px-3 py-8 text-center text-sm text-gray-500">
                No projects found
              </div>
            )}
          </div>

          {/* Add New Domain */}
          <div className="border-t border-gray-100 pt-2 px-3">
            <button
              onClick={handleAddProject}
              className="w-full flex items-center gap-2 px-3 py-2 text-sm text-blue-600 hover:bg-blue-50 rounded-lg transition-all"
            >
              <Plus className="w-4 h-4" />
              <span>Add new domain</span>
            </button>
          </div>

          {/* Visibility Link (as shown in the image) */}
          <div className="px-3 pb-1">
            <button
              onClick={() => {
                setIsOpen(false);
                if (selectedProject?.id) {
                  navigate(`/projects/${selectedProject.id}/visibility`);
                } else {
                  navigate('/visibility-dashboards');
                }
              }}
              className="w-full flex items-center gap-2 px-3 py-2 text-sm text-gray-700 hover:bg-gray-50 rounded-lg transition-all"
            >
              <div className="w-4 h-4 flex items-center justify-center">
                <div className="w-3 h-3 rounded-full bg-gray-300"></div>
              </div>
              <span>Visibility</span>
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default ProjectSelector;
