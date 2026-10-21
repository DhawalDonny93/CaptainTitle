import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5049';

const api = axios.create({
  baseURL: API_URL,
  withCredentials: true,
});

// Add request interceptor to include auth token
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export interface BrandProfile {
  id?: number;
  projectId: number;
  overview?: string;
  description?: string;
  topics?: string[];
  logoUrl?: string;
}

export interface Competitor {
  id: number;
  name: string;
  domain: string;
  logoUrl?: string;
  createdAt: string;
}

export const brandProfileService = {
  async getBrandProfile(projectId: number): Promise<BrandProfile> {
    const response = await api.get(`/brandprofile/${projectId}`);
    return response.data;
  },

  async saveBrandProfile(projectId: number, data: Partial<BrandProfile>): Promise<BrandProfile> {
    const response = await api.post(`/brandprofile/${projectId}`, data);
    return response.data;
  },

  async uploadLogo(projectId: number, file: File): Promise<{ logoUrl: string }> {
    const formData = new FormData();
    formData.append('file', file);
    const response = await api.post(`/brandprofile/${projectId}/logo`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
    return response.data;
  },

  async getCompetitors(projectId: number): Promise<Competitor[]> {
    const response = await api.get(`/brandprofile/${projectId}/competitors`);
    return response.data;
  },

  async addCompetitor(projectId: number, domain: string): Promise<Competitor> {
    const response = await api.post(`/brandprofile/${projectId}/competitors`, { domain });
    return response.data;
  },

  async deleteCompetitor(competitorId: number): Promise<void> {
    await api.delete(`/brandprofile/competitors/${competitorId}`);
  }
};
