# Development Guide

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
```

### Release Process

#### Release Candidate ####
1. Edit manifest.json to bump version
2. Update CHANGELOG.md with new version
3. git add manifest.json CHANGELOG.md && git commit -m "Prepare release v1.0.0"
4. dotnet build --configuration Release
5. git tag <new version> 
6. git push origin <new version>
7. When accepted
      git checkout main
      git merge feature/my-feature
      git push origin main

8. Upload to 
- GitHub Releases
- Nexus Mods
