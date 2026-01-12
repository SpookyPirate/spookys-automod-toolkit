# Build Release Package for Spooky's AutoMod Toolkit
# Creates a zip with structure:
#   spookys-automod-toolkit-v1.x.x/
#   ├── .claude/
#   └── spookys-automod-toolkit/

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,

    [Parameter(Mandatory=$false)]
    [string]$ClaudeSkillsPath = "..\toolkit-release-assets\.claude",

    [Parameter(Mandatory=$false)]
    [string]$OutputDir = "..\releases"
)

$ErrorActionPreference = "Stop"

Write-Host "Building Spooky's AutoMod Toolkit v$Version" -ForegroundColor Cyan

# Validate inputs
if (-not (Test-Path $ClaudeSkillsPath)) {
    Write-Error ".claude skills folder not found at: $ClaudeSkillsPath"
    exit 1
}

# Create temp directory
$tempDir = Join-Path $env:TEMP "spookys-automod-toolkit-release"
$releaseRoot = Join-Path $tempDir "spookys-automod-toolkit-v$Version"
$toolkitDir = Join-Path $releaseRoot "spookys-automod-toolkit"

Write-Host "Creating temporary directory..." -ForegroundColor Yellow
if (Test-Path $tempDir) {
    Remove-Item $tempDir -Recurse -Force
}
New-Item -ItemType Directory -Path $releaseRoot | Out-Null
New-Item -ItemType Directory -Path $toolkitDir | Out-Null

# Build the toolkit
Write-Host "Building toolkit..." -ForegroundColor Yellow
dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit 1
}

# Copy toolkit files
Write-Host "Copying toolkit files..." -ForegroundColor Yellow
$excludePatterns = @(
    "bin",
    "obj",
    ".git",
    ".vs",
    "tools",
    "skyrim-script-headers",
    "scripts",
    "*.code-workspace",
    ".claude",
    "claude.md",
    "CLAUDE.md",
    "tmpclaude-*"
)

Get-ChildItem -Path . | Where-Object {
    $item = $_
    -not ($excludePatterns | Where-Object { $item.Name -like $_ })
} | ForEach-Object {
    Copy-Item $_.FullName -Destination $toolkitDir -Recurse -Force
}

# Copy .claude skills to release root
Write-Host "Copying Claude Code skills..." -ForegroundColor Yellow
Copy-Item $ClaudeSkillsPath -Destination (Join-Path $releaseRoot ".claude") -Recurse -Force

# Create README for release structure
$releaseReadme = @"
# Spooky's AutoMod Toolkit v$Version

## Directory Structure

This release contains:
- **.claude/** - Claude Code skills for AI-assisted modding
- **spookys-automod-toolkit/** - The toolkit source code

## For Claude Code Users

Place this entire folder in your projects directory. Claude Code will automatically
detect the .claude/skills/ and enable AI-assisted Skyrim modding commands.

## Quick Start

See spookys-automod-toolkit/README.md for installation and usage instructions.
"@

Set-Content -Path (Join-Path $releaseRoot "README.txt") -Value $releaseReadme

# Create zip
Write-Host "Creating release archive..." -ForegroundColor Yellow
$outputPath = Join-Path $OutputDir "spookys-automod-toolkit-v$Version.zip"
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

if (Test-Path $outputPath) {
    Remove-Item $outputPath -Force
}

Compress-Archive -Path $releaseRoot -DestinationPath $outputPath -CompressionLevel Optimal

# Cleanup
Write-Host "Cleaning up..." -ForegroundColor Yellow
Remove-Item $tempDir -Recurse -Force

Write-Host "`nRelease package created:" -ForegroundColor Green
Write-Host "  $outputPath" -ForegroundColor White
Write-Host "`nRelease structure:" -ForegroundColor Green
Write-Host "  spookys-automod-toolkit-v$Version/" -ForegroundColor White
Write-Host "  ├── .claude/" -ForegroundColor White
Write-Host "  │   └── skills/" -ForegroundColor White
Write-Host "  └── spookys-automod-toolkit/" -ForegroundColor White
Write-Host "      ├── src/" -ForegroundColor White
Write-Host "      ├── docs/" -ForegroundColor White
Write-Host "      └── README.md" -ForegroundColor White
