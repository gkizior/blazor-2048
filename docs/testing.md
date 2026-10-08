# Testing strategy

Three test projects, three levels. All use **xUnit v3** (`xunit.v3`) and run on
**Microsoft.Testing.Platform**, which `global.json` turns on for `dotnet test`.

```mermaid
flowchart TB
    accTitle: Test pyramid
    E2E["E2E: Playwright for .NET<br/>published site in headless Chromium<br/>(few, slow, most realistic)"]
    Comp["Components: bUnit<br/>Razor components rendered in memory"]
    Unit["Logic: xUnit<br/>engine rules, tile tracking, docs generator, palette contrast<br/>(many, fast)"]
    E2E --- Comp --- Unit
```

## Logic tests: `tests/Blazor2048.Tests`

Plain xUnit tests with no UI.

- **Rules:** `SlideRow` theory data covers merges, gaps, and the "merge once" rule; scenario tests
  load exact boards with `SetBoard` and check each direction, spawning, winning and game over.
- **Tile tracking:** ids survive slides, merges retire both halves onto the target and create a new
  id, spawns are flagged, ids stay unique and ordered across many random moves.
- **Docs generator:** Markdown to HTML, Mermaid blocks to `<img>` pairs, link rewriting, headings,
  and that every doc in the repo is in the index.
- **Palette:** parses `app.css` and checks every tile's text/background contrast is at least 4.5:1.

## Component tests: `tests/Blazor2048.ComponentTests`

[bUnit](https://bunit.dev) renders components without a browser. Test classes derive from
`BunitContext`, register services, and use `Render<T>()`. JS interop runs in loose mode, so
`localStorage` calls return defaults and can be verified or stubbed with `JSInterop.Setup`.

Covered: board rendering and tile classes, keys and swipes, New Game, win/lose overlays, best score
loading, the theme toggle cycling and setting `data-theme` on the layout root, theme persistence,
the footer's name and build info, the docs button, and the docs page (TOC, filter, rendered
Markdown, diagrams, not-found) with a fake `IDocsSource`.

## End-to-end tests: `tests/Blazor2048.E2ETests`

[Playwright for .NET](https://playwright.dev/dotnet/) with `Microsoft.Playwright.Xunit.v3`:

- An xUnit v3 **assembly fixture** (`SiteServer`) serves the published `wwwroot` with Kestrel under
  the same base path as GitHub Pages, including a `404.html` fallback for deep links.
- Test classes derive from `AppTest`, which extends Playwright's `BrowserTest` (browser lifecycle,
  `BROWSER`/`HEADED` environment variables) and adds `OpenAsync()`.
- Tests: app loads and focuses the board; arrow keys move tiles; the layout fits iPhone screens;
  dark mode persists across a reload; docs open and show a rendered Mermaid diagram; rapid key
  presses during animations are all applied; no console errors.
- `AnimationE2ETests` samples every tile's box and opacity once per frame (a test-side
  `requestAnimationFrame` loop; the app ships no such code) and asserts that merge sources reach
  the target before they are removed, the merged tile stays invisible until they arrive, rapid
  input never snaps a spawn/pop animation, and retargeted slides never jump.

E2E tests are opt-in locally because they need a browser:

```bash
# once: install Chromium for Playwright
pwsh tests/Blazor2048.E2ETests/bin/Debug/net10.0/playwright.ps1 install chromium
RUN_E2E=1 dotnet test --project tests/Blazor2048.E2ETests
```

Set `E2E_SITE_DIR` to test an existing publish folder (CI does this with the exact files it deploys).

## Animation performance

`tools/PerfTrace` is a developer tool for measuring how the board animates. It
drives a published site with Playwright for .NET and, for desktop, desktop with 4x CPU throttling
and a 390x844 mobile viewport with 4x throttling, at two input paces (a key every 50 ms and every
250 ms), it records:

- a **Chromium performance trace** over CDP (`Tracing.start`), saved as `*.trace.json` and
  loadable in DevTools' Performance panel. From it: compositor frames dropped or presented
  (`PipelineReporter`), script time per keydown (Blazor's event handling, render and DOM patch),
  style, layout and paint time per move, and long tasks;
- input-to-DOM latency (keydown to the first mutation of the tile layer);
- a per-frame sample of every tile, used to count visual glitches: merge sources removed before
  arriving, merged tiles visible before their sources land, scale animations cut short, and
  slides that jump instead of animating.

```bash
dotnet publish src/Blazor2048 -c Release -o /tmp/site
python3 scripts/prepare-pages.py /tmp/site/wwwroot /blazor-2048/
# serve /tmp/site/wwwroot under /blazor-2048/ (any static server), then:
dotnet run --project tools/PerfTrace -c Release -- --url http://127.0.0.1:8765/blazor-2048/ --label local --out perf-results
dotnet run --project tools/PerfTrace -c Release -- --url https://gkizior.github.io/blazor-2048/ --label live --out perf-results
```

Options: `--runs N` (default 2), `--moves N` (default 24), `--profiles desktop,desktop-4x,mobile-4x`.
It prints a Markdown table and writes `<label>.json` plus the traces to `--out` (`perf-results/`
is git-ignored). It uses the same Playwright Chromium install as the E2E tests. CI builds it with the solution so it keeps compiling, but never runs it:
the numbers depend on the machine, so compare runs from the same machine only.
[Theming and animations](theming-and-animations.md#why-it-felt-choppy-and-how-it-was-measured)
has the findings that led to the current animation design.

## Running everything

```bash
dotnet test                       # logic + bUnit (E2E tests report as skipped)
RUN_E2E=1 dotnet test             # everything
```
