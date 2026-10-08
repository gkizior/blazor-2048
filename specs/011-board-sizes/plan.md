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

To be completed with the implementation: engine data layout, component structure, CSS sizing,
storage keys, measurements and the chosen maximum.
