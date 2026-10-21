import { createContext, useContext, useState, useEffect, useCallback, ReactNode } from 'react';
import { api } from '../lib/api';
import { authService } from '../services/authService';

export interface SubscriptionPlan {
  id: number;
  name: string;
  displayName: string;
  description: string;
  monthlyPrice: number;
  yearlyPrice: number;
  currency: string;
  maxProjects: number;
  maxPromptsPerProject: number;
  maxVisibilityChecksPerMonth: number;
  maxSiteAuditsPerMonth: number;
  maxContentArticles: number;
  maxLlmRequestsPerMonth: number;
  allowCompetitorTracking: boolean;
  allowAdvancedAnalytics: boolean;
  allowApiAccess: boolean;
  allowWhiteLabel: boolean;
  allowPrioritySupport: boolean;
  allowCustomIntegrations: boolean;
  isActive: boolean;
  sortOrder: number;
  stripePriceIdMonthly?: string;
  stripePriceIdYearly?: string;
}

export interface QuotaStatus {
  type: string;
  limit: number;
  used: number;
  remaining: number;
}

export interface SubscriptionStatus {
  hasActiveSubscription: boolean;
  requiresSubscription: boolean;
  planName?: string;
  planId?: number;
  status?: string;
  billingCycle?: string;
  quotas?: QuotaStatus[];
}

interface SubscriptionContextType {
  status: SubscriptionStatus | null;
  plans: SubscriptionPlan[];
  loading: boolean;
  currentPlan: SubscriptionPlan | null;
  // Limit helpers
  projectLimit: number;
  promptLimit: number;
  canCreateProject: (currentCount: number) => boolean;
  canCreatePrompt: (currentCount: number) => boolean;
  // Refresh
  refreshStatus: () => Promise<void>;
  // Upgrade modal control
  showUpgradeModal: boolean;
  upgradeReason: string;
  openUpgradeModal: (reason: string) => void;
  closeUpgradeModal: () => void;
}

const SubscriptionContext = createContext<SubscriptionContextType | undefined>(undefined);

export function SubscriptionProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<SubscriptionStatus | null>(null);
  const [plans, setPlans] = useState<SubscriptionPlan[]>([]);
  const [loading, setLoading] = useState(true);
  const [showUpgradeModal, setShowUpgradeModal] = useState(false);
  const [upgradeReason, setUpgradeReason] = useState('');

  const fetchData = useCallback(async () => {
    if (!authService.isAuthenticated()) {
      setLoading(false);
      return;
    }
    try {
      const [statusRes, plansRes] = await Promise.all([
        api.get('/subscription/status'),
        api.get('/subscription/plans'),
      ]);
      setStatus(statusRes.data);
      const rawPlans = plansRes.data;
      setPlans(Array.isArray(rawPlans) ? rawPlans : (Array.isArray(rawPlans?.data) ? rawPlans.data : []));
    } catch (err) {
      console.error('Failed to load subscription data:', err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const refreshStatus = useCallback(async () => {
    try {
      const [statusRes, plansRes] = await Promise.all([
        api.get('/subscription/status'),
        api.get('/subscription/plans'),
      ]);
      setStatus(statusRes.data);
      const rawPlans = plansRes.data;
      setPlans(Array.isArray(rawPlans) ? rawPlans : (Array.isArray(rawPlans?.data) ? rawPlans.data : []));
    } catch (err) {
      console.error('Failed to refresh subscription status:', err);
    }
  }, []);

  const currentPlan = plans.find(p => p.id === status?.planId) ?? null;

  // -1 means unlimited
  const projectLimit = currentPlan?.maxProjects ?? 3;
  const promptLimit = currentPlan?.maxPromptsPerProject ?? 10;

  const canCreateProject = useCallback((currentCount: number) => {
    if (projectLimit === -1) return true;
    return currentCount < projectLimit;
  }, [projectLimit]);

  const canCreatePrompt = useCallback((currentCount: number) => {
    if (promptLimit === -1) return true;
    return currentCount < promptLimit;
  }, [promptLimit]);

  const openUpgradeModal = useCallback((reason: string) => {
    setUpgradeReason(reason);
    setShowUpgradeModal(true);
  }, []);

  const closeUpgradeModal = useCallback(() => {
    setShowUpgradeModal(false);
    setUpgradeReason('');
  }, []);

  return (
    <SubscriptionContext.Provider value={{
      status,
      plans,
      loading,
      currentPlan,
      projectLimit,
      promptLimit,
      canCreateProject,
      canCreatePrompt,
      refreshStatus,
      showUpgradeModal,
      upgradeReason,
      openUpgradeModal,
      closeUpgradeModal,
    }}>
      {children}
    </SubscriptionContext.Provider>
  );
}

export function useSubscription() {
  const context = useContext(SubscriptionContext);
  if (context === undefined) {
    throw new Error('useSubscription must be used within a SubscriptionProvider');
  }
  return context;
}
