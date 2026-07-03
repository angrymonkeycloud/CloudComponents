# CloudComponents GitHub Pages Deployment

Complete setup for professional, multi-demo GitHub Pages deployment with proper subdirectory routing.

## Quick Start

### Deployment URLs
Once deployed, your demos will be available at:

- **Portal**: `https://angrymonkeycloud.github.io/CloudComponents/`
- **Grid Demo**: `https://angrymonkeycloud.github.io/CloudComponents/CloudGrid/`
- **Video Player Demo**: `https://angrymonkeycloud.github.io/CloudComponents/CloudVideo/`
- **Maps Demo**: `https://angrymonkeycloud.github.io/CloudComponents/CloudMap/`
- **Icons Demo**: `https://angrymonkeycloud.github.io/CloudComponents/Icons/`

### To Deploy

Simply push to `main` branch:
```bash
git add .
git commit -m "Update demos"
git push origin main
```

The `deploy-all-demos.yml` workflow will automatically:
1. Build all four demo projects in parallel
2. Prepare assets for GitHub Pages routing
3. Merge artifacts into subdirectories
4. Deploy to GitHub Pages

Monitor progress in **GitHub Actions** → **Workflows** → **Deploy All Demos**

---

## Architecture

### Workflows

#### 1. `deploy-all-demos.yml` (Primary)
**Recommended** - Orchestrates all demos with one deployment

- **Triggers**: Any push to `main` affecting any demo
- **Output**: Single consolidated GitHub Pages deployment
- **Benefits**: 
  - Parallel builds for speed
  - Single Pages environment (no conflicts)
  - Automatic root portal generation

**Workflow Steps:**
```
Push to main
	↓
[Parallel] Build Grid, Video, Maps, Icons
	↓
Merge artifacts into subdirectories
	↓
Create root index.html portal
	↓
Deploy to GitHub Pages
```

#### 2. Individual Workflows (Alternative)

For independent deployments:
- `deploy-grid-demo.yml` - Grid demo only
- `deploy-video-demo.yml` - Video Player demo only
- `deploy-maps-demo.yml` - Maps demo only
- `deploy-icons-demo.yml` - Icons demo only

**Note**: These will overwrite each other. Use `deploy-all-demos.yml` instead.

---

## Projects

### CloudComponents.Grid.Demo
**Base Href**: `/CloudGrid/`

Professional data grid showcase with:
- Sorting and filtering
- Pagination modes
- Row selection
- Reordering capabilities
- State management and theming

### CloudComponents.VideoPlayer.Demo
**Base Href**: `/CloudVideo/`

Advanced video playback demonstration featuring:
- MP4 playback with full controls
- HLS live stream support
- Fullscreen mode
- Volume and progress controls
- Metadata and closed captions

### CloudComponents.Maps.Demo
**Base Href**: `/CloudMap/`

Azure Maps integration showcase including:
- Basic marker placement
- Search and reverse geocoding
- Live tracking
- Location locking
- History visualization
- Map styling and controls

### CloudComponents.Icons.Demo
**Base Href**: `/Icons/` *(New)*

Professional icon and logo gallery with:
- 21 functional UI icons
- 8 brand/technology logos
- Interactive size control (16px–128px)
- Dynamic color picker
- Responsive grid layout
- Copy-to-clipboard feedback

---

## GitHub Pages Configuration

### Required Settings

1. **Repository Settings** → **Pages**
   - Source: `Deploy from a branch`
   - Branch: `gh-pages`
   - Folder: `/ (root)`

2. **Actions Permissions**
   - Allow GitHub Actions
   - Read and write permissions

### How It Works

Each workflow:
1. **Publishes** demo in Release mode
2. **Prepares HTML files** with routing scripts (404.html pattern)
3. **Updates base href** for subdirectory deployment
4. **Uploads artifact** to GitHub Pages

The `deploy-all-demos.yml` additionally:
- Merges all artifacts into proper subdirectories
- Creates root `index.html` navigation portal
- Handles SPA routing with 404.html files

---

## Local Development

### Run Grid Demo
```bash
cd CloudComponents.Grid.Demo
dotnet run
# http://localhost:5000
```

### Run VideoPlayer Demo
```bash
cd CloudComponents.VideoPlayer.Demo
dotnet run
# http://localhost:5001
```

### Run Maps Demo
```bash
cd CloudComponents.Maps.Demo
dotnet run
# http://localhost:5002
```

### Run Icons Demo
```bash
cd CloudComponents.Icons.Demo
dotnet run
# http://localhost:5003
```

### Build All
```bash
dotnet build CloudComponents.sln
```

### Publish Individual Demo
```bash
dotnet publish CloudComponents.Grid.Demo/CloudComponents.Grid.Demo.csproj -c Release -o ./publish-grid
```

---

## Project Structure

