import { api } from '../lib/api';

// --- Interfaces ---

export interface Project {
  id?: number;
  name: string;
  domain?: string;
  brandVoice?: string;
  targetAudience?: string;
  cmsCredentials?: string;
  isActive?: boolean;
  createdAt?: string;
}

export interface ContentArticle {
  id?: number;
  projectId: number;
  title: string;
  slug?: string;
  contentHtml?: string;
  contentMarkdown?: string;
  metaTitle?: string;
  metaDescription?: string;
  featuredImageUrl?: string;
  status: 'Draft' | 'Scheduled' | 'Published' | 'Archived';
  wordCount?: number;
  aiModelUsed?: string;
  generationPrompt?: string;
  seoScore?: number;
  createdAt?: string;
  updatedAt?: string;
}

export interface AiTrackingPrompt {
  id?: number;
  projectId: number;
  promptText: string;
  category?: string;
  isActive: boolean;
  aiTrackingResults?: AiTrackingResult[];
  createdAt?: string;
}

export interface AiTrackingResult {
  id: number;
  promptId: number;
  aiEngine: string;
  responseText: string;
  brandMentioned: boolean;
  mentionPosition?: number;
  sentimentScore?: number;
  visibilityScore?: number;
  sentiment?: string;
  executedAt: string;
}

// --- Service ---

class AiSeoService {

  // Projects
  async getProjects(): Promise<Project[]> {
    const response = await api.get('/projects');
    const raw = response.data;
    return Array.isArray(raw) ? raw : (Array.isArray(raw?.data) ? raw.data : []);
  }

  async getProject(id: number): Promise<Project> {
    const response = await api.get(`/projects/${id}`);
    return response.data;
  }

  async createProject(project: Project): Promise<Project> {
    const response = await api.post('/projects', project);
    return response.data;
  }

  async updateProject(id: number, project: Project): Promise<void> {
    await api.put(`/projects/${id}`, project);
  }

  async deleteProject(id: number): Promise<void> {
    await api.delete(`/projects/${id}`);
  }

  // Content Articles
  async getArticlesByProject(projectId: number): Promise<ContentArticle[]> {
    const response = await api.get(`/contentarticles/project/${projectId}`);
    return response.data;
  }

  async getArticle(id: number): Promise<ContentArticle> {
    const response = await api.get(`/contentarticles/${id}`);
    return response.data;
  }

  async createArticle(article: ContentArticle | any): Promise<ContentArticle> {
    const response = await api.post('/contentarticles', article);
    return response.data;
  }

  async updateArticle(id: number, article: ContentArticle): Promise<void> {
    await api.put(`/contentarticles/${id}`, article);
  }

  async deleteArticle(id: number): Promise<void> {
    await api.delete(`/contentarticles/${id}`);
  }

  // AI Tracking Prompts
  async getPromptsByProject(projectId: number): Promise<AiTrackingPrompt[]> {
    const response = await api.get(`/aitrackingprompts/project/${projectId}`);
    return response.data;
  }

  async createPrompt(prompt: AiTrackingPrompt): Promise<AiTrackingPrompt> {
    const response = await api.post('/aitrackingprompts', prompt);
    return response.data;
  }

  async deletePrompt(id: number): Promise<void> {
    await api.delete(`/aitrackingprompts/${id}`);
  }

  // Community Opportunities
  async getOpportunitiesByProject(projectId: number): Promise<CommunityOpportunity[]> {
    const response = await api.get(`/communityopportunities/project/${projectId}`);
    return response.data;
  }

  async updateOpportunity(id: number, opportunity: CommunityOpportunity): Promise<void> {
    await api.put(`/communityopportunities/${id}`, opportunity);
  }

  // Site Audits
  async getAuditsByProject(projectId: number): Promise<SiteAudit[]> {
    const response = await api.get(`/siteaudits/project/${projectId}`);
    return response.data;
  }

  async runAudit(projectId: number): Promise<void> {
    await api.post(`/siteaudits/run/${projectId}`);
  }

  // User Tasks (Action Plan)
  async getTasksByProject(projectId: number): Promise<UserTask[]> {
    const response = await api.get(`/usertasks/project/${projectId}`);
    return response.data;
  }

  async getUserTasks(): Promise<UserTask[]> {
    const response = await api.get('/usertasks/user');
    return response.data;
  }

  async createTask(task: Partial<UserTask>): Promise<void> {
    await api.post('/usertasks', task);
  }

  async updateTask(id: number, task: Partial<UserTask>): Promise<void> {
    await api.put(`/usertasks/${id}`, task);
  }

  async completeTask(id: number): Promise<void> {
    await api.put(`/usertasks/${id}/complete`);
  }

  async deleteTask(id: number): Promise<void> {
    await api.delete(`/usertasks/${id}`);
  }

  // Notifications
  async getNotifications(): Promise<Notification[]> {
    const response = await api.get('/notifications');
    return response.data;
  }

  async markNotificationsRead(): Promise<void> {
    await api.put('/notifications/read');
  }
}

export interface UserTask {
  id: number;
  projectId: number;
  title: string;
  description?: string;
  priority: string;
  status: string;
  isCompleted: boolean;
  dueDate?: string;
  source: string;
  createdAt: string;
}

export interface Notification {
  id: number;
  userId?: string;
  message: string;
  type: string;
  isRead: boolean;
  createdAt: string;
}

export interface SiteAudit {
  id: number;
  projectId: string;
  auditType: string;
  status: string;
  details?: string;
  score: number;
  checkDate: string;
}

export interface CommunityOpportunity {
  id: number;
  projectId: string;
  platform: string;
  title: string;
  url: string;
  summary?: string;
  sentiment?: string;
  status: string;
  draftReply?: string;
  createdAt: string;
}

export const aiSeoService = new AiSeoService();
