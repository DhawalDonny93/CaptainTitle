import { useState, useEffect } from 'react';
import { X, Check, ChevronDown, ChevronUp, Loader2, Zap, TrendingUp, Building2 } from 'lucide-react';
import { useSubscription, SubscriptionPlan } from '../contexts/SubscriptionContext';
import { api } from '../lib/api';
import { toast } from '../utils/toast';

interface UpgradeModalProps {
  open: boolean;
  onClose: () => void;
  reason?: string;
}

const PLAN_ICONS: Record<string, React.ReactNode> = {
  Starter:      <Zap className="w-4 h-4 text-amber-500" />,
  Professional: <TrendingUp className="w-4 h-4 text-cherry" />,
  Scale:        <Building2 className="w-4 h-4 text-cherry" />,
};

const PLAN_ACCENT: Record<string, string> = {
  Starter:      'border-amber-400 bg-amber-50',
  Professional: 'border-ink bg-butter',
  Scale:        'border-ink bg-butter',
};

const PLAN_BTN: Record<string, string> = {
  Starter:      'bg-amber-500 hover:bg-amber-600 text-white',
  Professional: 'bg-cherry hover:bg-cherry text-white',
  Scale:        'bg-cherry hover:bg-cherry text-white',
};

function buildFeatures(plan: SubscriptionPlan): string[] {
  const features: string[] = [];

  if (plan.maxProjects === -1) features.push('Unlimited Projects');
  else features.push(`${plan.maxProjects} Project${plan.maxProjects !== 1 ? 's' : ''}`);

  if (plan.maxPromptsPerProject === -1) features.push('Unlimited Prompts per project');
  else features.push(`${plan.maxPromptsPerProject} Prompts per project`);

  if (plan.maxVisibilityChecksPerMonth === -1) features.push('Unlimited visibility checks/month');
  else features.push(`${plan.maxVisibilityChecksPerMonth.toLocaleString()} visibility checks/month`);

  if (plan.maxSiteAuditsPerMonth === -1) features.push('Unlimited site audits/month');
  else if (plan.maxSiteAuditsPerMonth > 0) features.push(`${plan.maxSiteAuditsPerMonth} site audits/month`);

  if (plan.maxContentArticles === -1) features.push('Unlimited content articles');
  else if (plan.maxContentArticles > 0) features.push(`${plan.maxContentArticles} content articles/month`);

  if (plan.allowCompetitorTracking) features.push('Competitor tracking');
  if (plan.allowAdvancedAnalytics) features.push('Advanced analytics');
  if (plan.allowApiAccess) features.push('API access');
  if (plan.allowWhiteLabel) features.push('White-label');
  if (plan.allowPrioritySupport) features.push('Priority support');
  if (plan.allowCustomIntegrations) features.push('Custom integrations');

  return features;
}

