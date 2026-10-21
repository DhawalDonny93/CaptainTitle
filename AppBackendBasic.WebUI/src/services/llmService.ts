import { api } from '../lib/api';

export interface LLMProvider {
  id: number;
  userId: number;
  providerName: string;
  apiKeyHash: string;
  isActive: boolean;
  createdAt: Date;
  lastUsedAt?: Date;
}

export interface CreateLLMProviderRequest {
  providerName: string;
  apiKey: string;
}

export interface TestLLMConnectionRequest {
  providerId: number;
}

export interface LLMPromptTemplate {
  id: number;
  userId: number;
  name: string;
  description?: string;
  template: string;
  category: string;
  isPublic: boolean;
  usageCount: number;
  createdAt: Date;
  updatedAt: Date;
}

export interface CreatePromptTemplateRequest {
  name: string;
  description?: string;
  template: string;
  category: string;
  isPublic?: boolean;
}

export interface UpdatePromptTemplateRequest {
  name?: string;
  description?: string;
  template?: string;
  category?: string;
  isPublic?: boolean;
}

const llmService = {
  // Get user's LLM providers
  async getUserProviders(): Promise<LLMProvider[]> {
    const response = await api.get('/llm/providers');
    return response.data;
  },

  // Add new LLM provider
  async addProvider(request: CreateLLMProviderRequest): Promise<LLMProvider> {
    const response = await api.post('/llm/providers', request);
    return response.data;
  },

  // Update LLM provider
  async updateProvider(providerId: number, data: any): Promise<LLMProvider> {
    const response = await api.put(`/llm/providers/${providerId}`, data);
    return response.data;
  },

  // Delete LLM provider
  async deleteProvider(providerId: number): Promise<void> {
    await api.delete(`/llm/providers/${providerId}`);
  },

  // Test LLM connection
  async testConnection(providerId: number): Promise<{ success: boolean; message: string }> {
    const response = await api.post(`/llm/providers/${providerId}/test`);
    return response.data;
  },

  // Get provider usage stats
  async getProviderStats(providerId: number): Promise<any> {
    const response = await api.get(`/llm/providers/${providerId}/stats`);
    return response.data;
  },

  // Get all LLM providers (admin)
  async getAllProviders(): Promise<LLMProvider[]> {
    const response = await api.get('/llmproviders');
    return response.data;
  },

  // Get provider by ID
  async getProviderById(providerId: number): Promise<LLMProvider> {
    const response = await api.get(`/llmproviders/${providerId}`);
    return response.data;
  },

  // ========== Prompt Templates ==========
  
  // Get all prompt templates
  async getPromptTemplates(category?: string): Promise<LLMPromptTemplate[]> {
    const params = category ? `?category=${category}` : '';
    const response = await api.get(`/llmprompttemplate${params}`);
    return response.data;
  },

  // Get prompt template by ID
  async getPromptTemplateById(templateId: number): Promise<LLMPromptTemplate> {
    const response = await api.get(`/llmprompttemplate/${templateId}`);
    return response.data;
  },

  // Create prompt template
  async createPromptTemplate(request: CreatePromptTemplateRequest): Promise<LLMPromptTemplate> {
    const response = await api.post('/llmprompttemplate', request);
    return response.data;
  },

  // Update prompt template
  async updatePromptTemplate(templateId: number, request: UpdatePromptTemplateRequest): Promise<LLMPromptTemplate> {
    const response = await api.put(`/llmprompttemplate/${templateId}`, request);
    return response.data;
  },

  // Delete prompt template
  async deletePromptTemplate(templateId: number): Promise<void> {
    await api.delete(`/llmprompttemplate/${templateId}`);
  },

  // Get public templates
  async getPublicTemplates(): Promise<LLMPromptTemplate[]> {
    const response = await api.get('/llmprompttemplate/public');
    return response.data;
  },

  // Clone template
  async cloneTemplate(templateId: number): Promise<LLMPromptTemplate> {
    const response = await api.post(`/llmprompttemplate/${templateId}/clone`);
    return response.data;
  },
};

export default llmService;
