import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5049';

export interface Prompt {
  id: number;
  projectId: number;
  promptText: string;
  topic?: string;
  status: string;
  isCompetitor: boolean;
  isActive: boolean;
  userId: number;
  lastCheckedAt?: string;
  averageVisibilityScore?: number;
  createdAt: string;
  updatedAt: string;
  platformTrackings?: PlatformTracking[];
}

export interface PlatformTracking {
  id: number;
  promptId: number;
  platform: string;
  isEnabled: boolean;
  lastCheckDate?: string;
  currentVisibilityScore?: number;
  brandMentioned?: boolean;
  mentionPosition?: number;
  responseSummary?: string;
  visibilityMetrics?: VisibilityMetric[];
}

export interface VisibilityMetric {
  id: number;
  promptPlatformId: number;
  checkDate: string;
  visibilityScore: number;
  position?: number;
  brandMentioned: boolean;
  sentiment: string;
  sentimentScore?: number;
  responseText?: string;
}

export interface PromptResearchGroup {
  topic: string;
  promptCount: number;
  prompts: ResearchPrompt[];
}

export interface ResearchPrompt {
  id: number;
  text: string;
  estimatedVolume?: number;
  difficulty: string;
  relevanceScore?: number;
  discoveredAt: string;
}

export interface CreatePromptRequest {
  projectId: number;
  promptText: string;
  topic?: string;
  platforms?: string[];
  isCompetitor?: boolean;
}

export interface UpdatePromptRequest {
  promptText: string;
  topic?: string;
  status: string;
}

export interface BulkActionRequest {
  action: string;
  promptIds: number[];
  status?: string;
}

class PromptService {
  private getAuthHeaders() {
    const token = localStorage.getItem('accessToken');
    return {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    };
  }

  async getPrompts(projectId: number, status?: string, topic?: string, isActive?: boolean, isCompetitor?: boolean): Promise<Prompt[]> {
    const params: any = { projectId };
    if (status) params.status = status;
    if (topic) params.topic = topic;
    if (isActive !== undefined) params.isActive = isActive;
    if (isCompetitor !== undefined) params.isCompetitor = isCompetitor;

    const response = await axios.get(`${API_URL}/prompts?${new URLSearchParams(params).toString()}`, this.getAuthHeaders());
    return response.data;
  }

  async getPromptById(id: number): Promise<Prompt> {
    const response = await axios.get(`${API_URL}/prompts/${id}`, this.getAuthHeaders());
    return response.data;
  }

  async createPrompt(request: CreatePromptRequest): Promise<Prompt> {
    const response = await axios.post(`${API_URL}/prompts`, request, this.getAuthHeaders());
    return response.data;
  }

  async updatePrompt(id: number, request: UpdatePromptRequest): Promise<Prompt> {
    const response = await axios.put(`${API_URL}/prompts/${id}`, request, this.getAuthHeaders());
    return response.data;
  }

  async deletePrompt(id: number): Promise<void> {
    await axios.delete(`${API_URL}/prompts/${id}`, this.getAuthHeaders());
  }

  async searchPrompts(projectId: number, searchTerm: string): Promise<Prompt[]> {
    const params = new URLSearchParams();
    params.append('projectId', projectId.toString());
    params.append('searchTerm', searchTerm);

    const response = await axios.get(`${API_URL}/prompts/search?${params.toString()}`, this.getAuthHeaders());
    return response.data;
  }

  async bulkAction(request: BulkActionRequest): Promise<{ success: boolean }> {
    const response = await axios.post(`${API_URL}/prompts/bulk-action`, request, this.getAuthHeaders());
    return response.data;
  }

  async checkVisibility(id: number): Promise<{ message: string }> {
    const response = await axios.post(`${API_URL}/prompts/${id}/check-visibility`, {}, this.getAuthHeaders());
    return response.data;
  }

  async getResearchData(topic?: string, limit: number = 100): Promise<PromptResearchGroup[]> {
    const params = new URLSearchParams();
    if (topic) params.append('topic', topic);
    params.append('limit', limit.toString());

    const response = await axios.get(`${API_URL}/prompts/research?${params.toString()}`, this.getAuthHeaders());
    return response.data;
  }
}

export const promptService = new PromptService();
