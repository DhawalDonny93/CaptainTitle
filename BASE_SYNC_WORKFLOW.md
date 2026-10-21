# Base-Sync Branch Workflow Guide

## Quick Reference: Base-Sync Branch Strategy

This guide focuses specifically on using **base-sync branches** to safely merge base framework updates into derived projects with full review and conflict resolution capabilities.

---

## Why Use Base-Sync Branches?

### Benefits
✅ **Isolated Testing** - Test base updates without affecting main branch  
✅ **Conflict Resolution** - Resolve merge conflicts in dedicated branch  
✅ **Team Review** - Create PRs for team to review changes before merge  
✅ **Rollback Safety** - Easy to abandon if issues found  
✅ **Change Tracking** - Clear history of what was synced and when  
✅ **Incremental Sync** - Sync large updates in smaller chunks  

---

## Complete Base-Sync Workflow

### Step 1: Check for Base Framework Updates

```bash
cd ProjectA-Backend

# Fetch latest from base framework
git fetch base-framework

# See how many commits behind you are
git rev-list --count HEAD..base-framework/main

# View commits you don't have
git log HEAD..base-framework/main --oneline --graph

# See summary of changes
git diff --stat HEAD..base-framework/main

# See detailed changes
git diff HEAD..base-framework/main
```

### Step 2: Create Base-Sync Branch

```bash
# Create dated base-sync branch from main
SYNC_DATE=$(date +%Y%m%d)
git checkout -b base-sync/$SYNC_DATE main

# Verify you're on the new branch
git branch --show-current
# Output: base-sync/20260110
```

### Step 3: Merge Base Framework Changes

```bash
# Merge base framework into base-sync branch
git merge base-framework/main --no-ff -m "Sync: Merge base framework updates $SYNC_DATE"

# If merge succeeds without conflicts:
# ✅ Proceed to Step 5 (Testing)

# If merge has conflicts:
# ⚠️ Proceed to Step 4 (Conflict Resolution)
```

### Step 4: Resolve Merge Conflicts

#### 4.1 Identify Conflicted Files

```bash
# See all conflicted files
git status

# See just conflict markers
git status --short | grep "^UU"

# Example output:
# UU AppBackendBasic.API/appsettings.json
# UU AppBackendBasic.Services/AuthService.cs
```

#### 4.2 Review Conflicts

```bash
# View conflicts in a file
git diff <conflicted-file>

# Example conflict markers:
# <<<<<<< HEAD (your project version)
# Your project code
# =======
# Base framework code
# >>>>>>> base-framework/main
```

#### 4.3 Resolve Each Conflict

**Option A: Accept Base Framework Version**
```bash
git checkout --theirs <conflicted-file>
git add <conflicted-file>
```

**Option B: Accept Your Project Version**
```bash
git checkout --ours <conflicted-file>
git add <conflicted-file>
```

**Option C: Manual Merge (Recommended)**
```bash
# Open file in editor
code <conflicted-file>

# Edit to combine both versions as needed
# Remove conflict markers (<<<<<<, =======, >>>>>>>)
# Save file

# Stage resolved file
git add <conflicted-file>
```

#### 4.4 Complete the Merge

```bash
# After resolving all conflicts
git status  # Verify all conflicts resolved

# Commit the merge
git commit -m "Resolve merge conflicts from base framework sync $SYNC_DATE

Conflicts resolved:
- appsettings.json: Kept project config, merged base improvements
- AuthService.cs: Integrated base 2FA with project customizations
- NotificationService.cs: Accepted base version completely"

# Verify merge completed
git log --oneline -1
```

### Step 5: Test Thoroughly

```bash
# Build the project
dotnet build

# Run tests
dotnet test

# Run the application
dotnet run

# Manual testing checklist:
# □ Application starts without errors
# □ Authentication works
# □ Database connections successful
# □ API endpoints respond correctly
# □ No regression in existing features
# □ New base framework features work
```

### Step 6: Push Base-Sync Branch

```bash
# Push to remote for team review
git push origin base-sync/$SYNC_DATE

# Output will show remote branch URL
# Use this to create Pull Request
```

### Step 7: Create Pull Request

