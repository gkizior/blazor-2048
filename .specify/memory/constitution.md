# Blazor 2048 Constitution

The non-negotiable principles for this repository, set by Garrett Kizior. Every spec, plan and
change is checked against them (see the "Constitution Check" in each `plan.md`). The layout mirrors
[GitHub Spec Kit](https://github.com/github/spec-kit) (`.specify/memory/constitution.md`), but the
files are written and maintained by hand: no Copilot, no `specify` CLI.

## Core Principles

### I. Blazor and C# first, minimal JavaScript (NON-NEGOTIABLE)

UI, state, input handling, theming, animation and docs rendering are written in C# and Razor.
No custom JavaScript files and no JavaScript libraries ship with the app; heavy JS interop counts
as cheating. The only interop allowed is what a feature cannot do without, each use documented:

- `localStorage.getItem` / `localStorage.setItem` through `Services/BrowserStorage.cs` (best score, theme);
- Blazor's own `ElementReference.FocusAsync` (focus the board);
- the framework's `blazor.webassembly.js`, and the PWA template's service worker
  (`service-worker.published.js`) with its one-line registration in `index.html`.

Anything that can be done at build time in C# (Markdown, Mermaid diagrams) is done at build time.
Test code may run JavaScript in the browser (Playwright `EvaluateAsync`); the app may not.

### II. .NET 10

All projects target `net10.0`. `global.json` pins the .NET 10 SDK (roll forward to the latest
feature band). Moving to a new major version is a constitution amendment.

### III. Tested, and green before deploy

- **xUnit v3** for logic, the **latest stable bUnit** for components, **Playwright for .NET**
  (`Microsoft.Playwright.Xunit.v3`) for end-to-end tests against the published site.
- Every feature ships with tests at the right level, and bugs get a regression test that fails
  before the fix.
- CI runs every suite; the deploy job depends on all of them. Nothing deploys red.
- Performance and smoothness claims are backed by measurements (`tools/PerfTrace`), not guesses.

### IV. Latest stable, freely licensed dependencies

- Every NuGet package, GitHub Action and the .NET SDK is on its highest **stable** version.
  Prereleases are never used.
- Only free-to-use licenses (MIT, Apache-2.0, BSD, MS-PL and similar). Commercial or paid
  licenses (for example FluentAssertions 8+) are not allowed; such packages stay on their last
  free version or are replaced.
- Dependabot checks `nuget`, `github-actions` and `dotnet-sdk` weekly on **Monday at 08:00
  America/Chicago**, grouping minor and patch updates per ecosystem. Updates are merged after CI
  passes (a follow-up check runs at 08:57 Monday Central). Pull requests run CI but never deploy.

### V. Deploy through GitHub Pages only

The app ships from `main` through `.github/workflows/deploy.yml` to
<https://gkizior.github.io/blazor-2048/>, under the `/blazor-2048/` base path and offline-capable.
No force-pushes and no history rewrites on `main`.

### VI. Accessible by default

- Text on every tile and surface meets **WCAG AA (4.5:1)** in light and dark themes, checked by a
  unit test.
- `prefers-reduced-motion` turns animation off; `prefers-color-scheme` drives the default theme.
- Keyboard (arrows/WASD) and touch (swipe) both work; controls have accessible names.
- Input is never dropped or blocked by animations.

### VII. Documented, with diagrams

Docs live in `/docs` as Markdown with **Mermaid** diagrams, are rendered into the in-app docs
viewer at build time, and are updated in the same change as the code they describe. The README
stays current (badges, screenshots, commands).

### VIII. Spec first

Every new feature request from Garrett gets a folder `specs/NNN-feature-name/` **before**
implementation, with `spec.md` (his request quoted verbatim, with source and date; user stories;
Given/When/Then acceptance criteria; `FR-###` requirements; success criteria), `plan.md` (the
technical approach and a Constitution Check) and `tasks.md` (tasks checked off and linked to
commits). See [Spec-driven development](../../docs/spec-driven-development.md).

### IX. Snappy and lean at every scale

Every supported configuration (each board size, phone and desktop) must feel as fast as the classic
4x4 game. Hot paths (moves, rendering) avoid per-move allocations and LINQ; components render only
when something visible changed; nothing grows without bound over a long session. Budgets and
evidence come from `tools/PerfTrace` (timings under 4x CPU throttling and on a phone viewport,
JS heap and WASM memory over long sessions) and from allocation tests.

## Quality Gates

A change is done when:

1. its spec, plan and tasks are up to date;
2. all suites pass locally (`dotnet test --solution Blazor2048.sln`, plus `RUN_E2E=1` against a
   Release publish) and in CI;
3. the deploy succeeded and the live site was verified (the footer commit matches);
4. docs and README reflect the change.

## Governance

This constitution overrides other conventions in the repo. Amendments are made by Garrett (or at
his request) in a commit that updates this file, bumps the version below and notes the change in
the affected specs. Plans that need an exception must say so in their "Complexity Tracking" table.

**Version**: 1.1.0 | **Ratified**: 2026-10-08 | **Last Amended**: 2026-10-08

Amendments:
- 1.1.0 (2026-10-08): added principle IX, "Snappy and lean at every scale", for
  [011 Board sizes](../../specs/011-board-sizes/spec.md).
