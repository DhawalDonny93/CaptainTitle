#!/bin/bash
# Quick script to clean Git repository of unnecessary files

echo "🧹 Git Repository Cleanup Tool"
echo "==============================="
echo ""

# Navigate to project root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$PROJECT_ROOT"

echo "📁 Project: $PROJECT_ROOT"
echo ""

# Show current status
echo "📊 Current repository size:"
du -sh .git 2>/dev/null || echo "Unable to determine size"
echo ""

# Check for ignored files being tracked
echo "🔍 Checking for unnecessary files in Git..."
echo ""

# Check for bin folders
BIN_FILES=$(git ls-files | grep -E "/(bin|obj)/" 2>/dev/null | wc -l)
echo "   bin/obj folders: $BIN_FILES files"

# Check for IDE files
IDE_FILES=$(git ls-files | grep -E "\.(user|suo)$|/\.vs/|/\.vscode/|/\.idea/" 2>/dev/null | wc -l)
echo "   IDE files: $IDE_FILES files"

# Check for compiled files
COMPILED_FILES=$(git ls-files | grep -E "\.(dll|exe|pdb)$" 2>/dev/null | wc -l)
echo "   Compiled files: $COMPILED_FILES files"

# Check for OS files
OS_FILES=$(git ls-files | grep -E "(\.DS_Store|Thumbs\.db|Desktop\.ini)$" 2>/dev/null | wc -l)
echo "   OS files: $OS_FILES files"

TOTAL_UNNECESSARY=$((BIN_FILES + IDE_FILES + COMPILED_FILES + OS_FILES))

echo ""
echo "   Total unnecessary files: $TOTAL_UNNECESSARY"
echo ""

if [ "$TOTAL_UNNECESSARY" -eq 0 ]; then
    echo "✅ Repository is clean! No unnecessary files found."
    exit 0
fi

# Show sample of files
echo "📝 Sample of files to be removed:"
git ls-files | grep -E "/(bin|obj)/|\.(user|suo|dll|exe|pdb)$|/\.vs/" | head -20
if [ "$(git ls-files | grep -E "/(bin|obj)/|\.(user|suo|dll|exe|pdb)$|/\.vs/" | wc -l)" -gt 20 ]; then
    echo "   ... and more"
fi
echo ""

# Ask for confirmation
read -p "❓ Remove these files from Git tracking? (y/n) " -n 1 -r
echo ""

if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "❌ Cleanup cancelled"
    exit 1
fi

# Remove ignored files
echo ""
echo "🗑️  Removing unnecessary files from Git tracking..."
echo ""

# Remove bin and obj folders
echo "   Removing bin/ and obj/ folders..."
git rm -r --cached "**/bin" "**/obj" 2>/dev/null || true

# Remove IDE files
echo "   Removing IDE files..."
git rm -r --cached "**/.vs" "**/.vscode" "**/.idea" 2>/dev/null || true
git rm --cached "**/*.user" "**/*.suo" 2>/dev/null || true

# Remove compiled files
echo "   Removing compiled files..."
git rm --cached "**/*.dll" "**/*.exe" "**/*.pdb" 2>/dev/null || true

# Remove OS files
echo "   Removing OS files..."
git rm --cached "**/.DS_Store" "**/Thumbs.db" "**/Desktop.ini" 2>/dev/null || true

echo ""
echo "✅ Files removed from Git tracking"
echo ""

# Show what will be committed
echo "📝 Changes to be committed:"
git status --short | head -30
if [ "$(git status --short | wc -l)" -gt 30 ]; then
    echo "   ... and more"
fi
echo ""

# Ask to commit
read -p "❓ Commit these changes? (y/n) " -n 1 -r
echo ""

if [[ $REPLY =~ ^[Yy]$ ]]; then
    git commit -m "Clean repository: Remove bin, obj, IDE, and compiled files from Git tracking

- Removed bin/ and obj/ build folders
- Removed IDE-specific files (.vs, .vscode, .idea, *.user, *.suo)
- Removed compiled files (*.dll, *.exe, *.pdb)
- Removed OS-specific files (.DS_Store, Thumbs.db)

These files are now properly ignored by .gitignore"
    
    echo ""
    echo "✅ Changes committed!"
    echo ""
    echo "📤 Next step: Push to remote"
    echo "   git push origin main"
else
    echo ""
    echo "⚠️  Changes staged but not committed"
    echo ""
    echo "To commit manually:"
    echo "   git commit -m 'Clean repository: Remove unnecessary files'"
    echo ""
    echo "To undo:"
    echo "   git reset HEAD"
fi

echo ""
echo "📊 New repository size:"
du -sh .git 2>/dev/null || echo "Unable to determine size"
echo ""
echo "✅ Cleanup complete!"
