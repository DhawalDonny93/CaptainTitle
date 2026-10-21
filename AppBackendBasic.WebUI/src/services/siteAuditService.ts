import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL;

// Create axios instance with auth configuration
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


export interface SiteAudit {
  id: number;
  projectId: number;
  url: string;
  auditDate: Date;
  overallScore: number;
  performanceScore?: number;
  seoScore?: number;
  accessibilityScore?: number;
  bestPracticesScore?: number;
  issuesFound: number;
  createdAt: Date;
  updatedAt: Date;
}

export interface StartAuditRequest {
  projectId: number;
  url: string;
}

export interface AuditRun {
  id: number;
  status: string;
  message: string;
  auditId?: number;
}

export interface AuditIssue {
  id: number;
  siteAuditRunId: number;
  title: string;
  description: string;
  severity: 'Critical' | 'High' | 'Medium' | 'Low';
  category: 'Content' | 'AEO' | 'Technical';
  type: string;
  affectedPages: number;
  isCompleted: boolean;
  completedAt?: Date;
  completedBy?: number;
  recommendation?: string;
  autoFixAvailable: boolean;
  priority: number;
  estimatedImpactScore: number;
  createdAt: Date;
  updatedAt: Date;
}

const siteAuditService = {
  // Get audits for a project
  async getAuditsByProject(projectId: number): Promise<SiteAudit[]> {
    const response = await api.get(`/siteaudits/project/${projectId}`);
    return response.data;
  },

  // Start a new audit
  async startAudit(request: StartAuditRequest): Promise<AuditRun> {
    const response = await api.post('/siteaudits/run', request);
    return response.data;
  },

  // Get audit by ID
  async getAuditById(auditId: number): Promise<SiteAudit> {
    const response = await api.get(`/siteaudits/${auditId}`);
    return response.data;
  },

  // Get audit issues for a project (from latest audit)
  async getProjectAuditIssues(projectId: number): Promise<AuditIssue[]> {
    const response = await api.get(`/siteaudits/project/${projectId}/issues`);
    return response.data;
  },

  // Get audit issues for a specific audit run
  async getAuditIssues(runId: number, isCompleted?: boolean, severity?: string): Promise<AuditIssue[]> {
    const params = new URLSearchParams();
    if (isCompleted !== undefined) params.append('isCompleted', String(isCompleted));
    if (severity) params.append('severity', severity);
    
    const response = await api.get(`/siteaudits/runs/${runId}/issues?${params.toString()}`);
    return response.data;
  },

  // Mark an issue as completed
  async markIssueCompleted(issueId: number): Promise<void> {
    await api.post(`/siteaudits/issues/${issueId}/complete`);
  },

  // Mark an issue as incomplete
  async markIssueIncomplete(issueId: number): Promise<void> {
    await api.post(`/siteaudits/issues/${issueId}/incomplete`);
  },

  // Check if project has completed audits
  async hasCompletedAudits(projectId: number): Promise<boolean> {
    try {
      const audits = await this.getAuditsByProject(projectId);
      return audits.length > 0;
    } catch {
      return false;
    }
  },
};

export default siteAuditService;
