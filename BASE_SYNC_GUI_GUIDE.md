# Base Framework Sync - Simple GUI Guide

## For GitHub Desktop + GitHub.com (No Command Line!)

This guide shows you how to sync base framework updates to your derived project using **GitHub Desktop** and **GitHub.com** - no terminal commands needed!

---

## 🎯 What You'll Do

1. Check for base framework updates on GitHub.com
2. Create a base-sync branch in GitHub Desktop
3. Merge base framework changes
4. Resolve any conflicts (if needed)
5. Test your project
6. Create a Pull Request on GitHub.com
7. Merge to main after review

**Time:** 15-30 minutes (first time), 10 minutes (after practice)

---

## 📋 Prerequisites

### One-Time Setup

**1. Install GitHub Desktop**
- Download from: https://desktop.github.com/
- Install and sign in with your GitHub account

**2. Clone Your Derived Project**
- Open GitHub Desktop
- Click: `File` → `Clone Repository`
- Select your project (e.g., `ProjectA-Backend`)
- Choose location on your computer
- Click `Clone`

**3. Add Base Framework Remote**

This is the ONLY command-line step (one-time only):

**On Mac:**
- Open Terminal
- Navigate to your project:
  ```bash
  cd "/Users/YourName/Documents/GitHub/ProjectA-Backend"
  ```
- Add base framework:
  ```bash
  git remote add base-framework https://github.com/YourUsername/BasicFrameworkBackend.git
  ```

**On Windows:**
- Open Command Prompt
- Navigate to your project:
  ```cmd
  cd "C:\Users\YourName\Documents\GitHub\ProjectA-Backend"
  ```
- Add base framework:
  ```cmd
  git remote add base-framework https://github.com/YourUsername/BasicFrameworkBackend.git
  ```

**Verify it worked:**
- In GitHub Desktop, click `Repository` → `Repository Settings`
- Click `Remote` tab
- You should see two remotes:
  - `origin` (your project)
  - `base-framework` (the base template)

---

## 🔄 Step-by-Step Sync Process

### Step 1: Check for Base Framework Updates (GitHub.com)

**1.1 Open Base Framework Repository**
- Go to: `https://github.com/YourUsername/BasicFrameworkBackend`
- Look at recent commits on the main page
- Note the date of the latest commit

**1.2 Compare with Your Project**
- Go to: `https://github.com/YourUsername/ProjectA-Backend`
- Look at your latest commit date
- If base framework has newer commits → proceed with sync

**Visual Check:**
```
Base Framework: Last commit 2 days ago ← Newer
Your Project:   Last commit 1 week ago ← Older
→ You need to sync!
```

---

### Step 2: Fetch Base Framework Updates (GitHub Desktop)

**2.1 Open GitHub Desktop**
- Make sure your project is selected (top-left dropdown)
- Current branch should be `main`

**2.2 Fetch Updates**
- Click: `Repository` → `Fetch origin`
- This updates your local copy with remote changes

**2.3 Fetch Base Framework**
- Click: `Repository` → `Open in Terminal` (or Command Prompt)
- Type: `git fetch base-framework`
- Press Enter
- Close terminal
- Back in GitHub Desktop, click `Repository` → `Refresh`

---

### Step 3: Create Base-Sync Branch (GitHub Desktop)