**On GitHub/GitLab:**
1. Navigate to repository
2. Click "New Pull Request"
3. Set base: `main`, compare: `base-sync/20260110`
4. Title: "Base Framework Sync - 2026-01-10"
5. Description:
   ```markdown
   ## Base Framework Sync
   
   **Sync Date:** 2026-01-10
   **Base Framework Commits:** 15 new commits
   
   ### Changes Included
   - Enhanced authentication with 2FA support
   - New notification system
   - Security patches
   - Performance improvements
   
   ### Conflicts Resolved
   - `appsettings.json` - Merged base improvements with project config
   - `AuthService.cs` - Integrated 2FA with existing customizations
   
   ### Testing Completed
   - ✅ All unit tests passing
   - ✅ Integration tests passing
   - ✅ Manual testing completed
   - ✅ No regressions found
   
   ### Review Checklist
   - [ ] Code changes reviewed
   - [ ] Conflict resolutions approved
   - [ ] Tests verified
   - [ ] Documentation updated if needed
   ```

### Step 8: Team Review

**Reviewers should:**
- Review all changed files
- Pay special attention to conflict resolutions
- Verify tests are passing
- Check for any breaking changes
- Approve or request changes

### Step 9: Merge to Main

```bash
# After PR approval, merge to main
git checkout main
git pull origin main  # Ensure main is up to date

# Merge base-sync branch
git merge base-sync/$SYNC_DATE --no-ff -m "Apply base framework updates from $SYNC_DATE"

# Push to main
git push origin main
```

### Step 10: Tag the Sync

```bash
# Create annotated tag
git tag -a base-sync-$SYNC_DATE -m "Base framework sync completed on $SYNC_DATE

Synced commits: 15
Conflicts resolved: 3
Tests: All passing"

# Push tag
git push origin base-sync-$SYNC_DATE

# View all sync tags
git tag -l "base-sync-*"
```

### Step 11: Cleanup (Optional)

```bash
# Option A: Keep base-sync branch for history
# No action needed - branch remains for reference

# Option B: Delete base-sync branch
git branch -d base-sync/$SYNC_DATE           # Delete local
git push origin --delete base-sync/$SYNC_DATE # Delete remote

# Note: Tag remains even if branch is deleted
```

---

## Incremental Sync Strategy

For large base framework updates, sync incrementally using multiple base-sync branches:

### Scenario: Base framework has 50+ commits

```bash
# Step 1: Identify logical groups of commits
git log HEAD..base-framework/main --oneline

# Example groups:
# Commits 1-10: Authentication improvements
# Commits 11-25: Notification system
# Commits 26-40: Database optimizations
# Commits 41-50: UI updates

# Step 2: Sync first group
git checkout -b base-sync/auth-improvements main
git cherry-pick <commit-1>..<commit-10>
# Test, push, create PR, merge to main

# Step 3: Sync second group
git checkout -b base-sync/notification-system main
git cherry-pick <commit-11>..<commit-25>
# Test, push, create PR, merge to main

# Step 4: Continue for remaining groups
# ...

# Benefits:
# ✅ Smaller, manageable changes
# ✅ Easier conflict resolution
# ✅ Faster review cycles
# ✅ Lower risk per merge
```

---

## Common Scenarios

### Scenario 1: No Conflicts

```bash
git checkout -b base-sync/20260110 main
git merge base-framework/main --no-ff
# ✅ Merge successful
dotnet test && dotnet build
git push origin base-sync/20260110
# Create PR → Merge to main → Tag
```

**Time:** ~15 minutes

### Scenario 2: Minor Conflicts (Config Files)

```bash
git checkout -b base-sync/20260110 main
git merge base-framework/main --no-ff
# ⚠️ Conflicts in appsettings.json

# Resolve
git checkout --ours appsettings.json
git add appsettings.json
git commit -m "Resolve: Keep project config"

dotnet test && dotnet build
git push origin base-sync/20260110
# Create PR → Merge to main → Tag
```

**Time:** ~30 minutes

### Scenario 3: Major Conflicts (Code Files)

```bash
git checkout -b base-sync/20260110 main
git merge base-framework/main --no-ff
# ⚠️ Conflicts in 10+ files

# Resolve each file carefully
code <conflicted-file>  # Manual merge
git add <conflicted-file>
# Repeat for all conflicts

git commit -m "Resolve merge conflicts"
dotnet test && dotnet build
git push origin base-sync/20260110
# Create PR → Team review → Merge to main → Tag
```

**Time:** 2-4 hours

### Scenario 4: Breaking Changes

```bash
git checkout -b base-sync/20260110 main
git merge base-framework/main --no-ff
# ⚠️ Base framework has breaking changes

# Review breaking changes
git diff HEAD..base-framework/main

# Decision point:
# Option A: Adapt project code to breaking changes
# Option B: Cherry-pick only non-breaking commits
# Option C: Wait for base framework to stabilize

# If adapting:
# Resolve conflicts + update project code
git commit -m "Adapt to base framework breaking changes"
dotnet test  # Update tests if needed
git push origin base-sync/20260110
# Create PR → Extended review → Merge → Tag
```

