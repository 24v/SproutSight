# Development Guide

### Setup
```bash
git clone [repository-url]
cd SproutSight
dotnet build
```

### Release Process
1. Start from a feature or release branch
1. Edit manifest.json to bump version
2. Update CHANGELOG.md with new version
3. Build:
      ```bash
      dotnet build --configuration Release
      ```
4. Copy zip in bin into releases folder
5. compile bbcode
      ```bash
      python3 convert_to_bbcode.py README.md docs/bbcode/README_BBCODE.txt "https://raw.githubusercontent.com/24v/SproutSight/main"
      ```
6. test zip
7. test bbcode
5. commit "releasing v-x"
7. Merge to main
      ```bash
      git checkout main
      git rebase || merge
      git merge <branch>
      git push origin main
      git tag <new version> 
      git push origin <new version>
      ```
8. Upload to github releases
9. Upload to Nexus mods
