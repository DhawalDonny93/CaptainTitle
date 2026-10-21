import { api } from '../lib/api';

export interface Policy {
  id: number;
  type: 'privacy' | 'terms' | 'cookies' | 'acceptable_use';
  title: string;
  content: string;
  version: string;
  effectiveDate: Date;
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
}

export interface PolicyAcceptance {
  id: number;
  userId: number;
  policyId: number;
  policyVersion: string;
  acceptedAt: Date;
  ipAddress: string;
}

const policyService = {
  // Get all policies
  async getPolicies(): Promise<Policy[]> {
    const response = await api.get('/policies');
    return response.data;
  },

  // Get policy by type
  async getPolicyByType(type: string): Promise<Policy> {
    const response = await api.get(`/policies/${type}`);
    return response.data;
  },

  // Get policy by ID
  async getPolicyById(policyId: number): Promise<Policy> {
    const response = await api.get(`/policies/id/${policyId}`);
    return response.data;
  },

  // Get policy version history
  async getPolicyHistory(type: string): Promise<Policy[]> {
    const response = await api.get(`/policies/${type}/history`);
    return response.data;
  },

  // Accept policy
  async acceptPolicy(policyId: number): Promise<PolicyAcceptance> {
    const response = await api.post('/policies/accept', { policyId });
    return response.data;
  },

  // Get user's policy acceptances
  async getUserAcceptances(): Promise<PolicyAcceptance[]> {
    const response = await api.get('/policies/acceptances');
    return response.data;
  },

  // Check if user has accepted latest policy
  async hasAcceptedLatest(type: string): Promise<boolean> {
    const response = await api.get(`/policies/${type}/accepted`);
    return response.data.accepted;
  },
};

export default policyService;
