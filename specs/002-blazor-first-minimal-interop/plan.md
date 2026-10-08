# Implementation Plan: Blazor first, minimal JavaScript interop

**Feature**: `002-blazor-first-minimal-interop` | **Date**: 2026-10-08 | **Spec**: [spec.md](spec.md)

## Summary

Treat JavaScript as a last resort. Everything visual is CSS driven by state that Blazor renders;
everything that a library would do in the browser is done in C# at build time.

## Technical Context

- **Language/Version**: C# / .NET 10
- **Primary Dependencies**: none added for this rule
- **Testing**: code review plus the feature tests that prove CSS-only behaviour (theme, animations, docs)

## Constitution Check

| Principle | Status | Notes |
|---|---|---|
| I. Blazor/C# first | ✅ | This spec is the source of principle I. |
| II. .NET 10 | ✅ |  |
| III. Tested | ✅ | Theme, animation and docs tests run without app JS. |
| IV. Dependencies | ✅ | No JS packages. |
| V. Pages deploy | ✅ |  |
| VI. Accessible | ✅ |  |
| VII. Documented | ✅ | `docs/architecture.md` ("JavaScript policy"). |
| VIII. Spec first | ⚠️ | Reconstructed after the fact. |

## Design

| Need | How it is met without custom JS |
|---|---|
| Persist best score and theme | `BrowserStorage` → `localStorage.getItem`/`setItem` (the only interop) |
| Focus the board | Blazor's `ElementReference.FocusAsync` |
| Theme | `data-theme` on `.app-root` + CSS `light-dark()`; `theme-color` via `HeadContent` |
| Animations | CSS transitions/keyframes on elements keyed by tile id |
| Markdown docs | Markdig in `tools/DocsBuilder` at build time |
| Mermaid diagrams | mermaid-cli at authoring time; committed SVGs |

## Project Structure

```text
src/Blazor2048/Services/BrowserStorage.cs   the only IJSRuntime use
tools/DocsBuilder/                          build-time Markdown + Mermaid
```
