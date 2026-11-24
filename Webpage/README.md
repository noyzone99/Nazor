# Nazor website

The Nazor showcase and documentation site: a Blazor WebAssembly app styled with the NazorLeaf visual language and published to GitHub Pages.

| Project | Purpose |
| --- | --- |
| `Nazor.Website` | The Blazor WebAssembly site: home page, getting started, documentation shell, search |
| `Nazor.Website.Generator` | Converts `Docs/` from Markdown into HTML fragments and a navigation manifest |
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

## Publish

`.github/workflows/website.yml` publishes the site on pushes to `main` that touch `Docs/` or `Webpage/`. It sets the `<base>` address to `/<repository>/` for a GitHub project site. `404.html` keeps deep links working by redirecting unknown paths to the app, which restores the requested route.

Select **GitHub Actions** as the Pages source in the repository settings before the first deployment.

## Structure

| Route | Content |
| --- | --- |
| `/` | Showcase: what Nazor is, screenshots, features, NazorLeaf, platforms and the documentation hub |
| `/getting-started` | Quick start with the Visual Studio extension or the .NET CLI |
| `/docs`, `/guide/*` | Documentation overview and developer guide |
| `/api`, `/api/*` | API reference by package |
| `/devtools`, `/nuget`, `/conventions`, `/brand` | Tools and project pages |

Add showcase screenshots to `Docs/Assets/` and list them in `Shots` in `Nazor.Website/Pages/Home.razor`.
