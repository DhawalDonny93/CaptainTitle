# Base Framework Template Repository Strategy

## Overview

This document outlines the strategy for maintaining **BasicFrameworkBackend** as a base template repository that can be used as foundation code for multiple derived projects, with independent development and selective code synchronization.

---

## Repository Architecture

### Base Repository (Template)
**Repository:** `BasicFrameworkBackend` (this repo)
- **Purpose:** Maintain core framework features, common functionality, and best practices
- **Scope:** Generic, reusable components applicable to multiple projects
- **Development:** Framework improvements, bug fixes, security updates

### Derived Repositories (Projects)
**Examples:** `ProjectA-Backend`, `ProjectB-Backend`, `ClientX-App-Backend`
- **Purpose:** Specific business applications built on the base framework
- **Scope:** Project-specific features, business logic, customizations
- **Development:** Independent feature development, client requirements

---

## Strategy Options

### **Option 1: Git Template + Remote Upstream (Recommended)**

This is the most flexible approach for your use case.

#### Initial Setup

**1. Mark Base Repository as Template**
```bash
# On GitHub, go to Settings → Check "Template repository"
# Or keep it as regular repo and use git remote approach
```

**2. Create New Derived Project**
```bash
# Option A: Use GitHub template (if marked as template)
# Click "Use this template" button on GitHub

# Option B: Clone and create new repo
git clone https://github.com/YourUsername/BasicFrameworkBackend.git ProjectA-Backend
cd ProjectA-Backend
git remote rename origin base-framework
git remote add origin https://github.com/YourUsername/ProjectA-Backend.git
git push -u origin main
```

**3. Configure Upstream in Derived Project**
```bash
cd ProjectA-Backend
git remote add base-framework https://github.com/YourUsername/BasicFrameworkBackend.git
git fetch base-framework
```

#### Development Workflow

**A. Independent Development in Derived Project**
```bash
cd ProjectA-Backend

# Create feature branch
git checkout -b feature/project-specific-feature
# ... make changes ...
git commit -m "Add project-specific feature"
git push origin feature/project-specific-feature
```

**B. Pull Base Framework Updates into Derived Project**
```bash
cd ProjectA-Backend

# Fetch latest base framework changes
git fetch base-framework

# Option 1: Merge base changes (preserves history)
git checkout main
git merge base-framework/main --allow-unrelated-histories
# Resolve conflicts if any
git push origin main

# Option 2: Cherry-pick specific commits
git cherry-pick <commit-hash-from-base>
git push origin main

# Option 3: Rebase (cleaner history, more complex)
git rebase base-framework/main
git push origin main --force-with-lease
```

**C. Push Derived Project Changes Back to Base (Selective)**
```bash
cd ProjectA-Backend

# Identify commits to push back to base
git log --oneline

# Create patch files for specific commits
git format-patch <commit-hash>^..<commit-hash>

# Switch to base framework repo
cd ../BasicFrameworkBackend
git am < /path/to/patch/file.patch
git push origin main
```

---

### **Option 2: Git Subtree**

Use if you want to embed base framework as a subdirectory.

#### Setup
```bash
cd ProjectA-Backend

# Add base framework as subtree
git subtree add --prefix=framework \
  https://github.com/YourUsername/BasicFrameworkBackend.git main --squash
```

#### Pull Updates
```bash
# Pull latest base framework changes
git subtree pull --prefix=framework \
  https://github.com/YourUsername/BasicFrameworkBackend.git main --squash
```

#### Push Changes Back
```bash
# Push changes from framework/ directory back to base
git subtree push --prefix=framework \
  https://github.com/YourUsername/BasicFrameworkBackend.git feature/new-improvement
```

---

### **Option 3: Git Submodule**

Use if base framework should remain completely separate.

#### Setup
```bash
cd ProjectA-Backend

# Add base framework as submodule
git submodule add https://github.com/YourUsername/BasicFrameworkBackend.git framework
git commit -m "Add base framework as submodule"
```

#### Update Submodule
```bash
# Update to latest base framework
cd framework
git pull origin main
cd ..
git add framework
git commit -m "Update base framework to latest"
```

---

## Recommended Workflow (Option 1 Detailed)

### Phase 1: Initial Project Setup

