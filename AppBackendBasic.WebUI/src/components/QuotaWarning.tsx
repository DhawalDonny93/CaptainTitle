import React, { useEffect, useState } from 'react';
import { AlertTriangle, TrendingUp, X } from 'lucide-react';
import { api } from '../lib/api';

interface QuotaStatus {
  quotaType: string;
  limit: number;
  used: number;
  remaining: number;
  percentageUsed: number;
  isExceeded: boolean;
  isNearLimit: boolean;
  periodStart: string;
  periodEnd: string;
}

export const QuotaWarning: React.FC = () => {
  const [quotas, setQuotas] = useState<Record<string, QuotaStatus>>({});
  const [showWarning, setShowWarning] = useState(false);
  const [dismissed, setDismissed] = useState<string[]>([]);

  useEffect(() => {
    loadQuotas();
    // Refresh quotas every 5 minutes
    const interval = setInterval(loadQuotas, 5 * 60 * 1000);
    return () => clearInterval(interval);
  }, []);

  const loadQuotas = async () => {
    try {
      const response = await api.get('/subscription/quotas');
      setQuotas(response.data);
      
      // Check if any quota is near limit or exceeded
      const hasWarning = Object.values(response.data).some(
        (q: any) => (q as QuotaStatus).isNearLimit || (q as QuotaStatus).isExceeded
      );
      setShowWarning(hasWarning);
    } catch (error) {
      console.error('Error loading quotas:', error);
    }
  };

  const dismissQuota = (quotaType: string) => {
    setDismissed([...dismissed, quotaType]);
  };

  if (!showWarning) return null;

  const criticalQuotas = Object.values(quotas).filter(
    q => (q.isExceeded || q.isNearLimit) && !dismissed.includes(q.quotaType)
  );

  if (criticalQuotas.length === 0) return null;

  const getQuotaLabel = (quotaType: string): string => {
    const labels: Record<string, string> = {
      'VisibilityChecks': 'Visibility Checks',
      'SiteAudits': 'Site Audits',
      'LlmRequests': 'AI Requests',
      'LlmTokens': 'AI Tokens'
    };
    return labels[quotaType] || quotaType;
  };

  return (
    <div className="fixed bottom-4 right-4 max-w-md z-50 space-y-2">
      {criticalQuotas.map((quota) => (
        <div
          key={quota.quotaType}
          className={`p-4 rounded-lg shadow-lg animate-slide-in ${
            quota.isExceeded
              ? 'bg-red-50 border-2 border-red-500'
              : 'bg-yellow-50 border-2 border-yellow-500'
          }`}
        >
          <div className="flex items-start">
            <AlertTriangle
              className={`h-5 w-5 mt-0.5 mr-3 flex-shrink-0 ${
                quota.isExceeded ? 'text-red-600' : 'text-yellow-600'
              }`}
            />
            <div className="flex-1 min-w-0">
              <h4 className="font-semibold text-gray-900">
                {quota.isExceeded ? 'Quota Exceeded' : 'Quota Warning'}
              </h4>
              <p className="text-sm text-gray-700 mt-1">
                <strong>{getQuotaLabel(quota.quotaType)}:</strong>{' '}
                {quota.used.toLocaleString()} / {quota.limit.toLocaleString()} used
                <span className="ml-1 text-gray-600">
                  ({quota.percentageUsed.toFixed(0)}%)
                </span>
              </p>
              {quota.isExceeded ? (
                <p className="text-sm text-red-700 mt-2">
                  You've reached your limit. Upgrade to continue using this feature.
                </p>
              ) : (
                <p className="text-sm text-yellow-700 mt-2">
                  You're approaching your limit. Consider upgrading soon.
                </p>
              )}
              <button
                onClick={() => window.location.href = '/pricing'}
                className={`mt-3 inline-flex items-center px-3 py-1.5 text-white text-sm rounded font-medium transition ${
                  quota.isExceeded
                    ? 'bg-red-600 hover:bg-red-700'
                    : 'bg-blue-600 hover:bg-blue-700'
                }`}
              >
                <TrendingUp className="h-4 w-4 mr-1" />
                Upgrade Plan
              </button>
            </div>
            <button
              onClick={() => dismissQuota(quota.quotaType)}
              className="text-gray-400 hover:text-gray-600 ml-2 flex-shrink-0"
              aria-label="Dismiss"
            >
              <X className="h-5 w-5" />
            </button>
          </div>
        </div>
      ))}
    </div>
  );
};
