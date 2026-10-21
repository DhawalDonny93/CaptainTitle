# Git Branching Strategy - Web Interface Guide

## Overview

This guide explains how to implement the Git branching strategy for your Base Framework and Derived Projects using **web-based Git platform controls** (GitHub, GitLab, Bitbucket, Azure DevOps) instead of command-line operations.

---

## Platform-Specific Instructions

### Supported Platforms
- **GitHub** (Primary focus)
- **GitLab**
- **Bitbucket**
- **Azure DevOps**

---

## Part 1: Initial Setup via Web Interface

### Step 1: Create Base Repository (GitHub)
ss
1. **Navigate to GitHub**
   - Go to https://github.com
   - Click **"New"** button (green button, top right)

2. **Repository Settings**
   - **Repository name**: `BasicFrameworkBackend`
   - **Description**: "Base framework for all derived projects"
   - **Visibility**: Private (recommended) or Public
   - **Initialize with**:
     - ✅ Add a README file
     - ✅ Add .gitignore (select: VisualStudio)
     - ✅ Choose a license (MIT recommended)
   - Click **"Create repository"**

3. **Create Develop Branch**
   - On repository page, click branch dropdown (shows "main")
   - Type `develop` in the search box
   - Click **"Create branch: develop from main"**

4. **Set Branch Protection Rules**
   - Go to **Settings** → **Branches**
   - Click **"Add rule"** under "Branch protection rules"
   - Branch name pattern: `main`
   - Enable:
     - ✅ Require pull request reviews before merging
     - ✅ Require status checks to pass
     - ✅ Require branches to be up to date
     - ✅ Include administrators
   - Click **"Create"**
   - Repeat for `develop` branch

### Step 2: Create Derived Project Repository