```
CloudComponents/
├── .github/
│   ├── copilot-instructions.md
│   ├── upgrades/
│   └── workflows/
│       ├── deploy-all-demos.yml         ← Use this
│       ├── deploy-grid-demo.yml         (legacy)
│       ├── deploy-video-demo.yml
│       ├── deploy-maps-demo.yml
│       └── deploy-icons-demo.yml
│
├── CloudComponents.Grid/
├── CloudComponents.Grid.Demo/
│   └── wwwroot/
│       ├── index.html                   (base href: /CloudGrid/)
│       └── css/app.css
│
├── CloudComponents.VideoPlayer/
├── CloudComponents.VideoPlayer.Demo/
│   └── wwwroot/
│       ├── index.html                   (base href: /CloudVideo/)
│       └── css/app.css
│
├── CloudComponents.Maps/
├── CloudComponents.Maps.Demo/
│   └── wwwroot/
│       ├── index.html                   (base href: /CloudMap/)
│       └── css/app.css
│
├── CloudComponents.Icons/
├── CloudComponents.Icons.Demo/          ← NEW
│   ├── Pages/
│   │   ├── Index.razor                  (Overview)
│   │   ├── IconsPage.razor              (Functional icons)
│   │   └── LogosPage.razor              (Brand logos)
│   ├── Shared/
│   │   ├── MainLayout.razor
│   │   └── NavMenu.razor
│   └── wwwroot/
│       ├── index.html                   (base href: /Icons/)
│       ├── app.js                       (CSS variable utils)
│       └── css/app.css
│
└── CloudComponents.sln                  (includes all projects)
```

---

## Base Href Configuration

Each demo's `wwwroot/index.html` includes proper base href for GitHub Pages:

```html
<!-- Grid Demo -->
<base href="/CloudGrid/" />

<!-- Video Player Demo -->
<base href="/CloudVideo/" />

<!-- Maps Demo -->
<base href="/CloudMap/" />

<!-- Icons Demo -->
<base href="/Icons/" />
```

**Local development**: Set base href to `/` or use port-based routing

---

## Professional Features

✓ **No Emojis**: Professional icons and dismiss buttons (✕ symbol)  
✓ **Dark Theme**: Consistent, modern appearance  
✓ **Responsive Design**: Mobile-friendly layouts  
✓ **Color Scheme**: Sophisticated blue accent (#4c9aff)  
✓ **Typography**: Segoe UI with system font fallbacks  
✓ **Interactive Controls**: Sliders, pickers, hover states  
✓ **Accessibility**: Semantic HTML, proper contrast  
✓ **Performance**: Parallel builds, optimized assets  

---

## Troubleshooting

### Demos not loading
- Check GitHub Actions workflow logs
- Verify base href matches subdirectory
- Clear browser cache (Ctrl+Shift+Delete)

### 404 errors
- Ensure GitHub Pages is enabled
- Check `gh-pages` branch exists and has content
- Verify workflow completed successfully

### Styling issues
- Verify CSS files are deployed
- Check paths in `index.html` link tags
- Ensure `.styles.css` files are included

### Build failures
- Run `dotnet build` locally first
- Check .NET 10 SDK installed
- Review workflow logs for specific errors

---

## Adding New Demos

1. Create new `CloudComponents.{Feature}.Demo` project
2. Add to `CloudComponents.sln`: `dotnet sln add CloudComponents.{Feature}.Demo/CloudComponents.{Feature}.Demo.csproj`
3. Update base href to `/Cloud{Feature}/`
4. Update `.github/workflows/deploy-all-demos.yml`:
   - Add build job
   - Add merge step for new subdirectory
   - Add navigation card to root index.html
5. Commit and push to trigger deployment

---

## Performance

**Deployment Time**: ~3-5 minutes total

- **Parallel builds**: ~2-3 min (all 4 demos simultaneously)
- **Merge & prepare**: ~30 sec
- **Upload & deploy**: ~1 min

**Bundle Sizes** (approximate):
- Grid Demo: ~2 MB
- Video Demo: ~3 MB
- Maps Demo: ~4 MB
- Icons Demo: ~1.5 MB

**Total**: ~10-11 MB

---

## Maintenance

### Update dependencies
```bash
dotnet package search
dotnet package upgrade
```

### Add new icons
Edit `CloudComponents.Icons.Demo/Pages/IconsPage.razor` and `LogosPage.razor`

### Modify branding
Update root `index.html` styles in `deploy-all-demos.yml`

### Change subdirectory paths
Update all `base href` values and workflow artifact merging

---

## Support

For issues:
1. Check GitHub Actions workflow logs
2. Review this documentation
3. Verify local build works: `dotnet build`
4. Check GitHub Pages repository settings
5. Open GitHub issue with workflow logs

---

## Files Created/Modified

### New Files
- `CloudComponents.Icons.Demo/` (complete project)
- `.github/workflows/deploy-video-demo.yml`
- `.github/workflows/deploy-maps-demo.yml`
- `.github/workflows/deploy-icons-demo.yml`
- `.github/workflows/deploy-all-demos.yml`

### Modified Files
- `CloudComponents.Grid.Demo/wwwroot/index.html` - Updated base href
- `CloudComponents.VideoPlayer.Demo/wwwroot/index.html` - Updated base href
- `CloudComponents.Maps.Demo/wwwroot/index.html` - Updated base href
- `CloudComponents.sln` - Added Icons.Demo project

---

*Last updated: 2025*  
*For latest information, see workflow logs in GitHub Actions*
