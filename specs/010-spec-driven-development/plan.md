# Implementation Plan: Spec-driven development (Spec Kit layout)

**Feature**: `010-spec-driven-development` | **Date**: 2026-10-08 | **Spec**: [spec.md](spec.md)

## Summary

Plain Markdown in the Spec Kit layout, adapted: the spec template gains an "Original Request"
section, and the tasks template links tasks to commits. The docs builder learns nested TOC items so
each spec's plan and tasks sit under it in the sidebar. A unit test keeps the folders complete.

## Technical Context

- **Primary Dependencies**: none added
- **Testing**: `SpecsTests` (xUnit), `DocsBuilderTests` (nested TOC), `DocsPageTests` (bUnit)

## Constitution Check

| Principle | Status | Notes |
|---|---|---|
| I. Blazor/C# first | ✅ | Sidebar nesting in Razor; no JS. |
| II. .NET 10 | ✅ |  |
| III. Tested | ✅ | `SpecsTests` plus docs builder and page tests. |
| IV. Dependencies | ✅ | None. |
| V. Pages deploy | ✅ | Specs ship with the docs. |
| VI. Accessible | ✅ | Nested links are a plain list; current page marked with `aria-current`. |
| VII. Documented | ✅ | `docs/spec-driven-development.md`. |
| VIII. Spec first | ✅ | This spec introduces principle VIII. |

## Project Structure

```text
.specify/memory/constitution.md
.specify/templates/spec-template.md, plan-template.md, tasks-template.md
specs/001-core-game/ … specs/010-spec-driven-development/
docs/spec-driven-development.md
docs/toc.yml                              Specs section with nested items
tools/DocsBuilder/DocsSite.cs             nested TOC → DocPage.Level/Parent
src/Blazor2048/Pages/Docs.razor           .toc-child links, breadcrumb with parent
tests/Blazor2048.Tests/SpecsTests.cs
```