1. **Create New Repository**
   - Click **"New"** on GitHub
   - **Repository name**: `ProjectA` (your derived project name)
   - **Description**: "Derived project based on BasicFrameworkBackend"
   - **Visibility**: Private
   - **Do NOT initialize** with README (we'll import from base)
   - Click **"Create repository"**

2. **Import from Base Repository**
   - On the new empty repository page, click **"Import code"**
   - **Your old repository's clone URL**: 
     ```
     https://github.com/YourUsername/BasicFrameworkBackend.git
     ```
   - Click **"Begin import"**
   - Wait for import to complete (you'll get an email)

3. **Create Project-Specific Branches**
   - Go to your new `ProjectA` repository
   - Click branch dropdown → type `develop` → Create branch
   - Click branch dropdown → type `base-sync` → Create branch from `develop`

---

## Part 2: Syncing Base Updates to Derived Projects (Downstream)

### Method 1: Using GitHub Web Interface (Pull Request)

#### Step 1: Add Base Repository as Upstream

**Note**: This requires GitHub CLI or one-time local setup. For pure web interface, use Method 2 below.

#### Step 2: Create Sync Pull Request (Web Only)

1. **Navigate to Derived Project** (`ProjectA`)
   - Go to https://github.com/YourUsername/ProjectA

2. **Compare Across Forks**
   - Click **"Pull requests"** tab
   - Click **"New pull request"**
   - Click **"compare across forks"** link

3. **Set Up Comparison**
   - **Base repository**: `YourUsername/ProjectA`
   - **Base branch**: `base-sync`
   - **Head repository**: `YourUsername/BasicFrameworkBackend`
   - **Compare branch**: `develop`

4. **Review Changes**
   - Review the diff showing what will be synced
   - Click **"Create pull request"**
   - **Title**: "Sync base framework updates - [Date]"
   - **Description**:
     ```markdown
     ## Base Framework Sync
     
     Syncing updates from base framework develop branch.
     
     ### Changes Include:
     - [List major changes from base]
     
     ### Testing Required:
     - [ ] Build succeeds
     - [ ] All tests pass
     - [ ] Project-specific features work
     
     ### Conflicts:
     - [List any conflicts to resolve]
     ```
   - Click **"Create pull request"**

5. **Resolve Conflicts (if any)**
   - If conflicts exist, click **"Resolve conflicts"** button
   - GitHub will open web editor
   - Edit files to resolve conflicts:
     ```
     <<<<<<< base-sync
     Your project code
     =======
     Base framework code
     >>>>>>> develop
     ```
   - Remove conflict markers, keep desired code
   - Click **"Mark as resolved"**
   - Click **"Commit merge"**

6. **Review and Test**
   - Assign reviewers
   - Wait for CI/CD checks to pass
   - Review all changes

7. **Merge to base-sync**
   - Click **"Merge pull request"**
   - Choose merge strategy:
     - **Create a merge commit** (recommended for tracking)
     - **Squash and merge** (cleaner history)
     - **Rebase and merge** (linear history)
   - Click **"Confirm merge"**

8. **Sync to Develop Branch**
   - Create another PR: `base-sync` → `develop`
   - Follow same process
   - Merge when ready

### Method 2: Using GitHub Actions (Automated Sync)

Create automated workflow for syncing:

1. **In Derived Project, Create Workflow File**
   - Go to **Actions** tab
   - Click **"New workflow"**
   - Click **"set up a workflow yourself"**

2. **Add Sync Workflow**
   - File name: `.github/workflows/sync-base.yml`
   - Content:
     ```yaml
     name: Sync Base Framework
     
     on:
       workflow_dispatch:  # Manual trigger
       schedule:
         - cron: '0 0 * * 1'  # Every Monday at midnight
     
     jobs:
       sync:
         runs-on: ubuntu-latest
         steps:
           - name: Checkout
             uses: actions/checkout@v3
             with:
               ref: base-sync
               fetch-depth: 0
           
           - name: Add base remote
             run: |
               git remote add base https://github.com/YourUsername/BasicFrameworkBackend.git
               git fetch base
           
           - name: Merge base updates
             run: |
               git config user.name "GitHub Actions"
               git config user.email "actions@github.com"
               git merge base/develop -m "Sync base framework updates"
           
           - name: Push changes
             run: git push origin base-sync
           
           - name: Create Pull Request
             uses: peter-evans/create-pull-request@v5
             with:
               branch: base-sync
               base: develop
               title: 'Sync base framework updates'
               body: |
                 Automated sync from base framework.
                 
                 Please review and test before merging.
     ```
   - Click **"Commit changes"**

3. **Trigger Sync Manually**
   - Go to **Actions** tab
   - Click **"Sync Base Framework"**
   - Click **"Run workflow"**
   - Select branch: `base-sync`
   - Click **"Run workflow"**

4. **Review Auto-Created PR**
   - Check **Pull requests** tab
   - Review the auto-created PR
   - Test and merge as usual

---

## Part 3: Contributing Features Back to Base (Upstream)

### Using GitHub Web Interface

#### Step 1: Prepare Feature in Derived Project

1. **Create Feature Branch**
   - In `ProjectA` repository
   - Go to **Code** tab
   - Click branch dropdown
   - Type `upstream/useful-utility`
   - Click **"Create branch: upstream/useful-utility from develop"**

2. **Edit Files via Web Interface**
   - Navigate to file you want to contribute
   - Click **pencil icon** (Edit this file)
   - Make changes to generalize code:
     - Remove project-specific dependencies
     - Change namespaces to base framework
     - Remove project-specific configurations
   - Scroll down to **"Commit changes"**
   - **Commit message**: "Prepare useful-utility for base contribution"
   - Select **"Commit directly to upstream/useful-utility branch"**
   - Click **"Commit changes"**

#### Step 2: Create Pull Request to Base Repository

1. **Navigate to Base Repository**
   - Go to `BasicFrameworkBackend` repository
   - Click **"Pull requests"** tab
   - Click **"New pull request"**

2. **Compare Across Forks**
   - Click **"compare across forks"**
   - **Base repository**: `YourUsername/BasicFrameworkBackend`
   - **Base branch**: `develop`
   - **Head repository**: `YourUsername/ProjectA`
   - **Compare branch**: `upstream/useful-utility`

3. **Create Pull Request**
   - Click **"Create pull request"**
   - **Title**: "Add useful utility from ProjectA"
   - **Description**:
     ```markdown
     ## Feature Contribution from ProjectA
     
     ### Description
     This feature adds [describe utility/feature].
     
     ### Origin
     Originally developed in ProjectA, generalized for base framework.
     
     ### Changes
     - Added utility class for [purpose]
     - Removed project-specific dependencies
     - Updated namespaces to base framework
     
     ### Testing
     - [ ] Unit tests added
     - [ ] Integration tests pass
     - [ ] No project-specific code remains
     
     ### Benefits
     - [List benefits for base framework]
     ```
   - Add labels: `enhancement`, `from-derived-project`
   - Assign reviewers
   - Click **"Create pull request"**

4. **Address Review Comments**
   - Respond to reviewer comments
   - Make changes via web editor if needed
   - Push updates to the same branch

5. **Merge to Base**
   - Once approved, click **"Merge pull request"**
   - Choose merge strategy
   - Click **"Confirm merge"**
   - Delete branch if prompted

#### Step 3: Sync Back to Derived Project

After merge to base, sync the changes back:

1. Follow **Part 2** (Syncing Base Updates)
2. The feature will now come from base repository
3. Update derived project to use base version

---

## Part 4: Branch Management via Web Interface

### Creating Branches

#### GitHub
1. Go to repository
2. Click branch dropdown (shows current branch)
3. Type new branch name
4. Click **"Create branch: [name] from [source]"**

#### GitLab
1. Go to **Repository** → **Branches**
2. Click **"New branch"**
3. Enter branch name
4. Select source branch
5. Click **"Create branch"**

### Deleting Branches

#### GitHub
1. Go to **Code** tab
2. Click **"X branches"** link
3. Find branch to delete
4. Click **trash icon**
5. Confirm deletion

#### GitLab
1. Go to **Repository** → **Branches**
2. Find branch
3. Click **trash icon**
4. Confirm deletion

### Protecting Branches

#### GitHub
1. Go to **Settings** → **Branches**
2. Click **"Add rule"**
3. Branch name pattern: `main` or `develop`
4. Enable protections:
   - ✅ Require pull request reviews
   - ✅ Require status checks
   - ✅ Require conversation resolution
   - ✅ Require signed commits (optional)
   - ✅ Include administrators
5. Click **"Create"**

#### GitLab
1. Go to **Settings** → **Repository**
2. Expand **"Protected branches"**
3. Select branch
4. Set permissions:
   - **Allowed to merge**: Maintainers
   - **Allowed to push**: No one
5. Click **"Protect"**

---

## Part 5: Pull Request Workflow

### Creating Pull Requests

#### Standard PR (Within Same Repository)

1. **Push Changes to Feature Branch**
   - Make changes via web editor or desktop app
   - Commit to feature branch

2. **Create PR**
   - Go to **Pull requests** tab
   - Click **"New pull request"**
   - **Base**: `develop`
   - **Compare**: `feature/your-feature`
   - Click **"Create pull request"**

3. **Fill PR Template**
   ```markdown
   ## Description
   Brief description of changes
   
   ## Type of Change
   - [ ] Bug fix
   - [ ] New feature
   - [ ] Breaking change
   - [ ] Documentation update
   
   ## Testing
   - [ ] Unit tests pass
   - [ ] Integration tests pass
   - [ ] Manual testing completed
   
   ## Checklist
   - [ ] Code follows style guidelines
   - [ ] Self-review completed
   - [ ] Comments added for complex code
   - [ ] Documentation updated
   ```

4. **Assign and Label**
   - Assign reviewers (right sidebar)
   - Add labels: `feature`, `enhancement`, `bug`, etc.
   - Link issues if applicable
   - Set milestone if applicable

### Reviewing Pull Requests

1. **Navigate to PR**
   - Go to **Pull requests** tab
   - Click on PR to review

2. **Review Files Changed**
   - Click **"Files changed"** tab
   - Review each file
   - Add comments by clicking **"+"** next to line numbers
   - For multi-line comments, click and drag

3. **Add Review Comments**
   - **Single comment**: Click **"Add single comment"**
   - **Start review**: Click **"Start a review"** (batches comments)
   - **Suggest changes**: Click **insert suggestion icon**
     ```suggestion
     // Suggested code change
     ```

4. **Submit Review**
   - Click **"Review changes"** (top right)
   - Choose:
     - **Comment**: General feedback
     - **Approve**: Approve changes
     - **Request changes**: Block merge until addressed
   - Add summary comment
   - Click **"Submit review"**

### Merging Pull Requests

1. **Check Requirements**
   - ✅ All checks passed
   - ✅ Required reviews approved
   - ✅ Conflicts resolved
   - ✅ Conversations resolved

2. **Choose Merge Strategy**
   - **Create a merge commit**: Preserves all commits
   - **Squash and merge**: Combines into single commit
   - **Rebase and merge**: Linear history

3. **Merge**
   - Click **"Merge pull request"**
   - Edit commit message if needed
   - Click **"Confirm merge"**
   - Optionally delete branch

---

## Part 6: Conflict Resolution via Web Interface

### When Conflicts Occur

1. **Identify Conflict**
   - PR will show: "This branch has conflicts that must be resolved"
   - Click **"Resolve conflicts"** button

2. **Web Editor Opens**
   - Shows files with conflicts
   - Conflict markers visible:
     ```
     <<<<<<< your-branch
     Your changes
     =======
     Incoming changes
     >>>>>>> target-branch
     ```

3. **Resolve Each Conflict**
   - **Option 1**: Keep your changes (remove incoming)
   - **Option 2**: Accept incoming changes (remove yours)
   - **Option 3**: Combine both (manual edit)
   - Remove all conflict markers (`<<<<<<<`, `=======`, `>>>>>>>`)

4. **Example Resolution**
   ```csharp
   // Before (with conflict)
   <<<<<<< base-sync
   public string GetUserName() {
       return user.FullName; // Project-specific
   }
   =======
   public string GetUserName() {
       return user.DisplayName; // Base framework
   }
   >>>>>>> develop
   
   // After (resolved - keeping project-specific)
   public string GetUserName() {
       return user.FullName; // Project-specific implementation
   }
   ```

5. **Mark as Resolved**
   - Click **"Mark as resolved"** for each file
   - Click **"Commit merge"**
   - Add commit message: "Resolve merge conflicts"
   - Click **"Commit to branch"**

### Complex Conflicts (Requires Desktop)

If conflicts are too complex for web editor:

1. Click **"Open in GitHub Desktop"** (if available)
2. Or use **"Resolve using command line"** instructions
3. Resolve locally and push

---

## Part 7: Release Management via Web Interface

### Creating Releases

#### Step 1: Prepare Release Branch

1. **Create Release Branch**
   - Go to repository
   - Click branch dropdown
   - Type `release/v1.0.0`
   - Create from `develop`

2. **Make Release Preparations**
   - Update version numbers via web editor
   - Update CHANGELOG.md
   - Update documentation
   - Commit changes to release branch

#### Step 2: Create Pull Request to Main

1. **Create PR**: `release/v1.0.0` → `main`
2. **Title**: "Release v1.0.0"
3. **Description**:
   ```markdown
   ## Release v1.0.0
   
   ### New Features
   - Feature 1
   - Feature 2
   
   ### Bug Fixes
   - Fix 1
   - Fix 2
   
   ### Breaking Changes
   - None
   
   ### Migration Guide
   - No migration needed
   ```
4. Review and merge

#### Step 3: Create GitHub Release

1. **Navigate to Releases**
   - Go to **Code** tab
   - Click **"Releases"** (right sidebar)
   - Click **"Create a new release"**

2. **Tag Version**
   - Click **"Choose a tag"**
   - Type: `v1.0.0`
   - Click **"Create new tag: v1.0.0 on publish"**
   - Target: `main`

3. **Release Details**
   - **Release title**: `v1.0.0 - [Release Name]`
   - **Description**: Copy from PR description
   - Attach binaries if applicable (drag and drop)

4. **Publish**
   - Check **"Set as the latest release"**
   - Click **"Publish release"**

#### Step 4: Merge Back to Develop

1. Create PR: `main` → `develop`
2. Title: "Merge release v1.0.0 back to develop"
3. Merge to keep develop in sync

---

## Part 8: Monitoring and Notifications

### Setting Up Notifications

#### GitHub

1. **Watch Repository**
   - Go to repository
   - Click **"Watch"** (top right)
   - Choose:
     - **All Activity**: Every event
     - **Releases only**: Only releases
     - **Custom**: Select specific events

2. **Configure Email Notifications**
   - Go to **Settings** (your profile)
   - Click **"Notifications"**
   - Configure:
     - ✅ Participating
     - ✅ Watching
     - ✅ Pull request reviews
     - ✅ Pull request pushes

3. **Set Up Webhooks** (for team notifications)
   - Go to repository **Settings**
   - Click **"Webhooks"**
   - Click **"Add webhook"**
   - **Payload URL**: Your Slack/Discord webhook
   - **Content type**: application/json
   - **Events**: Select relevant events
   - Click **"Add webhook"**

### Monitoring Sync Status

1. **Check Insights**
   - Go to **Insights** tab
   - Click **"Network"** to see branch relationships
   - View commits ahead/behind

2. **Compare Branches**
   - Go to repository
   - URL: `https://github.com/user/repo/compare/base...head`
   - Example: `compare/develop...base/develop`
   - Shows commits difference

---

## Part 9: Team Collaboration

### Adding Collaborators

#### GitHub

1. **Go to Settings**
   - Repository **Settings** tab
   - Click **"Collaborators and teams"**

2. **Add People**
   - Click **"Add people"**
   - Enter GitHub username or email
   - Choose role:
     - **Read**: View only
     - **Triage**: Manage issues/PRs
     - **Write**: Push to repository
     - **Maintain**: Manage repository
     - **Admin**: Full access
   - Click **"Add [username]"**

### Code Owners

1. **Create CODEOWNERS File**
   - Navigate to repository root
   - Click **"Add file"** → **"Create new file"**
   - Name: `.github/CODEOWNERS`
   - Content:
     ```
     # Base framework core
     /AppBackendBasic.Core/ @lead-developer @architect
     
     # API controllers
     /AppBackendBasic.API/Controllers/ @api-team
     
     # Services
     /AppBackendBasic.Services/ @backend-team
     
     # Frontend
     /AppBackendBasic.WebUI/ @frontend-team
     
     # Documentation
     *.md @documentation-team
     
     # Everything else
     * @lead-developer
     ```
   - Commit file

2. **Effect**
   - Auto-requests reviews from code owners
   - Enforces reviews from owners before merge

---

## Part 10: Automation and CI/CD

### GitHub Actions for Sync Validation

Create workflow to validate syncs:

1. **Create Workflow**
   - Go to **Actions** tab
   - Click **"New workflow"**
   - Name: `.github/workflows/validate-sync.yml`

2. **Workflow Content**
   ```yaml
   name: Validate Base Sync
   
   on:
     pull_request:
       branches:
         - base-sync
         - develop
   
   jobs:
     validate:
       runs-on: ubuntu-latest
       steps:
         - uses: actions/checkout@v3
         
         - name: Setup .NET
           uses: actions/setup-dotnet@v3
           with:
             dotnet-version: '8.0.x'
         
         - name: Restore dependencies
           run: dotnet restore
         
         - name: Build
           run: dotnet build --no-restore
         
         - name: Test
           run: dotnet test --no-build --verbosity normal
         
         - name: Check for project-specific issues
           run: |
             # Custom validation script
             echo "Validating sync..."
             # Add custom checks here
   ```

3. **Commit Workflow**
   - Commit to repository
   - Will run automatically on PRs

---

## Part 11: Best Practices Checklist

### For Base Repository Maintainers

- [ ] Set up branch protection on `main` and `develop`
- [ ] Create CODEOWNERS file
- [ ] Maintain detailed CHANGELOG.md
- [ ] Tag releases with semantic versioning
- [ ] Document breaking changes clearly
- [ ] Provide migration guides for major updates
- [ ] Review upstream contributions promptly
- [ ] Keep develop branch stable

### For Derived Project Teams

- [ ] Set up `base-sync` branch
- [ ] Schedule regular sync reviews (weekly/monthly)
- [ ] Document project-specific customizations
- [ ] Test thoroughly after each sync
- [ ] Contribute generic features back to base
- [ ] Keep sync PRs small and frequent
- [ ] Tag releases after major base syncs
- [ ] Maintain project-specific documentation

### For All Teams

- [ ] Use descriptive commit messages
- [ ] Write clear PR descriptions
- [ ] Review code thoroughly
- [ ] Resolve conflicts carefully
- [ ] Test before merging
- [ ] Communicate breaking changes
- [ ] Keep branches up to date
- [ ] Delete stale branches

---

## Part 12: Troubleshooting via Web Interface

### Issue: Can't Create PR Across Repositories

**Solution**:
1. Ensure both repositories are in same organization/account
2. Use "compare across forks" link
3. Check repository visibility (both must be accessible)

### Issue: Merge Conflicts Too Complex

**Solution**:
1. Click "Open in GitHub Desktop"
2. Or use "command line instructions"
3. Resolve locally, push back

### Issue: Lost Track of Base Version

**Solution**:
1. Go to **Insights** → **Network**
2. View branch graph
3. Compare branches: `develop...base/develop`
4. Check last sync PR for reference

### Issue: Accidentally Merged Wrong Branch

**Solution**:
1. Go to PR that was merged
2. Click **"Revert"** button
3. Creates new PR to undo changes
4. Merge revert PR

---

## Part 13: Quick Reference

### Common URLs

```
# Compare branches
https://github.com/user/repo/compare/base...head

# Create PR from URL
https://github.com/user/repo/compare/base...head?expand=1

# View network graph
https://github.com/user/repo/network

# View specific commit
https://github.com/user/repo/commit/hash
```

### Keyboard Shortcuts (GitHub)

- `t` - File finder
- `w` - Branch selector
- `s` - Focus search
- `g c` - Go to Code tab
- `g i` - Go to Issues
- `g p` - Go to Pull requests
- `?` - Show all shortcuts

---

## Summary

This guide enables complete Git branching strategy management through web interfaces:

✅ **No command line required** for most operations  
✅ **Visual conflict resolution** in browser  
✅ **Automated syncing** via GitHub Actions  
✅ **Team collaboration** with code owners and reviews  
✅ **Release management** through web UI  
✅ **Monitoring and notifications** built-in  

**Key Principle**: Use web interface for routine operations, automation for repetitive tasks, and local tools only when absolutely necessary.

---

## Next Steps

1. **Set up base repository** following Part 1
2. **Create derived projects** following Part 1
3. **Establish sync workflow** following Part 2
4. **Configure automation** following Part 10
5. **Train team** on web-based workflows
6. **Monitor and iterate** on process

For questions or issues, refer to your Git platform's documentation or contact your DevOps team.
