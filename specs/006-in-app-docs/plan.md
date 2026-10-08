# Implementation Plan: In-app DocFX-style docs with Mermaid

**Feature**: `006-in-app-docs` | **Date**: 2026-10-08 | **Spec**: [spec.md](spec.md)

## Summary

`tools/DocsBuilder` (Markdig, YamlDotNet) runs from an MSBuild target before static web assets are
resolved and writes `wwwroot/docs-content/` (one HTML fragment per page plus `index.json`). The
`Docs.razor` page fetches them with `HttpClient`. Mermaid blocks are pre-rendered with
mermaid-cli into committed SVGs named by content hash.

## Technical Context

- **Primary Dependencies**: Markdig 1.4.0 (BSD-2-Clause), YamlDotNet 18.1.0 (MIT); authoring only: @mermaid-js/mermaid-cli (MIT) via `npx`, Node 22+
- **Testing**: `DocsBuilderTests` (xUnit), `DocsPageTests` (bUnit), docs E2E tests

## Constitution Check

| Principle | Status | Notes |
|---|---|---|
| I. Blazor/C# first | ✅ | Build-time C#; no Markdown or Mermaid JS in the browser. |
| II. .NET 10 | ✅ |  |
| III. Tested | ✅ | Builder unit tests, bUnit page tests, E2E. |
| IV. Dependencies | ✅ | Markdig (BSD-2), YamlDotNet (MIT), mermaid-cli (MIT) at authoring time. |
| V. Pages deploy | ✅ | Static files under `/blazor-2048/`. |
| VI. Accessible | ✅ | Diagrams use `accTitle` as alt text. |
| VII. Documented | ✅ | `docs/docs-system.md`. |
| VIII. Spec first | ⚠️ | Reconstructed after the fact. |

## Design

**Mermaid: pre-render vs lazy JS library.** Pre-rendering won: zero runtime JS (principle I), works
offline, no layout shift, and dark mode is a second SVG swapped by CSS. The cost is Node at
authoring time, and SVGs are committed so normal builds don't need Node. CI renders any diagram
whose SVG is missing.

## Project Structure

```text
tools/DocsBuilder/          Program.cs, DocsSite.cs, MarkdownRenderer.cs, MermaidRenderer.cs
docs/*.md, docs/toc.yml     content and sidebar order
docs/diagrams/*.svg         pre-rendered diagrams (light + dark)
src/Blazor2048/Pages/Docs.razor, Services/DocsSource.cs
```
