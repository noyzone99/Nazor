import json
import sys
import xml.etree.ElementTree as ET
from html.parser import HTMLParser
from pathlib import Path
from urllib.parse import urlsplit


class Page(HTMLParser):
    def __init__(self, path):
        super().__init__()
        self.metadata = {}
        self.headings = 0
        self.in_title = False
        self.title = ""
        self.feed(path.read_text(encoding="utf-8"))

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if tag == "title":
            self.in_title = True
        elif tag == "h1":
            self.headings += 1
        elif tag == "meta" and (key := attrs.get("name", attrs.get("property"))):
            self.metadata.setdefault(key, []).append(attrs.get("content"))
        elif tag == "base" or tag == "link" and attrs.get("rel") == "canonical":
            self.metadata.setdefault(tag, []).append(attrs.get("href"))

    def handle_endtag(self, tag):
        if tag == "title":
            self.in_title = False

    def handle_data(self, data):
        if self.in_title:
            self.title += data


site = Path(sys.argv[1])
deployed_url = sys.argv[2] if len(sys.argv) > 2 else ""
public_url = (deployed_url or "https://noyzone99.github.io/Nazor/").rstrip("/") + "/"
base_path = urlsplit(public_url).path if deployed_url else "/"
manifest = json.loads((site / "content/manifest.json").read_text(encoding="utf-8"))
version = ET.parse("Directory.Build.props").findtext(".//NazorVersion")
assert version and manifest["version"] == version, "Published documentation version differs from NazorVersion"
pages = manifest["pages"]
sources = {"Docs/Guide/README.md", "Docs/API.md"}
sources.update(path.as_posix() for path in Path("Docs/Guide").glob("[0-9][0-9]-*.md"))
sources.update(path.as_posix() for path in Path("Docs/ApiReferences").glob("*.md"))
missing = sources - {page["source"] for page in pages}
assert not missing, f"Missing documentation sources: {sorted(missing)}"

photo = "content/assets/nazor-github-banner.jpg"
for asset in ["404.html", "docs/index.html", "_framework/blazor.webassembly.js", photo,
              "content/fonts/Geist.ttf", "content/fonts/GeistMono.ttf", "content/fonts/BricolageGrotesque.ttf",
              *(page["content"] for page in pages)]:
    file = site / asset
    assert file.is_file() and file.stat().st_size, f"Missing or empty published asset: {asset}"

routes = ["", "getting-started/", *(page["route"] + "/" for page in pages)]
titles = set()
descriptions = set()
for route in routes:
    page = Page(site / route / "index.html")
    canonical = public_url + route
    assert page.headings >= 1 and page.title and page.title not in titles, f"Missing heading or unique title: {route}"
    titles.add(page.title)
    description, = page.metadata["description"]
    assert description and description not in descriptions, f"Missing or duplicate description: {route}"
    descriptions.add(description)
    for key, value in {
        "base": base_path, "link": canonical, "og:url": canonical, "og:title": page.title,
        "og:description": description, "og:image": public_url + photo, "twitter:card": "summary_large_image",
    }.items():
        assert page.metadata.get(key) == [value], f"Invalid {key} for {route}"

for doc in pages:
    fragment = (site / doc["content"]).read_text(encoding="utf-8").strip()
    document = (site / doc["route"] / "index.html").read_text(encoding="utf-8")
    assert fragment and fragment in document, f"Missing prerendered content for {doc['route']}"

sitemap = ET.parse(site / "sitemap.xml")
urls = [entry.text for entry in sitemap.findall("{*}url/{*}loc")]
assert urls == [public_url + route for route in routes], "Sitemap differs from the published canonical URLs"
assert f"Sitemap: {public_url}sitemap.xml" in (site / "robots.txt").read_text(encoding="utf-8").splitlines()
assert Page(site / "404.html").metadata.get("robots") == ["noindex"]
assert Page(site / "docs/index.html").metadata.get("link") == [public_url + "guide/"]
print(f"Verified {len(routes)} static pages, canonical URLs, social metadata, sitemap and robots.txt in {site}")
