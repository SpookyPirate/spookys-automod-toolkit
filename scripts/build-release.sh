#!/bin/bash
# Build Release Package for Spooky's AutoMod Toolkit
# Creates a zip with structure:
#   spookys-automod-toolkit-v1.x.x/
#   ├── .claude/
#   └── spookys-automod-toolkit/

set -e

VERSION="$1"
CLAUDE_SKILLS_PATH="${2:-../toolkit-release-assets/.claude}"
OUTPUT_DIR="${3:-../releases}"

if [ -z "$VERSION" ]; then
    echo "Usage: ./build-release.sh <version> [claude-skills-path] [output-dir]"
    echo "Example: ./build-release.sh 1.4.1"
    exit 1
fi

echo "Building Spooky's AutoMod Toolkit v$VERSION"

# Validate inputs
if [ ! -d "$CLAUDE_SKILLS_PATH" ]; then
    echo "Error: .claude skills folder not found at: $CLAUDE_SKILLS_PATH"
    exit 1
fi

# Create temp directory
TEMP_DIR="/tmp/spookys-automod-toolkit-release"
RELEASE_ROOT="$TEMP_DIR/spookys-automod-toolkit-v$VERSION"
TOOLKIT_DIR="$RELEASE_ROOT/spookys-automod-toolkit"

echo "Creating temporary directory..."
rm -rf "$TEMP_DIR"
mkdir -p "$RELEASE_ROOT"
mkdir -p "$TOOLKIT_DIR"

# Build the toolkit
echo "Building toolkit..."
dotnet build -c Release
if [ $? -ne 0 ]; then
    echo "Build failed"
    exit 1
fi

# Copy toolkit files (excluding build artifacts, git, etc.)
echo "Copying toolkit files..."
rsync -av --progress . "$TOOLKIT_DIR" \
    --exclude 'bin' \
    --exclude 'obj' \
    --exclude '.git' \
    --exclude '.vs' \
    --exclude 'tools' \
    --exclude 'skyrim-script-headers' \
    --exclude 'scripts' \
    --exclude '*.code-workspace' \
    --exclude '.claude' \
    --exclude 'claude.md' \
    --exclude 'CLAUDE.md' \
    --exclude 'tmpclaude-*'

# Copy .claude skills to release root
echo "Copying Claude Code skills..."
cp -r "$CLAUDE_SKILLS_PATH" "$RELEASE_ROOT/.claude"

# Create README for release structure
cat > "$RELEASE_ROOT/README.txt" << EOF
# Spooky's AutoMod Toolkit v$VERSION

## Directory Structure

This release contains:
- **.claude/** - Claude Code skills for AI-assisted modding
- **spookys-automod-toolkit/** - The toolkit source code

## For Claude Code Users

Place this entire folder in your projects directory. Claude Code will automatically
detect the .claude/skills/ and enable AI-assisted Skyrim modding commands.

## Quick Start

See spookys-automod-toolkit/README.md for installation and usage instructions.
EOF

# Create zip
echo "Creating release archive..."
mkdir -p "$OUTPUT_DIR"
OUTPUT_PATH="$OUTPUT_DIR/spookys-automod-toolkit-v$VERSION.zip"

cd "$TEMP_DIR"
zip -r "$OUTPUT_PATH" "spookys-automod-toolkit-v$VERSION"

# Cleanup
echo "Cleaning up..."
rm -rf "$TEMP_DIR"

echo ""
echo "Release package created:"
echo "  $OUTPUT_PATH"
echo ""
echo "Release structure:"
echo "  spookys-automod-toolkit-v$VERSION/"
echo "  ├── .claude/"
echo "  │   └── skills/"
echo "  └── spookys-automod-toolkit/"
echo "      ├── src/"
echo "      ├── docs/"
echo "      └── README.md"
