import { api } from '../lib/api';

export interface DashboardOverview {
  overallVisibilityScore: number;
  totalPrompts: number;
  totalCitations: number;
  totalOpportunities: number;
  averagePosition: number;
  trendDirection: string;
  lastUpdated: string;
  platformBreakdown: PlatformMetric[];
}

export interface PlatformMetric {
  platform: string;
  visibilityScore: number;
  mentionCount: number;
  averagePosition: number;
  change: number;
}

export interface TrendData {
  metricName: string;
  dataPoints: TrendPoint[];
}

export interface TrendPoint {
  date: Date;
  value: number;
}

export interface CompetitiveAnalysis {
  yourDomain: string;
  yourScore: number;
  competitors: CompetitorComparison[];
  marketPosition: string;
}

export interface CompetitorComparison {
  domain: string;
  visibilityScore: number;
  citationShare: number;
  rank: number;
  competitorType?: string; // 'Direct' or 'SERP'
}

export interface VisibilityGoal {
  id: number;
  goalType: string;
  targetValue: number;
  currentValue: number;
  startDate: Date;
  targetDate: Date;
  status: string;
  progress: number;
}

export interface VisibilityAlert {
  id: number;
  alertType: string;
  severity: string;
  message: string;
  createdAt: Date;
  isRead: boolean;
  isDismissed: boolean;
}

const visibilityService = {
  // Get dashboard overview
  async getDashboardOverview(projectId: number): Promise<DashboardOverview> {
    const response = await api.get(`/visibility/dashboard?projectId=${projectId}`);
    return response.data;
  },

  // Get detailed metrics
  async getMetrics(projectId: number, startDate?: Date, endDate?: Date): Promise<any> {
    const params = new URLSearchParams({ projectId: projectId.toString() });
    if (startDate) params.append('startDate', startDate.toISOString());
    if (endDate) params.append('endDate', endDate.toISOString());
    
    const response = await api.get(`/visibility/metrics?${params.toString()}`);
    return response.data;
  },

  // Get trend data
  async getTrends(projectId: number, metricType: string, days: number = 30): Promise<TrendData> {
    const response = await api.get(`/visibility/trends?projectId=${projectId}&metricType=${metricType}&days=${days}`);
    return response.data;
  },

  // Get competitive analysis
  async getCompetitiveAnalysis(projectId: number): Promise<CompetitiveAnalysis> {
    const response = await api.get(`/visibility/competitive?projectId=${projectId}`);
    return response.data;
  },

  // Get goals
  async getGoals(projectId: number): Promise<VisibilityGoal[]> {
    const response = await api.get(`/visibility/goals?projectId=${projectId}`);
    return response.data;
  },

  // Create goal
  async createGoal(projectId: number, goalData: any): Promise<VisibilityGoal> {
    const response = await api.post('/visibility/goals', { ...goalData, projectId });
    return response.data;
  },

  // Update goal
  async updateGoal(goalId: number, goalData: any): Promise<VisibilityGoal> {
    const response = await api.put(`/visibility/goals/${goalId}`, goalData);
    return response.data;
  },

  // Delete goal
  async deleteGoal(goalId: number): Promise<void> {
    await api.delete(`/visibility/goals/${goalId}`);
  },

  // Get alerts
  async getAlerts(projectId: number): Promise<VisibilityAlert[]> {
    const response = await api.get(`/visibility/alerts?projectId=${projectId}`);
    return response.data;
  },

  // Mark alert as read
  async markAlertAsRead(alertId: number): Promise<void> {
    await api.put(`/visibility/alerts/${alertId}/read`);
  },

  // Dismiss alert
  async dismissAlert(alertId: number): Promise<void> {
    await api.put(`/visibility/alerts/${alertId}/dismiss`);
  },
};

export default visibilityService;
