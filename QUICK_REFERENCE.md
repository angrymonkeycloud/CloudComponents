# Quick Reference Guide

## Deploy to GitHub Pages

```bash
# 1. Review changes
git status

# 2. Stage all changes
git add .

# 3. Commit
git commit -m "Deploy demos to GitHub Pages"

# 4. Push
git push origin main

# 5. Monitor in GitHub Actions
# → Go to your repo → Actions tab → Deploy All Demos workflow
```

## Access Your Demos

| Demo | URL |
|------|-----|
| Portal Home | `https://angrymonkeycloud.github.io/CloudComponents/` |
| Grid | `https://angrymonkeycloud.github.io/CloudComponents/CloudGrid/` |
| Video Player | `https://angrymonkeycloud.github.io/CloudComponents/CloudVideo/` |
| Maps | `https://angrymonkeycloud.github.io/CloudComponents/CloudMap/` |
| Icons | `https://angrymonkeycloud.github.io/CloudComponents/Icons/` |

## Local Development

```bash
# Navigate to demo
cd CloudComponents.Icons.Demo

# Run locally
dotnet run

# Access at http://localhost:5003 (or configured port)
```

## Build & Test

```bash
# Build all projects
dotnet build CloudComponents.sln

# Build specific project
dotnet build CloudComponents.Icons.Demo/CloudComponents.Icons.Demo.csproj

# Publish for deployment
dotnet publish CloudComponents.Icons.Demo/CloudComponents.Icons.Demo.csproj -c Release
```

## Project Files

| File | Purpose |
|------|---------|
| `.github/workflows/deploy-all-demos.yml` | Main deployment workflow |
| `CloudComponents.Icons.Demo/` | New icons showcase project |
| `IMPLEMENTATION_SUMMARY.md` | Full implementation details |
| `GITHUB_PAGES_DEPLOYMENT.md` | Deployment documentation |

## Key Features

### Icons Demo
- **21 functional icons** (PlayButton, SearchIcon, etc.)
- **8 brand logos** (Google, Microsoft, Apple, etc.)
- **Interactive controls**: Size slider & color picker
- **Professional design**: Dark theme, no emojis
- **Responsive**: Works on desktop and mobile

### Deployment
- **Automatic**: Push to `main` triggers deployment
- **Fast**: Parallel builds (~3-5 minutes)
- **Professional**: Root portal with navigation
- **Reliable**: No deployment conflicts

## Files Created

**New Project**: `CloudComponents.Icons.Demo/` (11 files)  
**Workflows**: 4 GitHub Actions files  
**Documentation**: 3 markdown files  

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Build fails | Run `dotnet build` locally, check .NET 10 SDK |
| Demos not loading | Clear cache, check GitHub Actions logs |
| Wrong paths | Verify base href in `wwwroot/index.html` |
| Styling broken | Check CSS files in artifact, refresh |

## Important Notes

✅ All projects compile successfully  
✅ Icons.Demo added to CloudComponents.sln  
✅ Professional design (no emojis/childish elements)  
✅ Ready for immediate deployment  

## Support

1. Check GitHub Actions workflow logs
2. Review `GITHUB_PAGES_DEPLOYMENT.md`
3. Verify local build: `dotnet build`
4. Check base href configuration

---

**Next Step**: Run `git push origin main` to deploy! 🚀
