import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

export interface MentionsAnalytics {
  insights: {
    totalMentions: number;
    totalCitations: number;
    topPlatform: string;
    keyInsights: string[];
  };
  opportunities: any[];
  analytics: {
    performanceOverTime: {
      dates: string[];
      brandData: { [key: string]: number[] };
    };
    positionDistribution: {
      brand: string;
      positions: { [key: string]: number };
    }[];
  };
  topics: {
    name: string;
    count: number;
    status: string;
    performanceScore: number;
  }[];
  mentions: {
    prompt: string;
    mentioned: boolean;
    locations: string;
    mentions: number;
    citations: number;
    avgPosition: number | null;
    isCompetitor: boolean;
    competitors: {
      name: string;
      type: string;
    }[];
  }[];
}

export const mentionsService = {
  async getMentionsAnalytics(
    projectId: number,
    filters?: {
      platform?: string;
      topic?: string;
      startDate?: string;
      endDate?: string;
    }
  ): Promise<MentionsAnalytics> {
    const params = new URLSearchParams({
      projectId: projectId.toString(),
      ...(filters?.platform && { platform: filters.platform }),
      ...(filters?.topic && { topic: filters.topic }),
      ...(filters?.startDate && { startDate: filters.startDate }),
      ...(filters?.endDate && { endDate: filters.endDate }),
    });

    const response = await axios.get(
      `${API_BASE_URL}/citations/mentions/analytics?${params}`,
      {
        headers: {
          Authorization: `Bearer ${localStorage.getItem('token')}`,
        },
      }
    );

    return response.data;
  },
};
