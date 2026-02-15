<#
.SYNOPSIS
    Publish WCAR - GitHub Release and Scoop.
.DESCRIPTION
    Builds a release zip (self-contained single-file exe), creates a GitHub
    release via git tag + push, and publishes to Scoop bucket.
    Requires: git (with SSH), .NET SDK.
.PARAMETER Target
    Where to publish: github, scoop, or all.
.PARAMETER Version
    Override version (e.g., "1.2.0"). If omitted, reads from version.txt.
.PARAMETER BumpType
    Auto-bump before publishing: major, minor, or patch.
.PARAMETER DryRun
    Show what would happen without making changes.
.EXAMPLE
    .\scripts\publish.ps1 -Target all -BumpType patch
    .\scripts\publish.ps1 -Target github -Version 2.0.0
    .\scripts\publish.ps1 -Target all -DryRun
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("github", "scoop", "all")]
    [string]$Target,

    [string]$Version,

    [ValidateSet("major", "minor", "patch")]
    [string]$BumpType,

    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$VersionFile = Join-Path $ProjectRoot "version.txt"
$DistDir = Join-Path $ProjectRoot "dist"
$CsprojPath = Join-Path $ProjectRoot "Wcar\Wcar.csproj"
$Repo = "doubleapp/wcar"

# --- Version helpers ---

function Get-CurrentVersion {
    if (-not (Test-Path $VersionFile)) {
        throw "version.txt not found at $VersionFile"
    }
    return (Get-Content $VersionFile -Raw).Trim()
}

function Step-Version {
    param([string]$Current, [string]$Bump)
    $parts = $Current.Split('.')
    switch ($Bump) {
        "major" { $parts[0] = [int]$parts[0] + 1; $parts[1] = "0"; $parts[2] = "0" }
        "minor" { $parts[1] = [int]$parts[1] + 1; $parts[2] = "0" }
        "patch" { $parts[2] = [int]$parts[2] + 1 }
    }
    return "$($parts[0]).$($parts[1]).$($parts[2])"
}

function Set-VersionFile {
    param([string]$NewVersion)
    "$NewVersion`n" | Set-Content $VersionFile -NoNewline
    Write-Host "  Updated version.txt to $NewVersion" -ForegroundColor Green
}

# --- Build helpers ---

function Build-ReleaseZip {
    param([string]$Ver)

    if (-not (Test-Path $DistDir)) { New-Item -ItemType Directory -Path $DistDir -Force | Out-Null }

    $publishDir = Join-Path $DistDir "publish"
    $zipName = "wcar-$Ver-win-x64.zip"
    $zipPath = Join-Path $DistDir $zipName

    if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

    # Build self-contained single-file exe
    Write-Host "  Building .NET release..." -ForegroundColor White
    $buildOutput = dotnet publish $CsprojPath -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true `
        -p:Version=$Ver `
        -o $publishDir 2>&1
    $buildOutput | Write-Host

    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

    # Zip the exe
    $exePath = Join-Path $publishDir "wcar.exe"
    if (-not (Test-Path $exePath)) { throw "wcar.exe not found at $exePath" }

    Compress-Archive -Path $exePath -DestinationPath $zipPath -Force
    Remove-Item $publishDir -Recurse -Force

    $size = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
    Write-Host "  Built: $zipPath ($size MB)" -ForegroundColor Green
    return $zipPath
}

function Get-FileHash256 {
    param([string]$FilePath)
    return (Get-FileHash -Path $FilePath -Algorithm SHA256).Hash.ToLower()
}

# --- Publish: GitHub ---

function Publish-GitHub {
    param([string]$Ver, [string]$ZipPath)

    $tag = "v$Ver"
    Write-Host "`n[GitHub] Creating release $tag..." -ForegroundColor Cyan

    if ($DryRun) {
        Write-Host "  [DRY RUN] Would create tag $tag and push to origin" -ForegroundColor Yellow
        Write-Host "  [DRY RUN] Would open GitHub release page for upload" -ForegroundColor Yellow
        return
    }

    # Create git tag
    $existingTag = git -C $ProjectRoot tag -l $tag 2>&1
    if ($existingTag) {
        Write-Host "  Tag $tag already exists, skipping tag creation" -ForegroundColor Yellow
    } else {
        git -C $ProjectRoot tag -a $tag -m "Release $tag"
        Write-Host "  Created tag: $tag" -ForegroundColor Green
    }

    # Push tag to origin
    Write-Host "  Pushing tag to origin (you may be prompted for SSH passphrase)..." -ForegroundColor White
    git -C $ProjectRoot push origin $tag
    Write-Host "  Pushed tag: $tag" -ForegroundColor Green

    # Open GitHub release creation page in browser
    $releaseUrl = "https://github.com/$Repo/releases/new?tag=$tag&title=WCAR+$tag"
    Write-Host ""
    Write-Host "  Opening GitHub release page in your browser..." -ForegroundColor Cyan
    Write-Host "  Attach this zip: $ZipPath" -ForegroundColor Yellow
    Start-Process $releaseUrl

    Write-Host ""
    Write-Host "  Release URL: https://github.com/$Repo/releases/tag/$tag" -ForegroundColor Green
}

