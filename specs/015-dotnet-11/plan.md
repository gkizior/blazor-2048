# Implementation Plan: Upgrade to .NET 11

**Feature**: `015-dotnet-11` | **Date**: 2026-11-17 | **Spec**: [spec.md](spec.md)

## Summary

Retarget the whole solution from `net10.0` / SDK 10.0.404 to `net11.0` / SDK 11.0.100, bump the
Blazor WebAssembly packages to 11.0.0 (MIT, same license), amend constitution principle II, sweep
docs and specs, point Dependabot's major-ignore rules at the next major (.NET 12), and ship through
a PR once CI is green.

This is a constitution amendment: principle II becomes ".NET 11".

## Technical Context

- **Language/Version**: C# / .NET 11 (Blazor WebAssembly); first built on .NET 9, then .NET 10
- **Primary Dependencies** (licenses verified on the versions used):

| Package / tool | From | To | License |
|---|---|---|---|
| .NET SDK (`global.json`) | 10.0.404 | 11.0.100 | MIT |
| Target framework | `net10.0` | `net11.0` | — |
| Microsoft.AspNetCore.Components.WebAssembly (+ DevServer) | 10.0.15 | 11.0.0 | MIT |
| bunit | 2.15.2 | 2.15.2 (already latest) | MIT |
| xunit.v3 / xunit.runner.visualstudio | 4.0.3 / 4.0.1 | unchanged (already latest) | Apache-2.0 |
| Microsoft.NET.Test.Sdk | 18.12.0 | unchanged (already latest) | MIT |
| Microsoft.Playwright (+ .Xunit.v3) | 1.64.1 | unchanged (already latest) | MIT |
| Markdig / YamlDotNet | 1.6.0 / 18.1.0 | unchanged (already latest) | BSD-2-Clause / MIT |
| @mermaid-js/mermaid-cli / puppeteer | 12.2.0 / 25.17.0 | unchanged (already latest) | MIT / Apache-2.0 |

- **Storage**: unchanged (`localStorage` via `BrowserStorage`)
- **Testing**: xUnit v3, bUnit, Playwright for .NET; full suite with `RUN_E2E=1`; 10x10 sanity check with `tools/PerfTrace`
- **Target Platform**: Modern browsers, GitHub Pages under `/blazor-2048/`
- **Constraints**: No prereleases; Actions stay on `@vN` tags; csproj files stay slim (spec 014)

Held back: none. .NET 12 majors will be ignored by Dependabot until Garrett decides.

## Constitution Check

*This plan amends principle II.*

| Principle | Status | Notes |
|---|---|---|
| I. Blazor/C# first, minimal JS | ✅ | No new interop. |
| II. .NET 11 | ✅ | The amendment: `net11.0`, SDK 11.0.100. |
| III. Tested, green before deploy | ✅ | Full suite including E2E; deploy from the squash-merge only. |
| IV. Latest stable, free licenses | ✅ | WASM 11.0.0 is MIT; other packages already latest. No prereleases. |
| V. Deploy through GitHub Pages | ✅ | Same workflow; `setup-dotnet` uses `11.0.x`. PR, then squash-merge. |
| VI. Accessible by default | ✅ | No UI behavior change. |
| VII. Documented, with diagrams | ✅ | README, constitution, docs, specs, social preview, repo metadata. |
| VIII. Spec first | ✅ | This spec, written before the changes. |
| IX. Snappy and lean at every scale | ✅ | Engine unchanged; 10x10 PerfTrace sanity check after the retarget. |

## Design

`TargetFramework` already lives in `Directory.Build.props` (spec 014), so the retarget is one line,
plus `global.json`, the two WebAssembly package versions, the workflow's `dotnet-version` and
Playwright path, tests and meta that assert ".NET 10", and the docs/spec sweep.

`RuntimeInformation.FrameworkDescription` already feeds the footer, so the live text becomes
".NET 11.0.0" without a footer code change (comment and tests only).

`actions/setup-dotnet` stays on a `dotnet-version: 11.0.x` tag (plain `@v6`), which installs the
latest 11.0 patch; `global.json` pins the minimum. Same pattern as before.

Dependabot's existing `semver-major` ignore rules for the SDK and the Microsoft.AspNetCore.* /
Microsoft.Extensions.* packages already hold the next major; only the comments change, from
"stay on .NET 10" to "hold .NET 12 majors for a decision".

Historical specs keep verbatim quotes ("target .NET 10") and historical measurements
(".NET 10 JIT: …"). Current-state requirements, constitution checks, and version tables are
updated and point at this spec for the retarget.

The social preview HTML said ".NET 10" and is re-rendered to a new `docs/images/social-preview.png`.
GitHub Settings still needs a manual upload (no API; same as spec 009).

Publish output: framework files, hash-suffixed WASM/JS, and generated docs will differ because
the runtime and WebAssembly packages changed. The site structure (index.html, 404.html, .nojekyll,
docs-content, service-worker, manifest, CSS) should stay the same. Differences are listed in the
PR and in tasks.md.

## Project Structure

```text
global.json
Directory.Build.props
src/Blazor2048/Blazor2048.csproj
src/Blazor2048/Services/BuildInfo.cs
src/Blazor2048/wwwroot/index.html
src/Blazor2048/wwwroot/manifest.webmanifest
.github/workflows/deploy.yml
.github/dependabot.yml
.specify/memory/constitution.md
.specify/templates/plan-template.md
README.md
docs/toc.yml
docs/spec-driven-development.md
docs/build-and-deploy.md
docs/testing.md
docs/game-engine.md
scripts/social-preview.html
docs/images/social-preview.png
specs/015-dotnet-11/
specs/014-action-tags-and-clean-csproj/tasks.md
tests/Blazor2048.ComponentTests/AppTestContext.cs
tests/Blazor2048.ComponentTests/FooterTests.cs
tests/Blazor2048.E2ETests/FeatureE2ETests.cs
plus the spec/docs sweep listed in tasks.md
```

## Complexity Tracking

None. No constitution exception; principle II is amended in the same change.
