import { api } from '../lib/api';

export interface Citation {
  id: number;
  promptId: number;
  citationType: string;
  citedUrl: string;
  citedText: string;
  position: number;
  context: string;
  sentiment: string;
  isVerified: boolean;
  isCompetitor: boolean;
  competitorId?: number;
  competitorType?: string; // 'Direct' or 'SERP'
  detectedAt: Date;
  createdAt: Date;
}

export interface CitationStats {
  totalCitations: number;
  verifiedCitations: number;
  averagePosition: number;
  sentimentBreakdown: {
    positive: number;
    neutral: number;
    negative: number;
  };
}

export interface BrandMention {
  id: number;
  projectId: number;
  platform: string;
  query: string;
  isMentioned: boolean;
  position?: number;
  snippet?: string;
  isCompetitor: boolean;
  competitorId?: number;
  competitorType?: string; // 'Direct' or 'SERP'
  detectedAt: Date;
}

export interface TopDomain {
  domain: string;
  citationCount: number;
  averagePosition: number;
  isCompetitor: boolean;
  competitorType?: string;
}

const citationService = {
  // Get citations for a project
  async getCitations(
    projectId: number, 
    platform?: string, 
    startDate?: Date, 
    endDate?: Date
  ): Promise<Citation[]> {
    const params = new URLSearchParams({ projectId: projectId.toString() });
    if (platform) params.append('platform', platform);
    if (startDate) params.append('startDate', startDate.toISOString());
    if (endDate) params.append('endDate', endDate.toISOString());
    
    const response = await api.get(`/citations?${params.toString()}`);
    return response.data;
  },

  // Get citation by ID
  async getCitationById(id: number): Promise<Citation> {
    const response = await api.get(`/citations/${id}`);
    return response.data;
  },

  // Get citation statistics
  async getCitationStats(
    projectId: number, 
    startDate?: Date, 
    endDate?: Date
  ): Promise<CitationStats> {
    const params = new URLSearchParams({ projectId: projectId.toString() });
    if (startDate) params.append('startDate', startDate.toISOString());
    if (endDate) params.append('endDate', endDate.toISOString());
    
    const response = await api.get(`/citations/stats?${params.toString()}`);
    return response.data;
  },

  // Get brand mentions
  async getBrandMentions(
    projectId: number,
    platform?: string,
    isMentioned?: boolean,
    startDate?: Date,
    endDate?: Date
  ): Promise<BrandMention[]> {
    const params = new URLSearchParams({ projectId: projectId.toString() });
    if (platform) params.append('platform', platform);
    if (isMentioned !== undefined) params.append('isMentioned', isMentioned.toString());
    if (startDate) params.append('startDate', startDate.toISOString());
    if (endDate) params.append('endDate', endDate.toISOString());
    
    const response = await api.get(`/citations/mentions?${params.toString()}`);
    return response.data;
  },

  // Get brand mention statistics
  async getBrandMentionStats(
    projectId: number,
    startDate?: Date,
    endDate?: Date
  ): Promise<any> {
    const params = new URLSearchParams({ projectId: projectId.toString() });
    if (startDate) params.append('startDate', startDate.toISOString());
    if (endDate) params.append('endDate', endDate.toISOString());
    
    const response = await api.get(`/citations/mentions/stats?${params.toString()}`);
    return response.data;
  },

  // Get top cited domains
  async getTopCitedDomains(
    projectId: number,
    limit: number = 10,
    startDate?: Date,
    endDate?: Date
  ): Promise<TopDomain[]> {
    const params = new URLSearchParams({ 
      projectId: projectId.toString(),
      limit: limit.toString()
    });
    if (startDate) params.append('startDate', startDate.toISOString());
    if (endDate) params.append('endDate', endDate.toISOString());
    
    const response = await api.get(`/citations/top-domains?${params.toString()}`);
    return response.data;
  },

  // Get detailed citation data for a specific domain
  async getCitationDetailsByDomain(
    projectId: number,
    domain: string,
    startDate?: Date,
    endDate?: Date
  ): Promise<CitationDetails> {
    const params = new URLSearchParams({ 
      projectId: projectId.toString(),
      domain: domain
    });
    if (startDate) params.append('startDate', startDate.toISOString());
    if (endDate) params.append('endDate', endDate.toISOString());
    
    const response = await api.get(`/citations/domain-details?${params.toString()}`);
    return response.data;
  },
};

export interface CitationDetails {
  domain: string;
  rank: number;
  citationShare: number;
  citationShareChange: number;
  globalShare: number;
  globalShareChange: number;
  totalCitations: number;
  pages: CitationPage[];
  prompts: CitationPrompt[];
  topics: CitationTopic[];
  platforms: CitationPlatform[];
  regions: CitationRegion[];
  shareHistory: ShareDataPoint[];
}

export interface CitationPage {
  rank: number;
  url: string;
  title: string;
  share: number;
  shareChange: number;
  globalShare: number;
  globalShareChange: number;
  lastSeen: string;
}

export interface CitationPrompt {
  id: number;
  promptText: string;
  citationCount: number;
  averagePosition: number;
  lastSeen: string;
}

export interface CitationTopic {
  topic: string;
  citationCount: number;
  share: number;
}

export interface CitationPlatform {
  platform: string;
  citationCount: number;
  averagePosition: number;
  share: number;
}

export interface CitationRegion {
  region: string;
  citationCount: number;
  share: number;
}

export interface ShareDataPoint {
  date: string;
  share: number;
  globalShare: number;
}

export default citationService;
