# 📋 URLs for Testing Unused/Disconnected Pages

## 🔗 **Currently Accessible Pages**

### ✅ ArticleGenerator (32KB - Just Added)
```
http://localhost:PORT/article-generator
http://localhost:PORT/projects/1/article-generator
```
*Just added temporary routes for testing*

---

## 🚫 **Pages Without Routes (Need Manual Addition)**

### 1. AITrackingDashboard (8.5KB)
**Add to App.tsx:**
```tsx
import AITrackingDashboard from './pages/AITrackingDashboard'
<Route path="/ai-tracking" element={<AITrackingDashboard />} />
```
**URLs after adding:**
```
http://localhost:PORT/ai-tracking
```

### 2. AnalyticsDashboard (9.6KB)
**Add to App.tsx:**
```tsx
import AnalyticsDashboard from './pages/AnalyticsDashboard'
<Route path="/analytics-dashboard" element={<AnalyticsDashboard />} />
```
**URLs after adding:**
```
http://localhost:PORT/analytics-dashboard
```

### 3. OnboardingFlow (8.7KB - Old Version)
**Add to App.tsx:**
```tsx
import OnboardingFlow from './pages/OnboardingFlow'
<Route path="/onboarding-old" element={<OnboardingFlow />} />
```
**URLs after adding:**
```
http://localhost:PORT/onboarding-old
```

### 4. ProjectDashboard (6.8KB - Multi-Tab)
**Add to App.tsx:**
```tsx
import ProjectDashboard from './pages/ProjectDashboard'
<Route path="/project-dashboard/:id" element={<ProjectDashboard />} />
```
**URLs after adding:**
```
http://localhost:PORT/project-dashboard/1
http://localhost:PORT/project-dashboard/2
```

### 5. ContentWriter (7.5KB)
**Add to App.tsx:**
```tsx
import ContentWriter from './pages/ContentWriter'
<Route path="/content-writer" element={<ContentWriter />} />
<Route path="/projects/:id/content-writer" element={<ContentWriter />} />
```
**URLs after adding:**
```
http://localhost:PORT/content-writer
http://localhost:PORT/projects/1/content-writer
```

### 6. AiVisibility (730 bytes)
**Add to App.tsx:**
```tsx
import AiVisibility from './pages/AiVisibility'
<Route path="/ai-visibility" element={<AiVisibility />} />
<Route path="/projects/:id/ai-visibility" element={<AiVisibility />} />
```
**URLs after adding:**
```
http://localhost:PORT/ai-visibility
http://localhost:PORT/projects/1/ai-visibility
```

### 7. ActionPlan (4.5KB - Old Version)
**Add to App.tsx:**
```tsx
import ActionPlan from './pages/ActionPlan'
<Route path="/action-plan-old" element={<ActionPlan />} />
<Route path="/projects/:id/action-plan-old" element={<ActionPlan />} />
```
**URLs after adding:**
```
http://localhost:PORT/action-plan-old
http://localhost:PORT/projects/1/action-plan-old
```

---

## 🟡 **Recently Removed Routes (Can Add Back for Testing)**

### Community Pages (You Recently Removed)
**Add back to App.tsx:**
```tsx
import ProjectCommunity from './pages/ProjectCommunity'
<Route path="/projects/:id/community" element={<ProjectCommunity />} />
```

**Add back to MainLayout.tsx navigation:**
```tsx
<button onClick={() => navigate(`/projects/${selectedProject.id}/community`)}>
  <MessageSquare className="w-4 h-4" />
  <span>Community</span>
</button>
```

**URLs after adding:**
```
http://localhost:PORT/projects/1/community
http://localhost:PORT/projects/2/community
```

### ActionPlans (You Recently Removed)
**Add back to App.tsx:**
```tsx
import ActionPlans from './pages/ActionPlans'
<Route path="/action-plans" element={<ActionPlans />} />
<Route path="/projects/:id/action-plan" element={<ActionPlans />} />
```

**URLs after adding:**
```
http://localhost:PORT/action-plans
http://localhost:PORT/projects/1/action-plan
```

---

## 🎯 **Quick Testing Template**

### To Test Any Page:

1. **Add the import** to `App.tsx`:
```tsx
import PageName from './pages/PageName'
```

2. **Add the route** in protected routes section:
```tsx
<Route path="/test-page" element={<PageName />} />
```

3. **Navigate to** `http://localhost:PORT/test-page`

### For Project-Specific Pages:
```tsx
<Route path="/projects/:id/test-page" element={<PageName />} />
```
Navigate to: `http://localhost:PORT/projects/1/test-page`

---

## 📊 **Testing Priority Order**

### 🔴 High Priority (Large Files - Check First)
1. **ArticleGenerator** - ✅ Already accessible at `/article-generator`
2. **AnalyticsDashboard** - 9.6KB
3. **AITrackingDashboard** - 8.5KB
4. **OnboardingFlow** - 8.7KB

### 🟡 Medium Priority
5. **ProjectDashboard** - 6.8KB (Multi-tab interface)
6. **ContentWriter** - 7.5KB
7. **ActionPlan** - 4.5KB (Old version)

### 🟢 Low Priority (Small Files)
8. **AiVisibility** - 730 bytes
9. **Community** - Recently removed
10. **ActionPlans** - Recently removed

---

## 📝 **Summary Table**

| Page | File Size | Status | URLs |
|------|-----------|---------|------|
| ArticleGenerator | 32KB | ✅ Added | `/article-generator`, `/projects/:id/article-generator` |
| AnalyticsDashboard | 9.6KB | 🚫 No Route | Need to add route |
| AITrackingDashboard | 8.5KB | 🚫 No Route | Need to add route |
| OnboardingFlow | 8.7KB | 🚫 No Route | Need to add route |
| ProjectDashboard | 6.8KB | 🚫 No Route | Need to add route |
| ContentWriter | 7.5KB | 🚫 No Route | Need to add route |
| AiVisibility | 730B | 🚫 No Route | Need to add route |
| ActionPlan | 4.5KB | 🚫 No Route | Need to add route |
| Community | - | 🟡 Removed | Can add back |
| ActionPlans | - | 🟡 Removed | Can add back |

---

## 💡 **Instructions**

1. **Start with ArticleGenerator** (already accessible)
2. **Add routes one by one** for other pages to test
3. **Check functionality** and decide what to keep/delete
4. **Remove unused code** after testing
5. **ProjectDashboard** is particularly interesting - multi-tab interface

**Note:** Replace `PORT` with your actual development server port (usually 3000, 5173, etc.)
