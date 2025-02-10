# Development Guide

## Quick Reference

### Setup
```bash
git clone [repository-url]
cd SroutSight
dotnet build
```

### Feature Development
```bash
# Create and switch to feature branch
git checkout -b feature/my-feature

# Make changes and commit
git add .
git commit -m "Description of changes"

# Push changes
git push origin feature/my-feature

# When done, merge to main
git checkout main
git merge feature/my-feature
git push origin main
```

### Release Process

1. Update version numbers:
   ```bash
   # Edit manifest.json to bump version
   # Update CHANGELOG.md with new version
   git add manifest.json CHANGELOG.md
   git commit -m "Prepare release v1.0.0"
   ```

2. Create and push tag:
   ```bash
   git tag v1.0.0
   git push origin main v1.0.0
   ```

3. GitHub Actions will automatically:
   - Build the project
   - Create GitHub release
   - Generate release notes
   - Create mod zip file
   - Upload to Nexus Mods (if configured)

### Release Configuration

#### GitHub Secrets Required
- `NEXUS_API_KEY`: Get from https://www.nexusmods.com/users/myaccount?tab=api
- `NEXUS_MOD_ID`: The number from your mod's Nexus URL

#### Manual Release (if needed)
```bash
# Build
dotnet build --configuration Release

# Create zip with:
# - SproutSight.dll
# - manifest.json
# - Other required files
```

Upload manually to:
- GitHub Releases
- Nexus Mods