```bash
# 1. Create new derived project from base
git clone https://github.com/YourUsername/BasicFrameworkBackend.git ProjectA-Backend
cd ProjectA-Backend

# 2. Rename remote and add new origin
git remote rename origin base-framework
git remote add origin https://github.com/YourUsername/ProjectA-Backend.git

# 3. Create initial commit for derived project
echo "# ProjectA Backend" > README.md
git add README.md
git commit -m "Initialize ProjectA from base framework"
git push -u origin main

# 4. Verify remotes
git remote -v
# base-framework  https://github.com/YourUsername/BasicFrameworkBackend.git (fetch)
# base-framework  https://github.com/YourUsername/BasicFrameworkBackend.git (push)
# origin          https://github.com/YourUsername/ProjectA-Backend.git (fetch)
# origin          https://github.com/YourUsername/ProjectA-Backend.git (push)
```

### Phase 2: Daily Development

**In Derived Project (ProjectA-Backend):**
```bash
# Regular development
git checkout -b feature/user-dashboard
# ... develop project-specific features ...
git commit -m "Add user dashboard"
git push origin feature/user-dashboard
```

**In Base Framework:**
```bash
cd BasicFrameworkBackend

# Improve base framework
git checkout -b feature/enhanced-auth
# ... improve authentication system ...
git commit -m "Enhance authentication with 2FA support"
git push origin feature/enhanced-auth
# Merge to main after review
```

### Phase 3: Sync Base Changes to Derived Project (Using Base-Sync Branches)

This approach uses dedicated `base-sync/*` branches to review, test, and resolve conflicts before merging to main.

```bash
cd ProjectA-Backend

# Step 1: Fetch latest base framework updates
git fetch base-framework

# Step 2: Review what changed in base framework
git log base-framework/main --oneline --graph --decorate
git log HEAD..base-framework/main --oneline  # See commits you don't have

# Step 3: View detailed changes
git diff HEAD..base-framework/main --stat  # Summary of changes
git diff HEAD..base-framework/main         # Full diff

# Step 4: Create dated base-sync branch for tracking
SYNC_DATE=$(date +%Y%m%d)
git checkout -b base-sync/$SYNC_DATE main

# Step 5: Merge base framework changes
git merge base-framework/main --no-ff -m "Sync: Merge base framework updates $SYNC_DATE"

# Step 6: If conflicts occur, review them carefully
git status  # See conflicted files
git diff    # Review conflicts

# Step 7: Resolve conflicts file by file
# Option A: Accept base framework version
git checkout --theirs <conflicted-file>

# Option B: Accept your project version
git checkout --ours <conflicted-file>

# Option C: Manual merge (recommended)
# Edit files manually, then:
git add <resolved-file>

# Step 8: Complete the merge
git commit -m "Resolve merge conflicts from base framework sync"

# Step 9: Test thoroughly in base-sync branch
# Run tests, build, manual testing
dotnet test
dotnet build
dotnet run

# Step 10: Push base-sync branch for review
git push origin base-sync/$SYNC_DATE

# Step 11: Create Pull Request for team review
# Review changes in GitHub/GitLab PR interface
# Team can comment on specific changes

# Step 12: After approval, merge to main
git checkout main
git merge base-sync/$SYNC_DATE --no-ff -m "Apply base framework updates from $SYNC_DATE"
git push origin main

# Step 13: Tag the sync for reference
git tag -a base-sync-$SYNC_DATE -m "Base framework sync completed on $SYNC_DATE"
git push origin base-sync-$SYNC_DATE

# Step 14: Keep base-sync branch for history (optional)
# Or delete after successful merge
git branch -d base-sync/$SYNC_DATE  # Delete local
git push origin --delete base-sync/$SYNC_DATE  # Delete remote
```

#### Alternative: Incremental Base-Sync Strategy

For large updates, sync incrementally using multiple base-sync branches:

```bash
# Sync specific feature/commit ranges
git checkout -b base-sync/auth-improvements
git cherry-pick <commit-1>
git cherry-pick <commit-2>
# Test and merge

git checkout main
git merge base-sync/auth-improvements

# Then sync next feature
git checkout -b base-sync/notification-system
git cherry-pick <commit-3>
git cherry-pick <commit-4>
# Test and merge
```

### Phase 4: Contribute Back to Base (Optional)

```bash
cd ProjectA-Backend

# Identify generic improvement to share
git log --oneline

# Create patch for specific commit
git format-patch -1 <commit-hash> -o ~/patches/

# Switch to base framework
cd ../BasicFrameworkBackend
git checkout -b feature/improvement-from-projecta

# Apply patch
git am ~/patches/0001-*.patch

# Review, test, and push
git push origin feature/improvement-from-projecta
# Create PR to base framework main
```