export default function UpgradeModal({ open, onClose, reason }: UpgradeModalProps) {
  const { plans, currentPlan, status } = useSubscription();
  const [billingCycle, setBillingCycle] = useState<'monthly' | 'yearly'>('monthly');
  const [expandedPlanId, setExpandedPlanId] = useState<number | null>(null);
  const [subscribing, setSubscribing] = useState<number | null>(null);

  // Auto-expand the first paid plan above current
  useEffect(() => {
    if (!open || plans.length === 0) return;
    const paidPlans = plans.filter(p => p.monthlyPrice > 0 && p.isActive);
    if (paidPlans.length > 0) {
      // Expand the "Popular" plan (Professional) or first paid
      const popular = paidPlans.find(p => p.name === 'Professional') ?? paidPlans[0];
      setExpandedPlanId(popular.id);
    }
  }, [open, plans]);

  if (!open) return null;

  const activePlans = plans.filter(p => p.isActive).sort((a, b) => a.sortOrder - b.sortOrder);
  const currentPlanId = currentPlan?.id;

  const handleSubscribe = async (plan: SubscriptionPlan) => {
    if (plan.monthlyPrice === 0) {
      try {
        setSubscribing(plan.id);
        await api.post('/subscription/assign-free');
        toast.success('Free plan activated!');
        onClose();
        window.location.reload();
      } catch (err: any) {
        toast.error(err.response?.data?.message || 'Failed to activate plan');
      } finally {
        setSubscribing(null);
      }
      return;
    }

    try {
      setSubscribing(plan.id);
      const response = await api.post('/subscription/create-checkout', {
        planId: plan.id,
        billingCycle: billingCycle === 'monthly' ? 'Monthly' : 'Yearly',
      });
      if (response.data.url) {
        window.location.href = response.data.url;
      }
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Failed to start checkout');
      setSubscribing(null);
    }
  };

  const annualSavingPct = 20;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-black/60 backdrop-blur-sm"
        onClick={onClose}
      />

      {/* Modal */}
      <div className="relative z-10 w-full max-w-3xl max-h-[92vh] overflow-hidden rounded-2xl shadow-2xl flex bg-white">

        {/* ── Left panel ── */}
        <div className="hidden md:flex flex-col justify-between w-72 flex-shrink-0 bg-cherry p-8 text-white">
          <div>
            <div className="w-10 h-10 rounded-xl bg-cherry border border-ink flex items-center justify-center mb-6">
              <Zap className="w-5 h-5 text-ink" />
            </div>
            <h2 className="text-2xl font-bold leading-tight mb-3">
              Upgrade to unlock more
            </h2>
            <p className="text-slate-400 text-sm leading-relaxed">
              {reason || 'You\'ve reached the limit of your current plan. Upgrade to continue growing your AI visibility.'}
            </p>

            <div className="mt-8 space-y-4">
              {[
                'Track more prompts & projects',
                'Unlimited AI visibility checks',
                'Advanced analytics & insights',
                'Priority support',
              ].map(f => (
                <div key={f} className="flex items-start gap-3">
                  <div className="w-5 h-5 rounded-full bg-green-500/20 border border-green-400/30 flex items-center justify-center flex-shrink-0 mt-0.5">
                    <Check className="w-3 h-3 text-green-400" />
                  </div>
                  <span className="text-sm text-slate-300">{f}</span>
                </div>
              ))}
            </div>
          </div>

          <div className="mt-8 p-4 rounded-xl bg-white/5 border border-white/10">
            <p className="text-xs text-slate-400 mb-1">Currently on</p>
            <p className="font-semibold text-white">{currentPlan?.displayName ?? 'Free'} Plan</p>
            {status?.billingCycle && (
              <p className="text-xs text-slate-400 mt-0.5">{status.billingCycle} billing</p>
            )}
          </div>
        </div>

        {/* ── Right panel ── */}
        <div className="flex-1 flex flex-col min-h-0">
          {/* Header */}
          <div className="flex items-center justify-between px-6 pt-6 pb-4 border-b border-gray-100 flex-shrink-0">
            <div>
              <h3 className="text-lg font-bold text-gray-900">Choose your plan</h3>
              <p className="text-xs text-gray-500 mt-0.5">Cancel anytime · No hidden fees</p>
            </div>
            <div className="flex items-center gap-3">
              {/* Billing toggle */}
              <div className="flex items-center bg-gray-100 rounded-lg p-0.5 text-sm">
                <button
                  onClick={() => setBillingCycle('monthly')}
                  className={`px-3 py-1.5 rounded-md transition-all font-medium ${
                    billingCycle === 'monthly'
                      ? 'bg-white shadow text-gray-900'
                      : 'text-gray-500 hover:text-gray-700'
                  }`}
                >
                  Monthly
                </button>
                <button
                  onClick={() => setBillingCycle('yearly')}
                  className={`px-3 py-1.5 rounded-md transition-all font-medium flex items-center gap-1.5 ${
                    billingCycle === 'yearly'
                      ? 'bg-white shadow text-gray-900'
                      : 'text-gray-500 hover:text-gray-700'
                  }`}
                >
                  Annual
                  <span className="text-xs font-semibold text-green-600 bg-green-100 px-1.5 py-0.5 rounded-full">
                    -{annualSavingPct}%
                  </span>
                </button>
              </div>
              <button
                onClick={onClose}
                className="w-8 h-8 flex items-center justify-center rounded-full hover:bg-gray-100 text-gray-400 hover:text-gray-600 transition-colors"
              >
                <X className="w-4 h-4" />
              </button>
            </div>
          </div>

          {/* Plan list — scrollable */}
          <div className="flex-1 overflow-y-auto px-6 py-4 space-y-3">
            {activePlans.map((plan) => {
              const price = billingCycle === 'monthly'
                ? plan.monthlyPrice
                : Math.round((plan.yearlyPrice / 12) * 100) / 100;
              const isExpanded = expandedPlanId === plan.id;
              const isCurrent = plan.id === currentPlanId;
              const isPopular = plan.name === 'Professional';
              const features = buildFeatures(plan);
              const accentClass = PLAN_ACCENT[plan.name] ?? 'border-gray-200 bg-gray-50';
              const btnClass = PLAN_BTN[plan.name] ?? 'bg-gray-800 hover:bg-gray-900 text-white';
              const icon = PLAN_ICONS[plan.name] ?? <Zap className="w-4 h-4 text-gray-500" />;

              return (
                <div
                  key={plan.id}
                  className={`rounded-xl border-2 transition-all overflow-hidden ${
                    isExpanded ? accentClass : 'border-gray-200 bg-white hover:border-gray-300'
                  }`}
                >
                  {/* Plan header row */}
                  <button
                    className="w-full flex items-center justify-between px-4 py-3.5 text-left"
                    onClick={() => setExpandedPlanId(isExpanded ? null : plan.id)}
                  >
                    <div className="flex items-center gap-3">
                      <div className={`w-7 h-7 rounded-full flex items-center justify-center ${
                        isExpanded ? 'bg-white shadow-sm' : 'bg-gray-100'
                      }`}>
                        {icon}
                      </div>
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="font-semibold text-gray-900 text-sm">{plan.displayName}</span>
                          {isPopular && (
                            <span className="text-xs font-bold px-2 py-0.5 rounded-full bg-cherry text-white">
                              Popular
                            </span>
                          )}
                          {isCurrent && (
                            <span className="text-xs font-medium px-2 py-0.5 rounded-full bg-gray-200 text-gray-600">
                              Current
                            </span>
                          )}
                        </div>
                        <p className="text-xs text-gray-500 mt-0.5">{plan.description}</p>
                      </div>
                    </div>
                    <div className="flex items-center gap-3 flex-shrink-0">
                      <div className="text-right">
                        <span className="text-xl font-bold text-gray-900">
                          {price === 0 ? 'Free' : `$${price}`}
                        </span>
                        {price > 0 && (
                          <span className="text-xs text-gray-500 ml-1">/mo</span>
                        )}
                      </div>
                      {isExpanded
                        ? <ChevronUp className="w-4 h-4 text-gray-400" />
                        : <ChevronDown className="w-4 h-4 text-gray-400" />
                      }
                    </div>
                  </button>

                  {/* Expanded details */}
                  {isExpanded && (
                    <div className="px-4 pb-4">
                      <div className="grid grid-cols-2 gap-1.5 mb-4">
                        {features.map((f, i) => (
                          <div key={i} className="flex items-start gap-2">
                            <Check className="w-3.5 h-3.5 text-green-500 mt-0.5 flex-shrink-0" />
                            <span className="text-xs text-gray-700">{f}</span>
                          </div>
                        ))}
                      </div>

                      {billingCycle === 'yearly' && plan.monthlyPrice > 0 && (
                        <p className="text-xs text-green-600 font-medium mb-3">
                          Save ${Math.round(plan.monthlyPrice * 12 * (annualSavingPct / 100))} per year vs monthly
                        </p>
                      )}

                      <button
                        onClick={() => handleSubscribe(plan)}
                        disabled={subscribing !== null || isCurrent}
                        className={`w-full py-2.5 rounded-lg font-semibold text-sm transition-all disabled:opacity-60 disabled:cursor-not-allowed flex items-center justify-center gap-2 ${
                          isCurrent
                            ? 'bg-gray-100 text-gray-500 cursor-default'
                            : btnClass
                        }`}
                      >
                        {subscribing === plan.id ? (
                          <><Loader2 className="w-4 h-4 animate-spin" /> Processing…</>
                        ) : isCurrent ? (
                          'Current Plan'
                        ) : price === 0 ? (
                          'Get Started Free'
                        ) : (
                          `Upgrade to ${plan.displayName}`
                        )}
                      </button>
                    </div>
                  )}
                </div>
              );
            })}
          </div>

          {/* Footer */}
          <div className="px-6 py-3 border-t border-gray-100 flex-shrink-0">
            <p className="text-xs text-center text-gray-400">
              Secure payment · Cancel anytime · Questions?{' '}
              <a href="mailto:support@aiseo.com" className="text-cherry hover:underline">Contact us</a>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