**3.1 Create New Branch**
- Make sure you're on `main` branch (see top bar)
- Click: `Branch` → `New Branch`
- Name it: `base-sync/20260111` (use today's date: YYYYMMDD)
- Click: `Create Branch`

**Visual:**
```
┌─────────────────────────────────┐
│ Current Branch: main            │ ← Click here
└─────────────────────────────────┘
         ↓
┌─────────────────────────────────┐
│ New Branch                      │
│ Name: base-sync/20260111        │ ← Type here
│ [Create Branch]                 │ ← Click
└─────────────────────────────────┘
```

**3.2 Verify Branch Created**
- Top bar should now show: `Current Branch: base-sync/20260111`
- ✅ You're now working in a safe testing branch!

---

### Step 4: Merge Base Framework (GitHub Desktop)

**4.1 Start Merge**
- Click: `Branch` → `Merge into Current Branch`
- A list of branches appears

**4.2 Select Base Framework**
- Scroll down to find: `base-framework/main`
- Click on it to select
- Click: `Merge base-framework/main into base-sync/20260111`

**Visual:**
```
┌─────────────────────────────────┐
│ Merge into base-sync/20260111   │
│                                 │
│ 🔍 Filter branches...           │
│                                 │
│ ☐ origin/main                   │
│ ☐ origin/develop                │
│ ☑ base-framework/main  ← Select │ ← 15 commits ahead
│                                 │
│ [Merge]                         │ ← Click
└─────────────────────────────────┘
```

**4.3 Two Possible Outcomes:**

**✅ Outcome A: Merge Successful (No Conflicts)**
- GitHub Desktop shows: "Successfully merged"
- Skip to Step 6 (Testing)

**⚠️ Outcome B: Merge Conflicts**
- GitHub Desktop shows: "Resolve conflicts before merging"
- Number of conflicted files shown
- Proceed to Step 5

---

### Step 5: Resolve Conflicts (If Needed)

**5.1 View Conflicted Files**
- GitHub Desktop shows list of files with conflicts
- Files marked with ⚠️ warning icon

**Example:**
```
⚠️ AppBackendBasic.API/appsettings.json
⚠️ AppBackendBasic.Services/AuthService.cs
✓ AppBackendBasic.Core/User.cs (no conflict)
```

**5.2 Open Conflicted File**
- Right-click on conflicted file
- Click: `Open in Visual Studio Code` (or your editor)

**5.3 Understand Conflict Markers**

When you open the file, you'll see:
```csharp
public class AuthService
{
<<<<<<< HEAD (Current Change - Your Project)
    // Your project's version
    public bool Login(string username, string password)
    {
        return ValidateUser(username, password);
    }
=======
    // Base framework's version
    public bool Login(string username, string password, bool use2FA)
    {
        return ValidateUser(username, password, use2FA);
    }
>>>>>>> base-framework/main (Incoming Change - Base Framework)
}
```

**What this means:**
- `<<<<<<< HEAD` = Your project's current code
- `=======` = Separator
- `>>>>>>> base-framework/main` = Base framework's new code

**5.4 Choose How to Resolve**

**Option A: Keep Your Version**
- Delete everything from `<<<<<<< HEAD` to `=======`
- Delete the line `>>>>>>> base-framework/main`
- Keep only your code

**Option B: Use Base Framework Version**
- Delete everything from `<<<<<<< HEAD` to `=======` (including your code)
- Delete the line `>>>>>>> base-framework/main`
- Keep only base framework code

**Option C: Combine Both (Recommended)**
- Manually edit to combine best of both
- Remove all conflict markers (`<<<<<<<`, `=======`, `>>>>>>>`)
- Save file

**Example Resolution:**
```csharp
public class AuthService
{
    // Combined version - best of both
    public bool Login(string username, string password, bool use2FA = false)
    {
        if (use2FA)
        {
            return ValidateUserWith2FA(username, password);
        }
        return ValidateUser(username, password);
    }
}
```

**5.5 Mark as Resolved in GitHub Desktop**
- Save the file in your editor
- Go back to GitHub Desktop
- The file should now show ✓ instead of ⚠️
- If it still shows ⚠️, click the checkbox next to the file

**5.6 Repeat for All Conflicted Files**
- Resolve each file one by one
- Mark each as resolved

**5.7 Complete the Merge**
- Once all files are resolved (all showing ✓)
- Click: `Commit merge` button (bottom-left)
- GitHub Desktop will create a merge commit

---

### Step 6: Test Your Project

**6.1 Build and Test**
- Open your project in Visual Studio or Rider
- Build the solution: `Build` → `Build Solution`
- Run tests: `Test` → `Run All Tests`
- Run the application: `Debug` → `Start Debugging`

**6.2 Manual Testing**
- Test login/authentication
- Test main features
- Check for errors in console
- Verify everything works

**If tests fail:**
- Fix issues in your code
- Commit fixes in GitHub Desktop
- Test again

---

### Step 7: Push Base-Sync Branch (GitHub Desktop)

**7.1 Push to GitHub**
- Click: `Publish branch` button (top-right)
- Or if already published: `Push origin`

**Visual:**
```
┌─────────────────────────────────┐
│ Current Branch: base-sync/...   │
│                                 │
│ [Publish branch] ← Click        │
└─────────────────────────────────┘
```

**7.2 Verify Push**
- GitHub Desktop shows: "Last fetched just now"
- No pending changes

---

### Step 8: Create Pull Request (GitHub.com)

**8.1 Open GitHub.com**
- Go to your project: `https://github.com/YourUsername/ProjectA-Backend`
- You'll see a yellow banner: "base-sync/20260111 had recent pushes"
- Click: `Compare & pull request` button

**Visual:**
```
┌──────────────────────────────────────────────────┐
│ 🟡 base-sync/20260111 had recent pushes         │
│    [Compare & pull request]  ← Click            │
└──────────────────────────────────────────────────┘
```

**8.2 Fill Out Pull Request**

**Title:**
```
Base Framework Sync - January 11, 2026
```

**Description:**
```markdown
## Base Framework Sync

**Sync Date:** 2026-01-11
**Base Framework Commits:** 15 new commits

### Changes Included
- Enhanced authentication with 2FA support
- New notification system
- Security patches
- Performance improvements

### Conflicts Resolved
- ✅ appsettings.json - Kept project config, merged improvements
- ✅ AuthService.cs - Integrated 2FA with existing code

### Testing Completed
- ✅ All tests passing
- ✅ Application runs without errors
- ✅ Manual testing completed

### Review Needed
Please review the changes, especially conflict resolutions.
```

**8.3 Set Base and Compare**
- **Base:** `main` (your project's main branch)
- **Compare:** `base-sync/20260111` (your sync branch)

**8.4 Create Pull Request**
- Click: `Create pull request` button
- PR is now created and ready for review

---

### Step 9: Review on GitHub.com

**9.1 View Changes**
- On the PR page, click: `Files changed` tab
- Review all modified files
- Green = Added lines
- Red = Removed lines

**9.2 Check Conflicts Were Resolved Correctly**
- Look for files you resolved conflicts in
- Verify the resolution makes sense
- Add comments if needed

**9.3 Request Team Review (Optional)**
- Click: `Reviewers` on the right sidebar
- Select team members
- They'll get notified to review

---

### Step 10: Merge to Main (GitHub.com)

**10.1 Wait for Approvals**
- If you requested reviews, wait for approval
- Or if working solo, proceed

**10.2 Merge Pull Request**
- Scroll to bottom of PR page
- Click: `Merge pull request` button
- Click: `Confirm merge`

**Visual:**
```
┌──────────────────────────────────────────────────┐
│ This branch has no conflicts with the base       │
│ branch                                           │
│                                                  │
│ [Merge pull request ▼]  ← Click                 │
│                                                  │
│ Merge method: Create a merge commit             │
└──────────────────────────────────────────────────┘
```

**10.3 Delete Base-Sync Branch (Optional)**
- After merge, GitHub shows: "Pull request successfully merged and closed"
- Click: `Delete branch` button
- This cleans up the base-sync branch (it's in history anyway)

---

### Step 11: Update Local Main (GitHub Desktop)

**11.1 Switch to Main Branch**
- In GitHub Desktop, click branch dropdown (top)
- Select: `main`

**11.2 Pull Latest Changes**
- Click: `Repository` → `Pull`
- This downloads the merged changes to your local main branch

**11.3 Verify Sync Complete**
- Your local `main` now has all base framework updates
- ✅ Sync complete!

---

## 🎉 You're Done!

Your project now has all the latest base framework improvements!

---

## 📊 Visual Summary

```
┌─────────────────────────────────────────────────────────┐
│                    SYNC WORKFLOW                        │
└─────────────────────────────────────────────────────────┘

1. GitHub.com
   └─→ Check base framework for updates

2. GitHub Desktop
   └─→ Fetch base-framework
   └─→ Create base-sync/YYYYMMDD branch
   └─→ Merge base-framework/main

3. If Conflicts:
   └─→ Open files in editor
   └─→ Remove conflict markers
   └─→ Save and mark resolved

4. GitHub Desktop
   └─→ Test project
   └─→ Push base-sync branch

5. GitHub.com
   └─→ Create Pull Request
   └─→ Review changes
   └─→ Merge to main

6. GitHub Desktop
   └─→ Switch to main
   └─→ Pull latest

✅ DONE!
```

---

## 🆘 Common Issues & Solutions

### Issue 1: Can't See base-framework/main in Merge List

**Solution:**
- Close GitHub Desktop
- Open Terminal/Command Prompt
- Navigate to project folder
- Run: `git fetch base-framework`
- Reopen GitHub Desktop

### Issue 2: Too Many Conflicts

**Solution:**
- Click: `Branch` → `Abort Merge`
- Try incremental sync instead:
  - Sync 5-10 commits at a time
  - Use cherry-pick (requires terminal)

### Issue 3: Tests Failing After Merge

**Solution:**
- Stay in base-sync branch
- Fix the failing tests
- Commit fixes
- Push again
- Then merge to main

### Issue 4: Accidentally Merged to Main Directly

**Solution:**
- Don't panic!
- If tests pass, you're okay
- If tests fail, revert:
  - GitHub.com → Go to commit
  - Click: `Revert` button
  - Create new PR to fix

---

## 💡 Pro Tips

### Tip 1: Sync Regularly
- Check for base framework updates weekly
- Smaller, frequent syncs = fewer conflicts

### Tip 2: Use Descriptive Branch Names
- ✅ Good: `base-sync/20260111`
- ✅ Good: `base-sync/auth-improvements`
- ❌ Bad: `sync`, `update`, `test`

### Tip 3: Test Before Merging
- Always test in base-sync branch first
- Never merge untested code to main

### Tip 4: Document Conflicts
- In PR description, list what conflicts you resolved
- Helps team understand changes

### Tip 5: Keep Base-Sync Branches
- Don't delete immediately after merge
- Keep for 1-2 weeks for reference
- Can delete later to clean up

---

## 📚 Quick Reference Card

**Print this and keep at your desk!**

```
╔════════════════════════════════════════════════════╗
║         BASE FRAMEWORK SYNC CHEAT SHEET           ║
╠════════════════════════════════════════════════════╣
║                                                    ║
║ 1. GitHub Desktop                                  ║
║    → Fetch base-framework                          ║
║    → Create branch: base-sync/YYYYMMDD             ║
║    → Merge base-framework/main                     ║
║                                                    ║
║ 2. If Conflicts:                                   ║
║    → Open file in editor                           ║
║    → Remove: <<<<<<<, =======, >>>>>>>            ║
║    → Save and mark resolved                        ║
║                                                    ║
║ 3. Test                                            ║
║    → Build solution                                ║
║    → Run tests                                     ║
║    → Run application                               ║
║                                                    ║
║ 4. GitHub Desktop                                  ║
║    → Push branch                                   ║
║                                                    ║
║ 5. GitHub.com                                      ║
║    → Create Pull Request                           ║
║    → Review → Merge                                ║
║                                                    ║
║ 6. GitHub Desktop                                  ║
║    → Switch to main                                ║
║    → Pull                                          ║
║                                                    ║
║ ✅ DONE!                                           ║
╚════════════════════════════════════════════════════╝
```

---

## 🎓 Practice Exercise

Try a test sync to get comfortable:

1. Create a test branch: `test-sync/practice`
2. Merge base-framework/main into it
3. If conflicts, resolve them
4. Push the branch
5. Create PR (but don't merge)
6. Review the changes
7. Close PR without merging
8. Delete test branch

This lets you practice without affecting your main branch!

---

## 📞 Need Help?

If you get stuck:

1. **Check this guide** - Read the relevant section again
2. **Check GitHub Desktop Help** - Help → GitHub Desktop Help
3. **Ask your team** - Share your screen and walk through together
4. **GitHub Support** - https://support.github.com/

---

**Last Updated:** January 11, 2026  
**Guide Version:** 1.0  
**Difficulty:** Beginner-Friendly ⭐⭐☆☆☆
