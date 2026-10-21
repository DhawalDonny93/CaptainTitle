import { api } from '../lib/api';

export interface CommunityOpportunity {
  id: number;
  projectId: number;
  keywordId?: number;
  platform: string;
  title: string;
  url: string;
  summary?: string;
  sentiment?: string;
  status: 'New' | 'Drafted' | 'Posted' | 'Ignored';
  draftReply?: string;
  createdAt: Date;
  priority: number;
  engagementScore: number;
  upvotes: number;
  comments: number;
  views: number;
  postedAt?: Date;
  lastCheckedAt?: Date;
  isArchived: boolean;
  assignedToUserId?: number;
}

export interface DiscoverOpportunitiesRequest {
  projectId: number;
  platforms?: string[];
  keywords?: string[];
  minRelevanceScore?: number;
}

export interface UpdateOpportunityRequest {
  status?: 'New' | 'Drafted' | 'Posted' | 'Ignored';
  draftReply?: string;
  priority?: number;
  isArchived?: boolean;
}

const communityOpportunityService = {
  // Get opportunities for a project
  async getOpportunities(projectId: number, status?: string): Promise<CommunityOpportunity[]> {
    const params = new URLSearchParams();
    if (status) params.append('status', status);
    
    const response = await api.get(`/communityopportunities/project/${projectId}?${params.toString()}`);
    return response.data;
  },

  // Get opportunity by ID
  async getOpportunityById(opportunityId: number): Promise<CommunityOpportunity> {
    const response = await api.get(`/communityopportunities/${opportunityId}`);
    return response.data;
  },

  // Discover new opportunities
  async discoverOpportunities(request: DiscoverOpportunitiesRequest): Promise<CommunityOpportunity[]> {
    const response = await api.post('/communityopportunities/discover', request);
    return response.data;
  },

  // Update opportunity status
  async updateOpportunity(opportunityId: number, request: UpdateOpportunityRequest): Promise<CommunityOpportunity> {
    const response = await api.put(`/communityopportunities/${opportunityId}`, request);
    return response.data;
  },

  // Delete opportunity
  async deleteOpportunity(opportunityId: number): Promise<void> {
    await api.delete(`/communityopportunities/${opportunityId}`);
  },

  // Get platform statistics
  async getPlatformStats(projectId: number): Promise<any> {
    const response = await api.get(`/communityopportunities/stats/${projectId}`);
    return response.data;
  },
};

export default communityOpportunityService;
