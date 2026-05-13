# Skill: Publish and Relaunch WCAR via GitHub API

## When to Use
When publishing a new version of WCAR to GitHub Releases and Scoop bucket, and relaunching locally.

## Prerequisites
- `gh` CLI authenticated (`gh auth status`)
- All tests passing (`dotnet test Wcar.Tests/Wcar.Tests.csproj`)
- Changes committed and pushed to `origin/main`

## Steps

### 1. Stop Running Instance
```powershell
Stop-Process -Name wcar -ErrorAction SilentlyContinue
```

### 2. Bump Version
Update `version.txt` (e.g., `1.0.1` → `1.0.2`). Commit and push.

**Note:** `git push origin main` hangs on SSH passphrase in Git Bash. Use `gh` API to push if needed, or ensure SSH agent is running.

### 3. Clean Build
```bash
rm -rf Wcar/obj Wcar/bin
dotnet publish Wcar/Wcar.csproj -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:Version=$VERSION -o dist/publish
```
Stale `obj/` caches cause `MSB3492` errors — always `rm -rf Wcar/obj Wcar/bin` before release builds.

### 4. Create Release Zip
```powershell
Compress-Archive -Path 'dist\publish\wcar.exe' -DestinationPath "dist\wcar-$VERSION-win-x64.zip" -Force
```

### 5. Tag and Push via GitHub API
SSH push hangs in Git Bash. Use `gh api` instead:
```bash
# Create local tag
git tag -a v$VERSION -m "Release v$VERSION"

# Push tag via API (avoids SSH passphrase prompt)
gh api repos/doubleapp/wcar/git/refs \
  -f ref="refs/tags/v$VERSION" \
  -f sha="$(git rev-parse v$VERSION^{commit})"
```

### 6. Create GitHub Release
```bash
gh release create v$VERSION dist/wcar-$VERSION-win-x64.zip \
  --repo doubleapp/wcar \
  --title "WCAR v$VERSION" \
  --notes "Release notes here"
```

### 7. Update Scoop Bucket
Get the SHA256 hash:
```powershell
(Get-FileHash -Path "dist\wcar-$VERSION-win-x64.zip" -Algorithm SHA256).Hash.ToLower()
```

Get current file SHA from GitHub:
```bash
gh api repos/doubleapp/wcar-scoop-bucket/contents/wcar.json --jq '.sha'
```

Update manifest via Contents API (avoids SSH clone/push):
```bash
gh api repos/doubleapp/wcar-scoop-bucket/contents/wcar.json -X PUT \
  -f message="Update wcar to $VERSION" \
  -f content="$(base64 -w 0 dist/scoop-bucket/wcar.json)" \
  -f sha="$FILE_SHA"
```

### 8. Relaunch Locally
```powershell
Start-Process 'E:\EProjects\wcar\dist\publish\wcar.exe'
```

## Key Gotchas
- **SSH hangs**: All git push operations through Git Bash hang on SSH passphrase. Always use `gh api` for remote operations.
- **Stale obj/ cache**: Always delete `Wcar/obj` and `Wcar/bin` before release builds to avoid MSB3492 errors.
- **dotnet publish output leak**: In PowerShell functions, capture dotnet output with `$out = dotnet publish ... 2>&1; $out | Write-Host` to prevent it from corrupting return values.
- **File lock**: Stop running `wcar.exe` before building, otherwise the publish output dir can't be written.

## Publish Script (Alternative)
`scripts/publish.ps1` automates all of the above but relies on SSH for git push. Use it only when SSH agent is loaded:
```powershell
.\scripts\publish.ps1 -Target all -BumpType patch
```