---

## Branch Strategy

### Base Framework Repository

```
main (protected)
├── develop (integration branch)
├── feature/new-feature-1
├── feature/new-feature-2
├── bugfix/security-patch
└── release/v1.0.0
```

**Branches:**
- `main` - Stable, production-ready base framework
- `develop` - Integration branch for new features
- `feature/*` - New framework features
- `bugfix/*` - Bug fixes
- `release/*` - Release preparation

### Derived Project Repository

```
main (protected)
├── develop
├── feature/project-feature-1
├── feature/project-feature-2
├── base-sync/20260110 (base framework sync branches)
├── base-sync/20260115
├── base-sync/auth-improvements (incremental sync)
└── release/v1.0.0
```

**Branches:**
- `main` - Production code for specific project
- `develop` - Project development branch
- `feature/*` - Project-specific features
- `base-sync/*` - Dedicated branches for reviewing and merging base framework updates
  - `base-sync/YYYYMMDD` - Dated sync branches for full merges
  - `base-sync/feature-name` - Feature-specific incremental syncs
- `release/*` - Release preparation

**Base-Sync Branch Workflow:**
1. Create `base-sync/YYYYMMDD` branch from main
2. Merge base framework changes into base-sync branch
3. Resolve conflicts in isolated branch
4. Test thoroughly without affecting main
5. Create PR for team review
6. Merge to main after approval
7. Tag sync completion for tracking
8. Keep or delete base-sync branch based on preference

---

## File Organization Strategy

### Base Framework Structure
```
BasicFrameworkBackend/
├── AppBackendBasic.API/          # Core API framework
├── AppBackendBasic.Core/         # Domain models, interfaces
├── AppBackendBasic.Infrastructure/ # Data access, external services
├── AppBackendBasic.Services/     # Business logic services
├── AppBackendBasic.AdminUI/      # Admin interface
├── docs/                         # Framework documentation
├── BASE_FRAMEWORK_STRATEGY.md    # This file
└── README.md                     # Framework overview
```

### Derived Project Structure
```
ProjectA-Backend/
├── AppBackendBasic.API/          # From base + project customizations
├── AppBackendBasic.Core/         # From base + project entities
├── AppBackendBasic.Infrastructure/ # From base + project repos
├── AppBackendBasic.Services/     # From base + project services
├── AppBackendBasic.AdminUI/      # From base + project admin
├── ProjectA.CustomModule/        # Project-specific module
├── docs/                         # Project documentation
├── PROJECT_SPECIFIC_README.md    # Project overview
└── .base-framework-version       # Track base version
```

**Track Base Version:**
```bash
# In derived project, create version file
echo "base-framework-version: v1.2.0" > .base-framework-version
echo "last-sync-commit: abc123def456" >> .base-framework-version
git add .base-framework-version
git commit -m "Track base framework version"
```

---

## Conflict Resolution Strategy

### Common Conflicts

**1. Configuration Files (appsettings.json)**
```bash
# Strategy: Keep project-specific, merge base improvements manually
# In .gitattributes:
appsettings.json merge=ours
appsettings.Development.json merge=ours
```

**2. Database Migrations**
```bash
# Strategy: Keep separate migration histories
# Base: Generic migrations
# Derived: Project-specific migrations
```

**3. Shared Code Files**
```bash
# Strategy: Merge carefully, prefer base for framework code
# Use merge tool to review changes
git mergetool
```

### Conflict Resolution Commands
```bash
# Accept base framework version
git checkout --theirs <file>

# Accept derived project version
git checkout --ours <file>

# Manual merge
git merge --no-commit base-framework/main
# Edit files manually
git add .
git commit -m "Merge base framework updates with conflict resolution"
```

---

## Selective Sync Strategy

### Sync Specific Features Only (Using Base-Sync Branches)

```bash
cd ProjectA-Backend

# Fetch base framework
git fetch base-framework

# Create base-sync branch for specific feature
git checkout -b base-sync/auth-improvements main

# Cherry-pick specific commits
git cherry-pick <commit-hash-1>
git cherry-pick <commit-hash-2>

# Or cherry-pick a range
git cherry-pick <start-commit>..<end-commit>

# Test the changes
dotnet test
dotnet build

# Push for review
git push origin base-sync/auth-improvements

# After approval, merge to main
git checkout main
git merge base-sync/auth-improvements --no-ff
git push origin main

# Tag the selective sync
git tag -a base-sync-auth-improvements -m "Synced auth improvements from base"
git push origin base-sync-auth-improvements
```

