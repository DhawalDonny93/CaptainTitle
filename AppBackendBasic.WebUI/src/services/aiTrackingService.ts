import { api } from '../lib/api';

export interface AIInteraction {
  id: number;
  userId: number;
  projectId?: number;
  interactionType: string;
  prompt: string;
  response: string;
  model: string;
  tokensUsed: number;
  cost: number;
  duration: number;
  createdAt: Date;
}

export interface AIUsageStats {
  totalInteractions: number;
  totalTokensUsed: number;
  totalCost: number;
  averageDuration: number;
  modelBreakdown: {
    model: string;
    count: number;
    tokens: number;
    cost: number;
  }[];
  dailyUsage: {
    date: string;
    interactions: number;
    tokens: number;
    cost: number;
  }[];
}

export interface CostAnalysis {
  currentPeriodCost: number;
  previousPeriodCost: number;
  percentageChange: number;
  projectedMonthlyCost: number;
  costByModel: {
    model: string;
    cost: number;
    percentage: number;
  }[];
}

const aiTrackingService = {
  // Get AI interactions
  async getInteractions(projectId?: number, startDate?: Date, endDate?: Date): Promise<AIInteraction[]> {
    const params = new URLSearchParams();
    if (projectId) params.append('projectId', projectId.toString());
    if (startDate) params.append('startDate', startDate.toISOString());
    if (endDate) params.append('endDate', endDate.toISOString());
    
    const response = await api.get(`/aitracking/interactions?${params.toString()}`);
    return response.data;
  },

  // Get usage statistics
  async getUsageStats(projectId?: number, startDate?: Date, endDate?: Date): Promise<AIUsageStats> {
    const params = new URLSearchParams();
    if (projectId) params.append('projectId', projectId.toString());
    if (startDate) params.append('startDate', startDate.toISOString());
    if (endDate) params.append('endDate', endDate.toISOString());
    
    const response = await api.get(`/aitracking/stats?${params.toString()}`);
    return response.data;
  },

  // Get cost analysis
  async getCostAnalysis(projectId?: number): Promise<CostAnalysis> {
    const params = new URLSearchParams();
    if (projectId) params.append('projectId', projectId.toString());
    
    const response = await api.get(`/aitracking/cost-analysis?${params.toString()}`);
    return response.data;
  },

  // Get interaction by ID
  async getInteractionById(interactionId: number): Promise<AIInteraction> {
    const response = await api.get(`/aitracking/interactions/${interactionId}`);
    return response.data;
  },

  // Export tracking data
  async exportData(startDate: Date, endDate: Date, format: 'csv' | 'json'): Promise<Blob> {
    const response = await api.get('/aitracking/export', {
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

export default aiTrackingService;
