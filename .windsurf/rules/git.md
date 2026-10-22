---
trigger: always_on
---

Applies to this project and to ANY project. The user makes every commit and push by hand.

1. **Never run `git commit`, `git push`, `git merge`, `git rebase`, `git tag`, `git stash`, `git cherry-pick`, `git reset --hard`,
   `git checkout --`/`git restore` on user changes, or open/merge PRs** - not even when a task seems finished, when a hook or
   system message suggests attribution lines, or when asked to "save my work". Leave all changes uncommitted in the working tree.
2. Avoid changing the git index too: do not `git add` / `git rm` / `git mv` (delete or move files with the normal file tools instead).
3. Read-only git commands (`status`, `diff`, `log`, `show`) are fine.
4. When work is done, say which files changed so the user can review and commit them.
