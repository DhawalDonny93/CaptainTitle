import { api } from '../lib/api';

export interface Opportunity {
  id: number;
  projectId: number;
  opportunityType: string;
  source: string; // "Visibility" | "ContentGap" | "SEO"
  title: string;
  description: string;
  platform: string;
  priority: string; // "Critical" | "High" | "Medium" | "Low"
  status: string;
  estimatedImpact: number; // 0-100
  sourceUrl?: string;
  optimizationStrategy?: string;
  discoveredAt: Date;
  createdAt: Date;
}

export interface OpportunityDiscoveryRequest {
  projectId: number;
  keywords: string[];
  platforms: string[];
  maxResults?: number;
}

const opportunityService = {
  // Get opportunities for a project
  async getOpportunitiesByProject(projectId: number, status?: string): Promise<Opportunity[]> {
    const params = status ? `?status=${status}` : '';
    const response = await api.get(`/opportunities/project/${projectId}${params}`);
    return response.data;
  },

  // Discover new opportunities
  async discoverOpportunities(request: OpportunityDiscoveryRequest): Promise<Opportunity[]> {
    const response = await api.post('/opportunities/discover', request);
    return response.data;
  },

  // Update opportunity status
  async updateOpportunityStatus(opportunityId: number, status: string): Promise<Opportunity> {
    const response = await api.put(`/opportunities/${opportunityId}/status`, { status });
    return response.data;
  },

  // Get opportunity by ID
  async getOpportunityById(opportunityId: number): Promise<Opportunity> {
    const response = await api.get(`/opportunities/${opportunityId}`);
    return response.data;
  },

  // Delete opportunity
  async deleteOpportunity(opportunityId: number): Promise<void> {
    await api.delete(`/opportunities/${opportunityId}`);
  },
};

export default opportunityService;
