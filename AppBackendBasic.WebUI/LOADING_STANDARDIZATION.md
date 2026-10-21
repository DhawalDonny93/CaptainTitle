# Loading Animation Standardization

## Overview
All loading animations across the application now use consistent components from `@/components/ui/LoadingSpinner`.

## Components Available

### 1. LoadingPage
Full-page loading state with centered spinner.

```tsx
import { LoadingPage } from '../components/ui/LoadingSpinner';

if (loading) {
  return <LoadingPage />;
}

// With custom text
if (loading) {
  return <LoadingPage text="Loading data..." />;
}

// With custom height
if (loading) {
  return <LoadingPage minHeight="h-96" />;
}
```

### 2. LoadingSpinner
Inline loading spinner for smaller sections.

```tsx
import { LoadingSpinner } from '../components/ui/LoadingSpinner';

<LoadingSpinner size="sm" />  // Small (w-4 h-4)
<LoadingSpinner size="md" />  // Medium (w-8 h-8) - default
<LoadingSpinner size="lg" />  // Large (w-12 h-12)

// With text
<LoadingSpinner size="md" text="Processing..." />
```

### 3. LoadingButton
Loading state for button content.

```tsx
import { LoadingButton } from '../components/ui/LoadingSpinner';

<button disabled={loading}>
  {loading ? <LoadingButton>Saving...</LoadingButton> : 'Save'}
</button>
```

## Pages Updated

### ✅ Standardized
- `/pages/Dashboard.tsx` - Uses LoadingPage
- `/pages/VisibilityDashboards.tsx` - Uses LoadingPage
- `/pages/Mentions.tsx` - Uses LoadingPage with text
- `/pages/CommunityOpportunities.tsx` - Uses LoadingPage
- `/pages/SiteAudits.tsx` - Uses LoadingPage

### 🔄 Needs Update
The following pages still use custom loading patterns and should be updated:

#### Admin Pages
- `/pages/admin/AdminDashboard.tsx`
- `/pages/admin/AuditLogs.tsx`
- `/pages/admin/SystemSettings.tsx`
- `/pages/admin/FeatureFlags.tsx`
- `/pages/admin/UserManagement.tsx`
- `/pages/admin/LLMSettings.tsx`
- `/pages/admin/EmailTemplates.tsx`
- `/pages/admin/Monitoring.tsx`

#### Main Pages
- `/pages/AnalyticsDashboard.tsx`
- `/pages/AITrackingDashboard.tsx`
- `/pages/AiVisibility.tsx`
- `/pages/ArticleGenerator.tsx`
- `/pages/ContentEditor.tsx`
- `/pages/ContentListing.tsx`
- `/pages/ContentWriter.tsx`
- `/pages/ToDoTasks.tsx`
- `/pages/YourPrompts.tsx`
- `/pages/OpportunitiesFound.tsx`
- `/pages/ActionPlans.tsx`
- `/pages/NotificationsPage.tsx`
- `/pages/ProjectDashboard.tsx`
- `/pages/PromptResearch.tsx`

#### Auth Pages (Custom Spinners)
- `/pages/Login.tsx` - Uses custom SVG spinner
- `/pages/VerifyEmail.tsx` - Uses custom SVG spinner
- `/pages/ForgotPassword.tsx` - Uses custom SVG spinner
- `/pages/ResetPassword.tsx` - Uses custom SVG spinner
- `/pages/LoginHistory.tsx` - Uses custom SVG spinner

#### Other
- `/components/NotificationDropdown.tsx` - Uses custom spinner
- `/pages/OnboardingFlowNew.tsx`
- `/pages/Subscription.tsx`
- `/pages/Pricing.tsx`

## Migration Guide

### Before (Multiple Patterns)
```tsx
// Pattern 1: Custom div with Loader2
if (loading) {
  return (
    <div className="flex items-center justify-center h-64">
      <Loader2 className="w-8 h-8 animate-spin text-blue-600" />
    </div>
  );
}

// Pattern 2: Custom SVG spinner
{loading ? (
  <svg className="w-5 h-5 animate-spin" fill="none" viewBox="0 0 24 24">
    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
    <path className="opacity-75" fill="currentColor" d="..."></path>
  </svg>
) : 'Submit'}

// Pattern 3: Inline Loader2
{loading ? <Loader2 className="animate-spin" /> : <Content />}
```

### After (Standardized)
```tsx
// Pattern 1: Full page loading
import { LoadingPage } from '../components/ui/LoadingSpinner';

if (loading) {
  return <LoadingPage />;
}

// Pattern 2: Button loading
import { LoadingButton } from '../components/ui/LoadingSpinner';

{loading ? <LoadingButton>Submitting...</LoadingButton> : 'Submit'}

// Pattern 3: Inline loading
import { LoadingSpinner } from '../components/ui/LoadingSpinner';

{loading ? <LoadingSpinner size="sm" /> : <Content />}
```

## Benefits

1. **Consistency** - Same loading experience across all pages
2. **Maintainability** - Single source of truth for loading UI
3. **Accessibility** - Centralized place to add ARIA labels
4. **Performance** - Reusable components reduce bundle size
5. **Customization** - Easy to update globally by modifying one file

## Design Specifications

- **Color**: Gray-900 (`text-gray-900`)
- **Animation**: Lucide's Loader2 with `animate-spin`
- **Sizes**: 
  - Small: 16px (w-4 h-4)
  - Medium: 32px (w-8 h-8)
  - Large: 48px (w-12 h-12)
- **Spacing**: 12px gap between spinner and text
- **Text Color**: Gray-600 (`text-gray-600`)
- **Text Size**: Small (text-sm)

## Next Steps

1. Update remaining admin pages
2. Update main application pages
3. Update auth pages (replace custom SVG spinners)
4. Update components (NotificationDropdown, etc.)
5. Remove unused Loader2 imports from updated files
