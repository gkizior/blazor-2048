# Implementation Plan: Board sizes (4x4 to NxN)

**Feature**: `011-board-sizes` | **Date**: 2026-10-08 | **Spec**: [spec.md](spec.md)

## Summary

Generalize the engine to N×N with flat arrays and reusable buffers, add a split New Game button
with a size menu and a Custom… dialog (pure Blazor), store the best score per size and the last
size, and make the board and tile labels scale with N. Measure with `tools/PerfTrace` at several
sizes before and after optimizing, and pick the custom maximum from those numbers.

## Technical Context

- **Language/Version**: C# / .NET 10 (Blazor WebAssembly)
- **Primary Dependencies**: none added
- **Storage**: `localStorage` through `BrowserStorage`: `blazor2048.size`, `blazor2048.best.{N}x{N}` (4x4 migrated from `blazor2048.best`)
- **Testing**: xUnit v3 (engine for every size, validation, allocations), bUnit (split button, menu, dialog), Playwright for .NET (presets, custom, persistence, 10x10 rapid input)
- **Target Platform**: Modern browsers, phone and desktop
- **Performance Goals**: see spec SC-001…SC-003
- **Constraints**: no custom JS; reduced motion respected; no dropped input

## Constitution Check

| Principle | Status | Notes |
|---|---|---|
| I. Blazor/C# first | ✅ | Menu, dialog, focus and click-outside in Razor; focus via Blazor's `FocusAsync`. |
| II. .NET 10 | ✅ | |
| III. Tested | ✅ | Every size 2…MaxSize in unit tests; PerfTrace evidence before/after. |
| IV. Dependencies | ✅ | None added. |
| V. Pages deploy | ✅ | |
| VI. Accessible | ✅ | ARIA menu button pattern; dialog with label and live error; palette contrast unchanged. |
| VII. Documented | ✅ | Architecture, engine, components, theming/animations, testing updated with diagrams. |
| VIII. Spec first | ✅ | This spec was committed before the implementation. |
| IX. Snappy and lean | ✅ | Added for this feature (constitution 1.1.0); budgets in spec SC-001…SC-003. |

## Design

### Engine (`src/Game2048.Core`)

- `BoardSize`: `Min` 2, `Max` 16, `Default` 4, `Presets` 4…10, `TryParse` (whole numbers only, one
  message per kind of mistake) and `WinningTile(n)` = 2^(N+7) (guarded to N ≤ 23, so no overflow).
- `Game` keeps values, tile ids and flags (new / merged) in **flat arrays** indexed `row * N + col`,
  with a second set that a move writes into and swaps in. Each row or column is read into reusable
  line buffers and slid by `SlideLine` on spans, so one implementation serves all four directions at
  any N. Retired tiles and the render list are reused `List<Tile>` (`Tile` is a struct); the render
  list is rebuilt lazily and sorted with a cached comparison. `AddRandomTile` picks the k-th empty
  cell, `CanMove` and the win check scan the flat array. Buffers are allocated only when N changes.
- Tile ids, retired tiles and merge order are unchanged, so `@key` identity and the animation model
  work as before at every size.

### Components (`src/Blazor2048`)

- `NewGameButton`: the split button. The main part starts a game at the current size; the caret
  opens a `role="menu"` of `menuitemradio` items (4x4 classic, 5x5 … 10x10, Custom…) with roving
  focus (`FocusAsync`), ArrowUp/Down, Home/End, Enter/Space, Escape (focus back to the caret), Tab
  and a transparent backdrop for click-outside. Custom… opens a modal dialog (`role="dialog"`,
  `aria-modal`, labelled) whose input shows the `BoardSize.TryParse` message inline (`role="alert"`,
  `aria-invalid`, `aria-describedby`). No JavaScript.
- `GameBoard`: owns the size; `StartNewGame(n)` resizes the engine, saves the size and loads that
  size's best. A `SizeText` record caches everything that depends on N (the target text used by the
  `<h1>`, `<PageTitle>`, hint, win message and board label; `--n`; `--digits`), and the tile
  positions are precomputed strings (`transform: translate(calc(var(--step) * c), …)`), so a move
  formats nothing per tile. Tile class/label strings are cached per tile id and pruned without LINQ.
  The title number is keyed by the target, so changing the size replays a small flip (none with
  reduced motion).
- `BoardCells`: the N² background cells, rendered once per size.

### CSS sizing

- `--n` drives the grid: `--board` fits the viewport width and height, `--gap` shrinks with N, and
  `--cell`, `--step` and `--tile-radius` follow. These are registered with `@property` as
  `<length>`, so they are computed once instead of carried as token lists into every tile's style.
- Tile fonts scale with the cell and the label length (`tl-1` … `tl-10`). From 8x8, values from
  16384 up are written compactly (`16K`, `128K`, `1M`); from 12x12, from 1024 up (`1K`). The full
  value stays in `data-value` and the `title`.
- The `<h1>` font shrinks with the number of digits in the name (`--digits`), so `8388608` fits.

### Storage keys

