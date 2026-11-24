# Nazor website

The Nazor showcase and documentation site: a Blazor WebAssembly app styled with the NazorLeaf visual language.

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

## Build artifact

The [Website workflow](../.github/workflows/website.yml) builds and verifies the site on relevant pushes to `main` and `dev`, and can also run on demand. It checks that the output contains the guide, the API reference, and their generated HTML fragments. Download the static site from the run's `website` artifact.

## Structure

| Route | Content |
| --- | --- |
| `/` | Showcase: what Nazor is, the Gallery carousel, features, NazorLeaf, platforms and the documentation hub |
| `/getting-started` | Quick start with the Visual Studio extension or the .NET CLI |
| `/docs`, `/guide/*` | Documentation overview and developer guide |
| `/api`, `/api/*` | API reference by package |
| `/devtools`, `/nuget`, `/conventions`, `/brand` | Tools and project pages |

The Gallery carousel uses the captures in `Nazor.Website/wwwroot/media/gallery/`. Its markup and appearance live in `Nazor.Website/Components/GalleryCarousel.razor` and its scoped stylesheet. The browser module in `Nazor.Website/wwwroot/js/gallery-carousel.js` handles playback, themes and local clocks.
