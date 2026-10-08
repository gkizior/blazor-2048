# Implementation Plan: Dark mode and modern palette

**Feature**: `003-dark-mode-and-palette` | **Date**: 2026-10-08 | **Spec**: [spec.md](spec.md)

## Summary

Tokens on `:root` use `light-dark()`; `MainLayout` renders `data-theme="system|light|dark"` on
`.app-root`, which sets `color-scheme`. System mode needs no code: the browser applies
`prefers-color-scheme`. `ThemeService` loads and saves the choice through `BrowserStorage`.

## Technical Context

- **Storage**: `localStorage` key `blazor2048.theme` (`system`, `light`, `dark`)
- **Testing**: `ThemeTests` (bUnit), `ThemePaletteTests` (xUnit, contrast), `FeatureE2ETests` (Playwright)
- **Constraints**: no JS; WCAG AA contrast

## Constitution Check

| Principle | Status | Notes |
|---|---|---|
| I. Blazor/C# first | ✅ | CSS + Blazor-rendered attribute; storage via BrowserStorage. |
| II. .NET 10 | ✅ |  |
| III. Tested | ✅ | bUnit, contrast unit test, E2E persistence and system-scheme tests. |
| IV. Dependencies | ✅ | None added. |
| V. Pages deploy | ✅ |  |
| VI. Accessible | ✅ | 4.5:1 contrast enforced by a test; toggle has an aria-label. |
| VII. Documented | ✅ | `docs/theming-and-animations.md` (palette table, diagram). |
| VIII. Spec first | ⚠️ | Reconstructed after the fact. |

## Design

- Palette: warm neutrals → orange → gold → violet; pink-to-violet gradient for 4096+.
  Dark 2/4 tiles are slate blue (`#3e4556`, `#505971`) so they stand out from empty cells.
- `ThemeToggle` (`.icon-btn.theme-toggle`) shows sun/moon/auto icons (inline SVG in `Icon.razor`).
- Mermaid diagrams in the docs are rendered in light and dark variants and swapped by CSS.

## Project Structure

```text
src/Blazor2048/wwwroot/css/app.css         tokens, palette
src/Blazor2048/Layout/MainLayout.razor      .app-root[data-theme], theme-color
src/Blazor2048/Services/ThemeService.cs
src/Blazor2048/Components/ThemeToggle.razor, Icon.razor
tests/Blazor2048.Tests/ThemePaletteTests.cs
tests/Blazor2048.ComponentTests/ThemeTests.cs
```
