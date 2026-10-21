import { api } from '../lib/api';

export interface AnalyticsOverview {
  totalVisits: number;
  totalCitations: number;
  totalOpportunities: number;
  averageVisibilityScore: number;
  periodComparison: {
    visits: number;
    citations: number;
    opportunities: number;
    visibilityScore: number;
  };
}

export interface TrafficMetrics {
  date: string;
  visits: number;
  uniqueVisitors: number;
  pageViews: number;
  bounceRate: number;
  avgSessionDuration: number;
}

export interface CitationMetrics {
  date: string;
  citationCount: number;
  citationShare: number;
  topDomains: string[];
}

export interface PerformanceMetrics {
  metric: string;
  value: number;
  change: number;
  trend: 'up' | 'down' | 'stable';
}

const analyticsService = {
  // Get analytics overview
  async getOverview(projectId: number, startDate: Date, endDate: Date): Promise<AnalyticsOverview> {
    const response = await api.get(`/analytics/overview/${projectId}`, {
      params: {
        startDate: startDate.toISOString(),
        endDate: endDate.toISOString(),
      },
    });
    return response.data;
  },

  // Get traffic metrics
  async getTrafficMetrics(projectId: number, startDate: Date, endDate: Date): Promise<TrafficMetrics[]> {
    const response = await api.get(`/analytics/traffic/${projectId}`, {
      params: {
        startDate: startDate.toISOString(),
        endDate: endDate.toISOString(),
      },
    });
    return response.data;
  },

  // Get citation metrics
  async getCitationMetrics(projectId: number, startDate: Date, endDate: Date): Promise<CitationMetrics[]> {
    const response = await api.get(`/analytics/citations/${projectId}`, {
      params: {
        startDate: startDate.toISOString(),
        endDate: endDate.toISOString(),
      },
    });
    return response.data;
  },

  // Get performance metrics
  async getPerformanceMetrics(projectId: number): Promise<PerformanceMetrics[]> {
    const response = await api.get(`/analytics/performance/${projectId}`);
    return response.data;
  },

  // Export analytics data
  async exportData(projectId: number, startDate: Date, endDate: Date, format: 'csv' | 'json' | 'pdf'): Promise<Blob> {
    const response = await api.get(`/analytics/export/${projectId}`, {
      params: {
        startDate: startDate.toISOString(),
        endDate: endDate.toISOString(),
        format,
      },
      responseType: 'blob',
    });
    return response.data;
  },
};

export default analyticsService;