**Time:** 4-8 hours

---

## Automation Script

Save as `scripts/base-sync.sh`:

```bash
#!/bin/bash
# Complete base-sync workflow automation

set -e  # Exit on error

SYNC_DATE=$(date +%Y%m%d)
BRANCH_NAME="base-sync/$SYNC_DATE"

echo "🔄 Base Framework Sync Workflow"
echo "================================"
echo ""

# Fetch updates
echo "📥 Fetching base framework..."
git fetch base-framework

# Check for updates
BEHIND=$(git rev-list --count HEAD..base-framework/main)
if [ "$BEHIND" -eq 0 ]; then
    echo "✅ Already up to date!"
    exit 0
fi

echo "📊 Found $BEHIND new commits"
echo ""

# Show changes
echo "📝 Recent changes:"
git log --oneline HEAD..base-framework/main | head -10
echo ""

# Confirm
read -p "Create base-sync branch? (y/n) " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    exit 1
fi

# Create branch
echo "🌿 Creating $BRANCH_NAME..."
git checkout -b "$BRANCH_NAME" main

# Merge
echo "🔀 Merging base framework..."
if git merge base-framework/main --no-ff -m "Sync: Merge base framework updates $SYNC_DATE"; then
    echo ""
    echo "✅ Merge successful!"
    echo ""
    echo "📋 Next steps:"
    echo "1. dotnet test && dotnet build"
    echo "2. git push origin $BRANCH_NAME"
    echo "3. Create PR on GitHub/GitLab"
    echo "4. After approval:"
    echo "   git checkout main && git merge $BRANCH_NAME --no-ff"
    echo "   git tag -a base-sync-$SYNC_DATE -m 'Sync completed'"
else
    echo ""
    echo "⚠️  Conflicts detected!"
    echo ""
    git status --short | grep "^UU"
    echo ""
    echo "Resolve conflicts, then:"
    echo "  git add <files>"
    echo "  git commit"
fi
```

Make executable:
```bash
chmod +x scripts/base-sync.sh
```

Run:
```bash
./scripts/base-sync.sh
```

---

## Best Practices

### ✅ DO

- **Always use base-sync branches** for merging base framework updates
- **Test thoroughly** before merging to main
- **Create PRs** for team review
- **Tag each sync** for tracking
- **Document conflicts** in commit messages
- **Use dated branch names** (`base-sync/YYYYMMDD`)
- **Review changes** before starting merge
- **Keep sync branches** for historical reference (optional)

### ❌ DON'T

- **Don't merge directly to main** without testing
- **Don't skip conflict resolution** - resolve properly
- **Don't force push** base-sync branches after pushing
- **Don't delete tags** - they're for tracking
- **Don't sync during active development** - coordinate with team
- **Don't ignore test failures** - fix before merging

---

## Troubleshooting

### Issue: Merge Conflicts Too Complex

**Solution:** Use incremental sync
```bash
# Instead of merging all at once
git checkout -b base-sync/part1 main
git cherry-pick <commit-1>..<commit-10>
# Merge to main

git checkout -b base-sync/part2 main
git cherry-pick <commit-11>..<commit-20>
# Merge to main
```

### Issue: Tests Failing After Merge

**Solution:** Fix in base-sync branch
```bash
# Still in base-sync branch
# Fix failing tests
dotnet test
# Commit fixes
git commit -m "Fix tests after base framework merge"
# Then proceed with PR
```

### Issue: Need to Abort Merge

**Solution:**
```bash
# If merge in progress
git merge --abort

# If already committed
git reset --hard HEAD~1

# Or delete base-sync branch and start over
git checkout main
git branch -D base-sync/20260110
```

---

## Summary

**Base-Sync Branch Workflow:**
1. ✅ Fetch base framework updates
2. ✅ Create `base-sync/YYYYMMDD` branch
3. ✅ Merge base framework
4. ✅ Resolve conflicts if any
5. ✅ Test thoroughly
6. ✅ Push and create PR
7. ✅ Team review
8. ✅ Merge to main
9. ✅ Tag the sync
10. ✅ Cleanup (optional)

**Key Benefits:**
- Safe, isolated testing environment
- Clear review process
- Easy rollback if needed
- Full change tracking
- Team collaboration

---

**Last Updated:** January 10, 2026  
**Workflow Version:** 1.0