| Key | Value |
|---|---|
| `blazor2048.size` | Last chosen N (invalid or missing → 4) |
| `blazor2048.best.{N}x{N}` | Best score for that size |
| `blazor2048.best` | The pre-011 4x4 best; copied to `blazor2048.best.4x4` on first read and left in place |

### The secret 1×1 board (third and fourth follow-ups, User Story 7)

| Size | Name | Board | Win screen | Remembered? |
|---|---|---|---|---|
| 1×1 | 256 (2^(1+7)) | one cell holding a 256 tile that pops in | "You made 256!" / "One tile, zero moves. Speedrun complete." | No |
| 0×0 | — | rejected ("A 0×0 board has nothing to play.") | — | — |

- `BoardSize.IsSecret` (1 only) and `IsSupported` (`IsValid` or secret); `TryParse` accepts 1 to 16
  but every message keeps saying "2 to 16". `IsValid` stays 2…16, so `BoardSizeStore` never restores
  the secret size.
- `Game.NewGame(1)` sets the only cell to the target and sets `HasWon` and `IsGameOver` without
  spawning; `IsInstantWin` tells the UI. Size 0 throws, like negatives.
- `GameBoard` shows a dedicated overlay (checked before "Game over!"), with **Play again** and
  **Back to N×N** (the last real size, tracked as `lastRealSize`), and no "Keep going". The 256 stays
  crisp in the upper part of its tile with the message in a band below; the overlay fades in after
  450 ms so the tile pops first (no motion with reduced motion).
- Persistence: choosing 1 saves nothing. The last size stays the previous real one, so a reload
  never reopens the instant win, and no best score is read or written (the Best box shows "–").
- History: a 0×0 "won by not playing" board was built for the third follow-up (`56a90c4`…`381018b`)
  and removed for the fourth.

## Measurements

All numbers from `tools/PerfTrace` (headless Chromium, 30 moves at the **rapid** pace of 50 ms
between keys, so every key lands mid-slide), single runs, on a local publish of the site. "Before"
is the first straightforward N×N version (2D arrays, LINQ, per-line lists, `--r`/`--c` custom
properties per tile); "after" is the shipped build. Profiles: `desktop` (no throttling),
`desktop-4x` and `mobile-4x` (390×844, touch) with 4x CPU throttling. Normal play keeps big boards
sparse (12–17 tiles on 10x10), so the tool warms each board up with N² moves first.

### Key to paint and dropped frames

| Size | Profile | Key→paint median, before → after (ms) | Dropped frames, before → after | Blazor ms/move, before → after | Style ms/move, before → after | Long tasks, before → after |
|---|---|---|---|---|---|---|
| 4×4 | desktop | 9.3 → 7.4 | 17.6% (one hitch) → 0% | 3.6 → 2.9 | 2.4 → 1.2 | 0 → 0 |
| 4×4 | desktop-4x | 30.8 → 23.3 | 1.5% → 0.5% | 15.8 → 13.0 | 10.6 → 6.1 | 2 → 0 |
| 4×4 | mobile-4x | 29.1 → 23.1 | 7.2% → 0% | 14.9 → 13.2 | 9.9 → 5.6 | 1 → 0 |
| 6×6 | desktop-4x | 36.7 → 24.3 | 8.6% → 9.0% | 15.8 → 12.8 | 18.2 → 7.4 | 5 → 0 |
| 6×6 | mobile-4x | 35.8 → 23.5 | 11.5% → 2.1% | 16.0 → 12.0 | 16.3 → 7.4 | 6 → 0 |
| 8×8 | desktop-4x | 35.4 → 27.1 | 12.3% → 2.1% | 15.3 → 13.4 | 16.8 → 9.0 | 1 → 0 |
| 8×8 | mobile-4x | 37.2 → 28.1 | 10.2% → 1.6% | 16.4 → 14.3 | 17.9 → 10.2 | 3 → 1 |
| 10×10 | desktop | 11.4 → 8.8 | 0% → 0% | 3.8 → 2.8 | 5.0 → 2.1 | 0 → 0 |
| 10×10 | desktop-4x | 36.1 → 27.5 | 11.5% → 3.6% | 16.3 → 12.9 | 20.5 → 11.4 | 7 → 0 |
| 10×10 | mobile-4x | 41.7 → 26.3 | 19.4% → 4.5% | 15.8 → 13.4 | 22.1 → 9.5 | 6 → 1 |
| 12×12 | desktop-4x | — → 29.4 | — → 7.7% | — → 13.1 | — → 11.8 | — → 1 |
| 12×12 | mobile-4x | — → 26.4 | — → 7.3% | — → 12.3 | — → 11.8 | — → 2 |
| 16×16 | desktop | 10.9 → 8.9 | 0% → 0% | 3.3 → 2.6 | 6.7 → 3.2 | 0 → 0 |
| 16×16 | desktop-4x | 50.5 → 33.0 | 24.6% → 9.1% | 14.3 → 12.8 | 32.8 → 16.8 | 11 → 2 |
| 16×16 | mobile-4x | 51.5 → 29.4 | 23.6% → 6.4% | 15.0 → 11.7 | 36.1 → 15.2 | 14 → 0 |
| 20×20 | desktop-4x | 50.9 → not shipped | 36.1% → — | 15.0 → — | 39.3 → — | 13 → — |
| 20×20 | mobile-4x | 66.6 → not shipped | 41.0% → — | 19.8 → — | 46.4 → — | 20 → — |