# --- Publish: Scoop ---

function Publish-Scoop {
    param([string]$Ver, [string]$ZipPath)

    Write-Host "`n[Scoop] Generating manifest..." -ForegroundColor Cyan

    $hash = Get-FileHash256 -FilePath $ZipPath
    $url = "https://github.com/$Repo/releases/download/v$Ver/wcar-$Ver-win-x64.zip"

    $manifest = @{
        version = $Ver
        description = "Windows tray app that saves and restores desktop sessions (window positions, terminal CWDs, Explorer folders, Docker state)"
        homepage = "https://github.com/$Repo"
        license = "MIT"
        architecture = @{
            "64bit" = @{
                url = $url
                hash = $hash
            }
        }
        bin = @("wcar.exe")
        post_install = @(
            "Write-Host 'WCAR installed! Run wcar.exe to start the tray app.' -ForegroundColor Green"
        )
        notes = @(
            "WCAR runs as a system tray icon. Right-click for Save/Restore Session, Scripts, and Settings."
            "Data stored in: `$env:LOCALAPPDATA\WCAR\"
        )
    }

    $manifestPath = Join-Path $DistDir "wcar.json"

    if ($DryRun) {
        Write-Host "  [DRY RUN] Would write manifest to $manifestPath" -ForegroundColor Yellow
        $manifest | ConvertTo-Json -Depth 5 | Write-Host
        return
    }

    $manifest | ConvertTo-Json -Depth 5 | Set-Content $manifestPath -Encoding UTF8
    Write-Host "  Generated: $manifestPath" -ForegroundColor Green
    Write-Host "  Hash: $hash" -ForegroundColor DarkGray

    # Push manifest to scoop bucket repo
    $bucketRepo = "git@github.com:${Repo}-scoop-bucket.git"
    $bucketDir = Join-Path $DistDir "scoop-bucket"

    Write-Host ""
    Write-Host "  Pushing manifest to scoop bucket (you may be prompted for SSH passphrase)..." -ForegroundColor Cyan

    if (Test-Path $bucketDir) {
        git -C $bucketDir pull --rebase origin main
    } else {
        git clone $bucketRepo $bucketDir
    }

    Copy-Item $manifestPath (Join-Path $bucketDir "wcar.json") -Force

    git -C $bucketDir add wcar.json
    git -C $bucketDir diff --cached --quiet 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        git -C $bucketDir commit -m "Update wcar to $Ver"
        git -C $bucketDir push origin main
        Write-Host "  Pushed wcar.json (v$Ver) to scoop bucket" -ForegroundColor Green
    } else {
        Write-Host "  Scoop bucket already up to date" -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "  Users install with:" -ForegroundColor White
    Write-Host "    scoop bucket add wcar https://github.com/${Repo}-scoop-bucket" -ForegroundColor Gray
    Write-Host "    scoop install wcar" -ForegroundColor Gray
}

# --- Main ---

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " WCAR - Publish" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

# Resolve version
$currentVersion = Get-CurrentVersion
if ($BumpType) {
    $resolvedVersion = Step-Version -Current $currentVersion -Bump $BumpType
    Write-Host "`nVersion: $currentVersion -> $resolvedVersion ($BumpType bump)" -ForegroundColor White
    if (-not $DryRun) { Set-VersionFile -NewVersion $resolvedVersion }
} elseif ($Version) {
    $resolvedVersion = $Version
    Write-Host "`nVersion: $resolvedVersion (manual override)" -ForegroundColor White
    if (-not $DryRun) { Set-VersionFile -NewVersion $resolvedVersion }
} else {
    $resolvedVersion = $currentVersion
    Write-Host "`nVersion: $resolvedVersion (from version.txt)" -ForegroundColor White
}

# Pre-publish: run tests
Write-Host "`n[Tests] Running unit tests..." -ForegroundColor Cyan
dotnet test (Join-Path $ProjectRoot "Wcar.Tests\Wcar.Tests.csproj") --no-restore -q
if ($LASTEXITCODE -ne 0) { throw "Tests failed — aborting publish" }
Write-Host "  All tests passed" -ForegroundColor Green

# Build zip
Write-Host "`n[Build] Packaging release zip..." -ForegroundColor Cyan
$zipPath = Build-ReleaseZip -Ver $resolvedVersion

# Publish to selected targets
$targets = if ($Target -eq "all") { @("github", "scoop") } else { @($Target) }

foreach ($t in $targets) {
    switch ($t) {
        "github" { Publish-GitHub -Ver $resolvedVersion -ZipPath $zipPath }
        "scoop"  { Publish-Scoop  -Ver $resolvedVersion -ZipPath $zipPath }
    }
}

Write-Host "`n============================================================" -ForegroundColor Cyan
Write-Host " Done!" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Cyan
