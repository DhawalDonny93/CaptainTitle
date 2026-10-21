import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

export interface SourcesAnalytics {
  insights: {
    topSource: string;
    topSourceCitations: number;
    secondSource: string;
    secondSourcePercentage: number;
    thirdSource: string;
    thirdSourcePercentage: number;
  };
  opportunities: any[];
  analytics: {
    sources: {
      dates: string[];
      you: number[];
      competitors: number[];
      unattributed: number[];
    };
    brandSources: {
      domain: string;
      usage: number;
      lastSeen: string;
    }[];
  };
  sources: {
    rank: number;
    domain: string;
    contentType: string;
    usage: number;
    avgCitations: number;
    popularArticleTypes: string[];
    sourceTypes: string[];
    platforms: string[];
  }[];
  contentTypeDistribution: {
    type: string;
    percentage: number;
    count: number;
  }[];
}

export const sourcesService = {
  async getSourcesAnalytics(
    projectId: number,
    filters?: {
      platform?: string;
      topic?: string;
      startDate?: string;
      endDate?: string;
    }
  ): Promise<SourcesAnalytics> {
    const params = new URLSearchParams({
      projectId: projectId.toString(),
      ...(filters?.platform && { platform: filters.platform }),
      ...(filters?.topic && { topic: filters.topic }),
      ...(filters?.startDate && { startDate: filters.startDate }),
      ...(filters?.endDate && { endDate: filters.endDate }),
    });

    const response = await axios.get(
      `${API_BASE_URL}/citations/sources/analytics?${params}`,
      {
        headers: {
          Authorization: `Bearer ${localStorage.getItem('token')}`,
        },
      }
    );

    return response.data;
  },
};
