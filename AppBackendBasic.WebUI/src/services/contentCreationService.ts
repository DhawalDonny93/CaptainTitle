import axios from 'axios';
import type { ContentCreationData } from '../types/contentCreation';

const API_BASE_URL = import.meta.env.VITE_API_URL;

const api = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add auth token to requests
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('accessToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

const contentCreationService = {
  async getCreationData(projectId: number): Promise<ContentCreationData> {
    const response = await api.get(`/ContentArticles/Project/${projectId}/CreationData`);
    return response.data;
  },
};

export default contentCreationService;
