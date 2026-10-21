import { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { Project, aiSeoService } from '../services/aiSeoService';
import { useAuth } from './AuthContext';

interface ProjectContextType {
  projects: Project[];
  selectedProject: Project | null;
  setSelectedProject: (project: Project | null) => void;
  refreshProjects: () => Promise<void>;
  loading: boolean;
}

const ProjectContext = createContext<ProjectContextType | undefined>(undefined);

export const ProjectProvider = ({ children }: { children: ReactNode }) => {
  const { isAuthenticated } = useAuth();
  const [projects, setProjects] = useState<Project[]>([]);
  const [selectedProject, setSelectedProjectState] = useState<Project | null>(null);
  const [loading, setLoading] = useState(true);
  const [lastFetchTime, setLastFetchTime] = useState<number>(0);
  const [isFetching, setIsFetching] = useState(false);

  // Cache duration: 30 seconds to prevent excessive API calls
  const CACHE_DURATION = 30000;

  useEffect(() => {
    if (!isAuthenticated) {
      setProjects([]);
      setSelectedProjectState(null);
      setLoading(false);
      return;
    }
    initializeProjects();
  }, [isAuthenticated]);

  const initializeProjects = async () => {
    // Prevent multiple simultaneous fetches
    if (isFetching) {
      console.log('Projects fetch already in progress, skipping...');
      return;
    }

    // Check cache validity
    const now = Date.now();
    if (projects.length > 0 && (now - lastFetchTime) < CACHE_DURATION) {
      console.log('Using cached projects data');
      setLoading(false);
      return;
    }

    setLoading(true);
    setIsFetching(true);
    try {
      console.log('Fetching projects from API...');
      const raw = await aiSeoService.getProjects();
      const data: Project[] = Array.isArray(raw) ? raw : [];
      setProjects(data);
      setLastFetchTime(Date.now());

      // Check if there's a saved project ID in session storage
      const savedProjectId = sessionStorage.getItem('selectedProjectId');

      if (savedProjectId && data.length > 0) {
        // Try to find the saved project
        const savedProject = data.find(p => p.id?.toString() === savedProjectId);
        if (savedProject) {
          setSelectedProjectState(savedProject);
        } else if (data.length > 0) {
          // If saved project not found, select first project
          setSelectedProjectState(data[0]);
          sessionStorage.setItem('selectedProjectId', data[0].id?.toString() || '');
        }
      } else if (data.length > 0) {
        // No saved project, select first one
        setSelectedProjectState(data[0]);
        sessionStorage.setItem('selectedProjectId', data[0].id?.toString() || '');
      }
    } catch (error: any) {
      console.error('Failed to fetch projects:', error);
      // If rate limited, show helpful message
      if (error.response?.status === 429) {
        console.warn('Rate limit exceeded. Using cached data if available.');
        // Don't clear existing projects on rate limit error
      }
    } finally {
      setLoading(false);
      setIsFetching(false);
    }
  };

  const setSelectedProject = (project: Project | null) => {
    setSelectedProjectState(project);
    if (project?.id) {
      sessionStorage.setItem('selectedProjectId', project.id.toString());
    } else {
      sessionStorage.removeItem('selectedProjectId');
    }
  };

  const refreshProjects = async () => {
    // Prevent multiple simultaneous fetches
    if (isFetching) {
      console.log('Projects fetch already in progress, skipping refresh...');
      return;
    }

    // Check if we should respect cache (unless explicitly refreshing)
    const now = Date.now();
    if ((now - lastFetchTime) < 5000) {
      console.log('Refresh called too soon after last fetch, using cache');
      return;
    }

    setIsFetching(true);
    try {
      console.log('Refreshing projects from API...');
      const raw = await aiSeoService.getProjects();
      const data: Project[] = Array.isArray(raw) ? raw : [];
      setProjects(data);
      setLastFetchTime(Date.now());

      // Maintain current selection if it still exists
      if (selectedProject) {
        const currentProject = data.find(p => p.id === selectedProject.id);
        if (currentProject) {
          setSelectedProjectState(currentProject);
        } else if (data.length > 0) {
          setSelectedProject(data[0]);
        }
      } else if (data.length > 0) {
        setSelectedProject(data[0]);
      }
    } catch (error: any) {
      console.error('Failed to refresh projects:', error);
      // If rate limited, show helpful message
      if (error.response?.status === 429) {
        console.warn('Rate limit exceeded during refresh. Please wait a moment before trying again.');
      }
    } finally {
      setIsFetching(false);
    }
  };

  return (
    <ProjectContext.Provider
      value={{
        projects,
        selectedProject,
        setSelectedProject,
        refreshProjects,
        loading,
      }}
    >
      {children}
    </ProjectContext.Provider>
  );
};

export const useProject = () => {
  const context = useContext(ProjectContext);
  if (context === undefined) {
    throw new Error('useProject must be used within a ProjectProvider');
  }
  return context;
};
