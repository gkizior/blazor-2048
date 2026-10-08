#!/usr/bin/env python3
"""Prepare a published Blazor wwwroot folder for GitHub Pages.

- Rewrites <base href="/"> to the repo sub-path (e.g. /blazor-2048/).
- Updates index.html's hash in service-worker-assets.js so the PWA cache still installs.
- Copies index.html to 404.html (SPA fallback) and adds .nojekyll.

Usage: prepare-pages.py <wwwroot-dir> <base-path>
"""
import base64, hashlib, json, os, re, shutil, sys

root, base = sys.argv[1], sys.argv[2]
if not base.startswith("/"): base = "/" + base
if not base.endswith("/"): base += "/"

index = os.path.join(root, "index.html")
html = open(index, encoding="utf-8").read()
html, n = re.subn(r'<base href="[^"]*"\s*/?>', f'<base href="{base}" />', html)
if n != 1:
    sys.exit("Could not find <base href> in index.html")
open(index, "w", encoding="utf-8", newline="").write(html)

# Remove stale precompressed copies of the files we modify (Pages doesn't serve them anyway).
for name in ("index.html", "service-worker-assets.js"):
    for ext in (".br", ".gz"):
        p = os.path.join(root, name + ext)
        if os.path.exists(p): os.remove(p)

digest = "sha256-" + base64.b64encode(hashlib.sha256(open(index, "rb").read()).digest()).decode()
sw_assets = os.path.join(root, "service-worker-assets.js")
if os.path.exists(sw_assets):
    text = open(sw_assets, encoding="utf-8").read()
    prefix = "self.assetsManifest = "
    data = json.loads(text[text.index(prefix) + len(prefix):].rstrip().rstrip(";"))
    for asset in data["assets"]:
        if asset["url"] == "index.html":
            asset["hash"] = digest
    open(sw_assets, "w", encoding="utf-8").write(prefix + json.dumps(data, indent=2) + ";\n")

shutil.copyfile(index, os.path.join(root, "404.html"))
open(os.path.join(root, ".nojekyll"), "w").close()
print(f"Prepared {root} for GitHub Pages at {base}")