### Sync Specific Files/Directories

```bash
# Checkout specific files from base framework
git checkout base-framework/main -- AppBackendBasic.Services/Authentication/
git commit -m "Update authentication service from base framework"
```

### Exclude Certain Files from Sync

Create `.gitattributes` in derived project:
```
# Never merge these files from base
appsettings.json merge=ours
appsettings.*.json merge=ours
PROJECT_SPECIFIC_README.md merge=ours
```

---

## Automation Scripts

### Script 1: Check for Base Framework Updates

**File:** `scripts/check-base-updates.sh`
```bash
#!/bin/bash
# Check for updates in base framework

cd "$(dirname "$0")/.."
git fetch base-framework

BEHIND=$(git rev-list --count HEAD..base-framework/main)

if [ "$BEHIND" -gt 0 ]; then
    echo "⚠️  Base framework has $BEHIND new commits"
    echo "Recent changes:"
    git log --oneline HEAD..base-framework/main | head -10
    echo ""
    echo "To sync: git merge base-framework/main"
else
    echo "✅ Up to date with base framework"
fi
```

### Script 2: Sync Base Framework (Using Base-Sync Branches)

**File:** `scripts/sync-base-framework.sh`
```bash
#!/bin/bash
# Sync base framework updates using base-sync branches

cd "$(dirname "$0")/.."

echo "🔄 Base Framework Sync Tool"
echo "============================"

# Fetch latest updates
echo "📥 Fetching base framework updates..."
git fetch base-framework

# Check if there are updates
BEHIND=$(git rev-list --count HEAD..base-framework/main)
if [ "$BEHIND" -eq 0 ]; then
    echo "✅ Already up to date with base framework"
    exit 0
fi

echo "📊 Found $BEHIND new commits in base framework"
echo ""
echo "Recent changes:"
git log --oneline --graph --decorate HEAD..base-framework/main | head -20
echo ""

# Show detailed stats
echo "📈 Change summary:"
git diff --stat HEAD..base-framework/main
echo ""

# Confirm sync
read -p "Do you want to create a base-sync branch? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "❌ Sync cancelled"
    exit 1
fi

# Create dated base-sync branch
SYNC_DATE=$(date +%Y%m%d)
BRANCH_NAME="base-sync/$SYNC_DATE"

echo "🌿 Creating branch: $BRANCH_NAME"
git checkout -b "$BRANCH_NAME" main

# Merge base framework
echo "🔀 Merging base framework updates..."
git merge base-framework/main --no-ff -m "Sync: Merge base framework updates $SYNC_DATE"

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Merge successful!"
    echo ""
    echo "📋 Next steps:"
    echo "1. Test the application:"
    echo "   dotnet test"
    echo "   dotnet build"
    echo "   dotnet run"
    echo ""
    echo "2. Push base-sync branch:"
    echo "   git push origin $BRANCH_NAME"
    echo ""
    echo "3. Create Pull Request for review"
    echo ""
    echo "4. After approval, merge to main:"
    echo "   git checkout main"
    echo "   git merge $BRANCH_NAME --no-ff"
    echo "   git push origin main"
    echo ""
    echo "5. Tag the sync:"
    echo "   git tag -a base-sync-$SYNC_DATE -m 'Base sync completed'"
    echo "   git push origin base-sync-$SYNC_DATE"
else
    echo ""
    echo "⚠️  Merge conflicts detected!"
    echo ""
    echo "📝 Conflicted files:"
    git status --short | grep "^UU"
    echo ""
    echo "🔧 Resolve conflicts using:"
    echo "   git status              # See all conflicts"
    echo "   git diff                # Review conflicts"
    echo "   git checkout --theirs <file>  # Accept base version"
    echo "   git checkout --ours <file>    # Accept project version"
    echo "   # Or edit manually"
    echo ""
    echo "After resolving:"
    echo "   git add <resolved-files>"
    echo "   git commit -m 'Resolve merge conflicts from base sync'"
    echo ""
    echo "To abort merge:"
    echo "   git merge --abort"
fi
```

### Script 3: Extract Patch for Base Framework