Unthrottled desktop drops no frames at any size after the change (key to paint 7.4–8.9 ms). No
scale snaps or teleports were recorded in any run, before or after. SC-001: at 16×16 the median key
to paint under 4x throttling is 1.42x (desktop) and 1.27x (mobile) of 4×4, and Blazor time per move
is flat (≈ 12–13 ms at every size); the remaining cost that grows with N is the browser's style work
for more tiles.

What made the difference, in order of effect:

1. **Registered custom properties.** `@property` for `--board`, `--gap`, `--cell`, `--step` and
   `--tile-radius` as `<length>`. A/B at 16×16 desktop-4x: style time 33.4 → 19.5 ms per move
   (0.196 → 0.096 ms per restyled element), key to paint 47–57 → 35 ms, dropped frames 26–29% →
   18–21%.
2. **Inline positions** (`transform: translate(calc(var(--step) * c), …)` precomputed per cell)
   instead of `--r`/`--c` per tile, so moving a tile changes one declaration.
3. **Engine and render path**: no allocation per move, render list reused, strings cached per tile
   and per size, `ShouldRender` in `NewGameButton` so a move doesn't re-render the menu.
4. **Tried and dropped**: CSS containment (`contain: strict`) on the tile layer and cells and
   `will-change: transform` on tiles. With registered properties, the build without containment was
   as good or slightly better (dropped 9.6–12.4% vs 15.7–20.5% at 16×16 desktop-4x), so it was
   removed. AOT was measured on an earlier feature: faster .NET code, but about +2 MB of Brotli
   download (+75%) and no fewer dropped frames, because the time is in the browser's style work, not
   in .NET; not used.

### Engine allocations (.NET 10 JIT, a move plus building the render list)

| Size | Before B/move | Before µs/move | After B/move | After µs/move |
|---|---|---|---|---|
| 4×4 | 3709 | 10.3 | 0 | 1.4 |
| 6×6 | 5888 | 6.1 | 0 | 1.9 |
| 8×8 | 8729 | 6.4 | 0 | 2.2 |
| 10×10 | 12530 | 7.1 | 0 | 1.9 |
| 12×12 | 15833 | 7.1 | 0 | 2.3 |
| 16×16 | 25610 | 10.1 | 0 | 3.0 |
| 20×20 | 39733 | 14.0 | 0 | 4.2 |

`AllSizesTests.Moves_And_The_Render_List_Do_Not_Allocate` keeps it that way (under 2000 bytes for
2000 moves; the runtime itself accounts for a few hundred).

### Memory (600 moves, then 50 new games, desktop; after a forced GC at each sample)

| Size | Build | JS heap start → 600 moves → 50 games (MB) | DOM nodes | JS listeners | WASM memory (MB) |
|---|---|---|---|---|---|
| 4×4 | before | 3.37 → 4.35 → 4.52 | 289–334 | 21 | 38.4 → **46.1** at move ~350 |
| 4×4 | after | 3.38 → 4.28 → 4.48 | 287–332 | 21 | 38.4 flat |
| 10×10 | before | 3.40 → 4.34 → 4.56 | 457–514 | 21 | 38.4 → **46.1** at move ~350 |
| 10×10 | after | 3.40 → 4.29 → 4.48 | 455–518 | 21 | 38.4 flat |
| 16×16 | after | 3.45 → 4.31 → 4.50 | 767–830 | 21 | 38.4 flat |
| 20×20 | before | 3.48 → 4.44 → 4.62 | 1057–1123 | 21 | 38.4 → **46.1** at move ~200 |

The JS heap creeps by about 1 MB over the first few hundred moves at every size, including 4×4, and
flattens (Blazor's interop and render batch buffers warming up); it does not depend on N. DOM nodes
follow the tile count and return to the same value after new games; event listeners never change.
Before, the per-move garbage made the .NET heap grow the WASM memory by 7.7 MB once; after, WASM
memory never grows. SC-003 holds.

### Choosing `BoardSize.Max` = 16

- **Readability decides.** On a 390 px phone the cells are about 31 px at 10×10, 26 px at 12×12,
  19.5 px at 16×16 and 15.6 px at 20×20. With compact labels a tile needs at most three or four
  characters (`128K` on 10×10 is about 11 px tall and fills 28 of 31 px); at 16×16 three characters
  (`16K`, `1K`) still read at about 8.5–10 px, at 20×20 they don't.
- **Speed allows it.** After the optimizations 16×16 stays within SC-001's 1.5x budget of 4×4 and
  drops nothing on unthrottled desktop. 20×20 did not fit the budget: 1.6x (desktop-4x) to 2.2x
  (mobile-4x) of 4×4 after the engine and render work, with 34–39% dropped frames, and its style
  work is about twice that of 10×10.
- **Overflow is not a concern**: the target at 16 is 2^23 = 8,388,608, and `WinningTile` refuses
  N > 23.
