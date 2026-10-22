import { lazy, Suspense } from 'react'
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom'
import LandingPage from './pages/LandingPage'
import OnboardingFlow from './pages/OnboardingFlowNew'
import Login from './pages/Login'
import Privacy from './pages/Privacy'
import Terms from './pages/Terms'
import Features from './pages/Features'
import Pricing from './pages/Pricing'
import About from './pages/About'
import GoogleCallback from './pages/GoogleCallback'
import AppleCallback from './pages/AppleCallback'
import VerifyEmail from './pages/VerifyEmail'
import ForgotPassword from './pages/ForgotPassword'
import ResetPassword from './pages/ResetPassword'
import LoginHistory from './pages/LoginHistory'
import Settings from './pages/Settings'
import NotificationsPage from './pages/NotificationsPage'
import PaymentSuccess from './pages/PaymentSuccess'
import PaymentCancelled from './pages/PaymentCancelled'
import OnboardingPlans from './pages/OnboardingPlans'
import SessionManager from './components/SessionManager'
import ProtectedRoute from './components/ProtectedRoute'
import AuthRedirect from './components/AuthRedirect'
import MainLayout from './components/layout/MainLayout'
import { AuthProvider } from './contexts/AuthContext'
import { SubscriptionProvider, useSubscription } from './contexts/SubscriptionContext'
import { UserProfileProvider } from './contexts/UserProfileContext'
import { WaitlistProvider } from './contexts/WaitlistContext'
import UpgradeModal from './components/UpgradeModal'
import ErrorBoundary from './components/ErrorBoundary'

// Captain Title caption studio - lazy so its (scoped) stylesheet only loads when the studio is opened
const StudioPage = lazy(() => import('./features/captions/pages/StudioPage'))
const CaptionPaymentSuccess = lazy(() => import('./features/captions/pages/CaptionPaymentResultPage').then((m) => ({ default: m.CaptionPaymentSuccess })))
const CaptionPaymentCancel = lazy(() => import('./features/captions/pages/CaptionPaymentResultPage').then((m) => ({ default: m.CaptionPaymentCancel })))

function GlobalUpgradeModal() {
  const { showUpgradeModal, upgradeReason, closeUpgradeModal } = useSubscription();
  return (
    <UpgradeModal
      open={showUpgradeModal}
      onClose={closeUpgradeModal}
      reason={upgradeReason}
    />
  );
}

function App() {
  return (
    <ErrorBoundary>
      <Router>
        <WaitlistProvider>
        <AuthProvider>
          <UserProfileProvider>
            <SubscriptionProvider>
              <AuthRedirect />
              <SessionManager idleTimeout={30}>
              <Routes>
              {/* Public Routes */}
              <Route path="/" element={<LandingPage />} />
              <Route path="/login" element={<Login />} />
              <Route path="/verify-email" element={<VerifyEmail />} />
              <Route path="/forgot-password" element={<ForgotPassword />} />
              <Route path="/reset-password" element={<ResetPassword />} />
              <Route path="/privacy" element={<Privacy />} />
              <Route path="/terms" element={<Terms />} />
              <Route path="/features" element={<Features />} />
              <Route path="/pricing" element={<Pricing />} />
              <Route path="/about" element={<About />} />
              <Route path="/onboarding/plans" element={<OnboardingPlans />} />
              <Route path="/payment-success" element={<PaymentSuccess />} />
              <Route path="/payment-cancelled" element={<PaymentCancelled />} />
              <Route path="/auth/google/callback" element={<GoogleCallback />} />
              <Route path="/auth/apple/callback" element={<AppleCallback />} />

              {/* Captain Title (caption generator) - full-screen, no sidebar */}
              <Route path="/studio" element={<ProtectedRoute><Suspense fallback={null}><StudioPage /></Suspense></ProtectedRoute>} />
              <Route path="/payment/success" element={<ProtectedRoute><Suspense fallback={null}><CaptionPaymentSuccess /></Suspense></ProtectedRoute>} />
              <Route path="/payment/cancel" element={<ProtectedRoute><Suspense fallback={null}><CaptionPaymentCancel /></Suspense></ProtectedRoute>} />

              {/* Account pages inside the sidebar layout */}
              <Route element={<ProtectedRoute><MainLayout /></ProtectedRoute>}>
                <Route path="/notifications" element={<NotificationsPage />} />
                <Route path="/login-history" element={<LoginHistory />} />
                <Route path="/settings" element={<Settings />} />
              </Route>

              {/* The old AI-SEO dashboard is gone: legacy links land in the studio */}
              <Route path="/dashboard" element={<Navigate to="/studio" replace />} />

              {/* Onboarding Flow (No Sidebar) */}
              <Route path="/onboarding" element={<OnboardingFlow />} />
              </Routes>
            </SessionManager>
            <GlobalUpgradeModal />
            </SubscriptionProvider>
          </UserProfileProvider>
        </AuthProvider>
        </WaitlistProvider>
      </Router>
    </ErrorBoundary>
  )
}

export default App
