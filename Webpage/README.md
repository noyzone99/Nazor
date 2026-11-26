# Nazor website

The Nazor showcase and documentation site: a Blazor WebAssembly app styled with the NazorLeaf visual language.

| Project | Purpose |
| --- | --- |
| `Nazor.Website` | The Blazor WebAssembly site: home page, getting started, documentation shell, search |
| `Nazor.Website.Generator` | Converts `Docs/` from Markdown into HTML fragments and a navigation manifest |
| `Nazor.Website.Prerender` | Renders the Razor pages to HTML and writes the sitemap and robots.txt during publish |
| `Shared` | The manifest model and the syntax highlighter, compiled into both projects |

The site never ships Markdown. Before each build, the generator renders `Docs/` into `Nazor.Website/wwwroot/content/`:

- `pages/**.html` holds one HTML fragment per document, with highlighted code, framed tables, heading anchors, and links rewritten to site routes or to the GitHub repository.
- `manifest.json` lists every page with its route, area, navigation group, summary, previous and next pages, and table of contents. The client search uses it.
- `assets/`, `media/` and `fonts/` hold the identity artwork, referenced images, and the NazorLeaf fonts with their licenses.

The generated folder is not tracked. Page routes, guide groups and API package groups are defined in `Nazor.Website.Generator/SiteLayout.cs`.

## Run locally

```powershell
dotnet run --project Webpage/Nazor.Website/Nazor.Website.csproj
```

The build regenerates the content when `Docs/`, the generator, or the NazorLeaf fonts change. Add `-p:SkipSiteContent=true` to reuse existing content. To regenerate the content without building the site:

```powershell
dotnet run --project Webpage/Nazor.Website.Generator -- . Webpage/Nazor.Website/wwwroot/content
```

## Build and deploy

The [Website workflow](../.github/workflows/website.yml) builds and verifies the site on relevant pushes to `main` and `dev`, and can also run on demand. It checks that each route has rendered content, its own metadata and a canonical URL listed in the sitemap.

Every published route has an `index.html` with its content, title, description, canonical URL and social preview. The content is readable and the links work without JavaScript. Blazor loads in the background and enables features such as search and theme controls. Routes use a trailing slash and unknown URLs use the custom `404.html`.

Pushes to `main` deploy the verified output to GitHub Pages. The workflow passes the configured Pages URL as `SitePublicUrl`, which sets both the HTML base path and public metadata. Set the Pages source to **GitHub Actions** in the repository settings.

Download the static site from the `website` artifact in `dev` and manually triggered runs, or build and verify it locally:

```powershell
dotnet publish Webpage/Nazor.Website/Nazor.Website.csproj -c Release -o publish
python Webpage/verify-publish.py publish/wwwroot
python -m http.server --directory publish/wwwroot 8000
```

GitHub Pages hosts this project under `/Nazor/`. Its `sitemap.xml` covers the project URLs. The project artifact includes a `robots.txt` pointing to the sitemap. Crawlers read `robots.txt` from the host root, which a project deployment under `/Nazor/` cannot configure.

## Structure

| Route | Content |
| --- | --- |
| `/` | Showcase: what Nazor is, the Gallery carousel, features, NazorLeaf, platforms and the documentation hub |
| `/getting-started/` | Quick start with the Visual Studio extension or the .NET CLI |
| `/guide/`, `/guide/*/`, `/devtools/`, `/devtools/protocol/` | Developer guide, including the DevTools pages |
| `/api/`, `/api/*/` | API reference by package |

Existing links to `/docs/` redirect to `/guide/`.

The Gallery carousel uses WebP captures in `Nazor.Website/wwwroot/media/gallery/`. Its markup and appearance live in `Nazor.Website/Components/GalleryCarousel.razor` and its scoped stylesheet. The browser module loads the first desktop as it enters view, then preloads the next desktop during autoplay. When a selected preview is still loading, the carousel shows its status and keeps the current desktop visible.
