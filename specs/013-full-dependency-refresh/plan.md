# Implementation Plan: Full dependency refresh and tracked diagram tooling

**Feature**: `013-full-dependency-refresh` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

## Summary

`tools/mermaid/package.json` pins `@mermaid-js/mermaid-cli` and `puppeteer` exactly; the committed
`package-lock.json` pins everything below them (including `mermaid`). `MermaidRenderer` reads the
locked versions of mermaid-cli and mermaid into its settings fingerprint, so any version change
re-renders every diagram. When it has work to do it runs `npm ci` in `tools/mermaid` (once) and
calls `node_modules/.bin/mmdc` instead of `npx --yes -p …`. Dependabot gets an `npm` entry for
`/tools/mermaid`. Actions are pinned to commit SHAs (`uses: owner/action@<sha> # vX.Y.Z`), which
Dependabot keeps updated, comment included.

| Item | Before | After | License |
|---|---|---|---|
| Markdig | 1.5.1 | 1.6.0 | BSD-2-Clause |
| @mermaid-js/mermaid-cli | 12.0.0 | 12.2.0 | MIT |
| puppeteer | 25.12.0 | 25.16.0 | Apache-2.0 |
| mermaid (locked, transitive) | 12.3.0 (floating) | 12.3.0 | MIT |
| Actions | major tags | commit SHAs of latest releases | MIT |

License review: no license changed and no new transitive packages. Existing non-MIT build-time-only
transitives (fonts OFL-1.1, Font Awesome CC-BY-4.0/OFL-1.1/MIT, dompurify MPL-2.0 OR Apache-2.0,
elkjs EPL-2.0 OR GPL-3.0-or-later) are used only while rendering and nothing from them ships in the app.

## Constitution Check

| Principle | Status | Notes |
|---|---|---|
| I. Blazor/C# first | ✅ | Node is build-time only; no app JavaScript. |
| II. .NET 10 | ✅ | SDK 10.0.403 is the latest; .NET 11 is still RC. |
| III. Tested | ✅ | Full suite with E2E; unit test for version reading. |
| IV. Dependencies | ✅ | Everything on latest stable; npm now tracked. |
| V. Pages deploy | ✅ | Normal deploy. |
| VI. Accessible | ✅ | Diagram palettes unchanged. |
| VII. Documented | ✅ | Specs 006/008, constitution, README, docs updated. |
| VIII. Spec first | ✅ | Committed before the changes. |
| IX. Snappy and lean | ✅ | No runtime change. |

## Project Structure

```text
tools/mermaid/package.json, package-lock.json
tools/DocsBuilder/MermaidRenderer.cs, DocsBuilder.csproj
.github/dependabot.yml, .github/workflows/deploy.yml
docs/diagrams/*.svg
```