**File:** `scripts/create-base-patch.sh`
```bash
#!/bin/bash
# Create patch from derived project to contribute back to base

if [ -z "$1" ]; then
    echo "Usage: ./create-base-patch.sh <commit-hash>"
    exit 1
fi

COMMIT_HASH=$1
PATCH_DIR="patches"
mkdir -p "$PATCH_DIR"

git format-patch -1 "$COMMIT_HASH" -o "$PATCH_DIR/"

echo "✅ Patch created in $PATCH_DIR/"
echo "To apply in base framework:"
echo "cd ../BasicFrameworkBackend"
echo "git am $PATCH_DIR/*.patch"
```

---

## Best Practices

### 1. **Keep Base Framework Generic**
- ❌ Don't add project-specific business logic
- ✅ Add reusable components, utilities, patterns
- ✅ Focus on infrastructure, authentication, common services

### 2. **Document Base Changes**
```bash
# In base framework commits, use clear prefixes
git commit -m "[FRAMEWORK] Add email service abstraction"
git commit -m "[SECURITY] Update JWT token validation"
git commit -m "[BREAKING] Change user ID from Guid to int"
```

### 3. **Version Base Framework**
```bash
# Tag major releases in base framework
git tag -a v1.0.0 -m "Base framework v1.0.0 - Initial stable release"
git tag -a v1.1.0 -m "Base framework v1.1.0 - Add notification system"
git push origin --tags
```

### 4. **Test Before Syncing**
- Always test base framework updates in a separate branch
- Run full test suite after merging
- Deploy to staging environment before production

### 5. **Maintain Changelog**
**File:** `CHANGELOG.md` in base framework
```markdown
# Changelog

## [1.2.0] - 2026-01-10
### Added
- Health check endpoints for Kubernetes
- Redis caching support
- Advanced rate limiting middleware

### Changed
- User ID type from Guid to int
- JWT token validation logic

### Fixed
- Exception handling middleware type mismatch
```

---

## Migration Checklist for New Projects

### Starting a New Derived Project

- [ ] Clone base framework repository
- [ ] Rename `origin` remote to `base-framework`
- [ ] Create new repository on GitHub/GitLab
- [ ] Add new `origin` remote pointing to new repo
- [ ] Create `.base-framework-version` file
- [ ] Update `README.md` with project-specific information
- [ ] Update `appsettings.json` with project configuration
- [ ] Create project-specific branches
- [ ] Set up CI/CD for new project
- [ ] Document project-specific customizations
- [ ] Add project team members
- [ ] Configure merge strategy in `.gitattributes`

---

## Troubleshooting

### Issue: Merge Conflicts on Every Sync

**Solution:**
```bash
# Use merge strategy for specific files
echo "appsettings.json merge=ours" >> .gitattributes
git add .gitattributes
git commit -m "Configure merge strategy for config files"
```

### Issue: Lost Project-Specific Changes After Merge

**Solution:**
```bash
# Recover from reflog
git reflog
git checkout <commit-before-merge>
git checkout -b recovery-branch

# Cherry-pick lost commits
git cherry-pick <lost-commit-hash>
```

### Issue: Base Framework Too Coupled to Derived Project

**Solution:**
- Refactor base framework to be more generic
- Move project-specific code to derived project
- Use dependency injection for customization points

---

## Summary

### Quick Reference Commands

```bash
# Setup derived project
git clone <base-repo> <new-project>
cd <new-project>
git remote rename origin base-framework
git remote add origin <new-repo-url>
git push -u origin main

# Sync base updates
git fetch base-framework
git merge base-framework/main

# Cherry-pick specific feature
git cherry-pick <commit-hash>

# Create patch for base
git format-patch -1 <commit-hash> -o patches/

# Check for updates
git fetch base-framework
git log HEAD..base-framework/main --oneline
```

### Recommended Approach

**Use Option 1 (Git Remote Upstream)** because it provides:
- ✅ Full independence for derived projects
- ✅ Selective synchronization
- ✅ Bidirectional code sharing
- ✅ Clear separation of concerns
- ✅ Standard Git workflows
- ✅ Easy conflict resolution

---

## Additional Resources

- [Git Remote Documentation](https://git-scm.com/docs/git-remote)
- [Git Cherry-Pick Guide](https://git-scm.com/docs/git-cherry-pick)
- [Git Merge Strategies](https://git-scm.com/docs/merge-strategies)
- [Monorepo vs Multi-repo](https://www.atlassian.com/git/tutorials/monorepos)

---

**Last Updated:** January 10, 2026  
**Base Framework Version:** v1.0.0  
**Strategy Version:** 1.0
