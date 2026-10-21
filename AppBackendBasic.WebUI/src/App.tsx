import { BrowserRouter as Router, Routes, Route } from 'react-router-dom'
import LandingPage from './pages/LandingPage'
import OnboardingFlow from './pages/OnboardingFlowNew'
import Login from './pages/Login'
import Dashboard from './pages/Dashboard'
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
import Projects from './pages/Projects'
import ProjectAudits from './pages/ProjectAudits'
import NotificationsPage from './pages/NotificationsPage'
import VisibilityDashboards from './pages/VisibilityDashboards'
import ContentListing from './pages/ContentListing'
import ContentCreation from './pages/ContentCreation'
import ContentEditor from './pages/ContentEditor'
import ActionPlanWorkflow from './pages/ActionPlanWorkflow'
import ToDoTasks from './pages/ToDoTasks'
import OpportunitiesScanner from './pages/OpportunitiesScanner'
import OpportunitiesFound from './pages/OpportunitiesFound'
import PromptResearch from './pages/PromptResearch'
import YourPrompts from './pages/YourPrompts';
import Sources from './pages/Sources';
import Mentions from './pages/Mentions';
import BrandProfile from './pages/BrandProfile';
import Competitors from './pages/Competitors';
import LLMsTxtGenerator from './pages/LLMsTxtGenerator'
import AICrawlabilityChecker from './pages/AICrawlabilityChecker'
import PaymentSuccess from './pages/PaymentSuccess'
import PaymentCancelled from './pages/PaymentCancelled'
import OnboardingPlans from './pages/OnboardingPlans'
import SessionManager from './components/SessionManager'
import ProtectedRoute from './components/ProtectedRoute'
import AuthRedirect from './components/AuthRedirect'
import MainLayout from './components/layout/MainLayout'
import { AuthProvider } from './contexts/AuthContext'
import { ProjectProvider } from './contexts/ProjectContext'
import { SubscriptionProvider, useSubscription } from './contexts/SubscriptionContext'
import { UserProfileProvider } from './contexts/UserProfileContext'
import { WaitlistProvider } from './contexts/WaitlistContext'
import UpgradeModal from './components/UpgradeModal'
import ErrorBoundary from './components/ErrorBoundary'

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
              <ProjectProvider>
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
              <Route path="/tools/llms-txt-generator" element={<LLMsTxtGenerator />} />
              <Route path="/tools/ai-crawlability-checker" element={<AICrawlabilityChecker />} />
              <Route path="/onboarding/plans" element={<OnboardingPlans />} />
              <Route path="/payment-success" element={<PaymentSuccess />} />
              <Route path="/payment-cancelled" element={<PaymentCancelled />} />
              <Route path="/auth/google/callback" element={<GoogleCallback />} />
              <Route path="/auth/apple/callback" element={<AppleCallback />} />

              {/* Protected Routes with Main Layout */}
              <Route element={<ProtectedRoute><MainLayout /></ProtectedRoute>}>
                <Route path="/dashboard" element={<Dashboard />} />
                <Route path="/projects" element={<Projects />} />
                <Route path="/projects/:id/audits" element={<ProjectAudits />} />
                <Route path="/notifications" element={<NotificationsPage />} />
                <Route path="/login-history" element={<LoginHistory />} />
                <Route path="/settings" element={<Settings />} />
                <Route path="/todo-tasks" element={<ToDoTasks />} />
                
                {/* Unified pages - work both standalone and project-specific */}
                <Route path="/visibility-dashboards" element={<VisibilityDashboards />} />
                <Route path="/projects/:id/visibility" element={<VisibilityDashboards />} />
                <Route path="/content" element={<ContentListing />} />
                <Route path="/projects/:id/content" element={<ContentListing />} />
                <Route path="/projects/:id/content/create" element={<ContentCreation />} />
                <Route path="/projects/:id/content/edit/:articleId" element={<ContentEditor />} />
                <Route path="/action-plan-workflow" element={<ActionPlanWorkflow />} />
                <Route path="/projects/:id/action-plan-workflow" element={<ActionPlanWorkflow />} />
                <Route path="/opportunities" element={<OpportunitiesScanner />} />
                <Route path="/opportunities/results" element={<OpportunitiesFound />} />
                <Route path="/projects/:id/opportunities" element={<OpportunitiesScanner />} />
                <Route path="/projects/:id/opportunities/results" element={<OpportunitiesFound />} />
                <Route path="/prompt-research" element={<PromptResearch />} />
                <Route path="/projects/:id/prompt-research" element={<PromptResearch />} />
                <Route path="/your-prompts" element={<YourPrompts />} />
                <Route path="/projects/:id/your-prompts" element={<YourPrompts />} />
                <Route path="/sources" element={<Sources />} />
                <Route path="/projects/:id/sources" element={<Sources />} />
                <Route path="/mentions" element={<Mentions />} />
                <Route path="/projects/:id/mentions" element={<Mentions />} />
                <Route path="/brand-profile" element={<BrandProfile />} />
                <Route path="/projects/:id/brand-profile" element={<BrandProfile />} />
                <Route path="/competitors" element={<Competitors />} />
                <Route path="/projects/:id/competitors" element={<Competitors />} />
              </Route>

              {/* Onboarding Flow (No Sidebar) */}
              <Route path="/onboarding" element={<OnboardingFlow />} />
              </Routes>
            </SessionManager>
            <GlobalUpgradeModal />
              </ProjectProvider>
            </SubscriptionProvider>
          </UserProfileProvider>
        </AuthProvider>
        </WaitlistProvider>
      </Router>
    </ErrorBoundary>
  )
}

export default App
