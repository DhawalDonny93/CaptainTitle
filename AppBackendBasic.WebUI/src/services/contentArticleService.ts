import { api } from '../lib/api';

export interface ContentArticle {
  id: number;
  projectId: number;
  title: string;
  slug?: string;
  contentHtml?: string;
  contentMarkdown?: string;
  metaTitle?: string;
  metaDescription?: string;
  featuredImageUrl?: string;
  status: 'Draft' | 'Scheduled' | 'Published' | 'Archived';
  scheduledPublishAt?: Date;
  publishedAt?: Date;
  wordCount: number;
  aiModelUsed?: string;
  generationPrompt?: string;
  seoScore: number;
  createdAt: Date;
  updatedAt?: Date;
}

export interface CreateArticleRequest {
  projectId: number;
  title: string;
  slug?: string;
  contentHtml?: string;
  contentMarkdown?: string;
  metaTitle?: string;
  metaDescription?: string;
  featuredImageUrl?: string;
  status?: 'Draft' | 'Scheduled' | 'Published' | 'Archived';
  scheduledPublishAt?: Date;
  wordCount?: number;
  aiModelUsed?: string;
  generationPrompt?: string;
  seoScore?: number;
}

export interface UpdateArticleRequest {
  title?: string;
  slug?: string;
  contentHtml?: string;
  contentMarkdown?: string;
  metaTitle?: string;
  metaDescription?: string;
  featuredImageUrl?: string;
  status?: 'Draft' | 'Scheduled' | 'Published' | 'Archived';
  scheduledPublishAt?: Date;
  wordCount?: number;
  aiModelUsed?: string;
  generationPrompt?: string;
  seoScore?: number;
}

export interface ArticleAnalysis {
  seoScore: number;
  readabilityScore: number;
  wordCount: number;
  suggestions: string[];
}

const contentArticleService = {
  // Get articles for a project
  async getArticlesByProject(projectId: number, status?: string): Promise<ContentArticle[]> {
    const params = new URLSearchParams();
    if (status) params.append('status', status);
    
    const response = await api.get(`/contentarticles/project/${projectId}?${params.toString()}`);
    return response.data;
  },

  // Get article by ID
  async getArticleById(articleId: number): Promise<ContentArticle> {
    const response = await api.get(`/contentarticles/${articleId}`);
    return response.data;
  },

  // Create a new article
  async createArticle(request: CreateArticleRequest): Promise<ContentArticle> {
    const response = await api.post('/contentarticles', request);
    return response.data;
  },

  // Update an article
  async updateArticle(articleId: number, request: UpdateArticleRequest): Promise<ContentArticle> {
    const response = await api.put(`/contentarticles/${articleId}`, request);
    return response.data;
  },

  // Delete an article
  async deleteArticle(articleId: number): Promise<void> {
    await api.delete(`/contentarticles/${articleId}`);
  },

  // Publish an article
  async publishArticle(articleId: number): Promise<ContentArticle> {
    const response = await api.put(`/contentarticles/${articleId}/publish`);
    return response.data;
  },

  // Analyze article content
  async analyzeArticle(articleId: number): Promise<ArticleAnalysis> {
    const response = await api.post(`/contentarticles/${articleId}/analyze`);
    return response.data;
  },

  // Alias for backward compatibility
  async getArticles(projectId: number, status?: string): Promise<ContentArticle[]> {
    return this.getArticlesByProject(projectId, status);
  },
};

export default contentArticleService;
