# Implementation Plan: [FEATURE]

**Feature**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: `[spec.md](spec.md)`

<!--
  Copy to specs/[###-feature-name]/plan.md once the spec is agreed. Adapted from GitHub Spec Kit's
  plan-template.md. Keep it true to what is actually built: update it if the approach changes.
-->

## Summary

[Primary requirement and the chosen technical approach, in a few sentences.]

## Technical Context

- **Language/Version**: C# / .NET 10 (Blazor WebAssembly)
- **Primary Dependencies**: [packages this feature adds or relies on, with licenses]
- **Storage**: [localStorage via BrowserStorage / build-time files / N/A]
- **Testing**: xUnit v3, bUnit, Playwright for .NET
- **Target Platform**: Modern browsers (desktop and mobile), GitHub Pages under `/blazor-2048/`
- **Performance Goals**: [e.g. 60 fps animations, no dropped input]
- **Constraints**: [e.g. offline-capable, no custom JS]

## Constitution Check

*Gate: must pass before implementation; re-check when the plan changes.*

| Principle | Status | Notes |
|---|---|---|
| I. Blazor/C# first, minimal JS | ✅ / ⚠️ | [interop used, if any, and why it is unavoidable] |
| II. .NET 10 | ✅ | |
| III. Tested, green before deploy | ✅ | [which suites cover it] |
| IV. Latest stable, free licenses | ✅ | [new packages and their licenses] |
| V. Deploy through GitHub Pages | ✅ | |
| VI. Accessible by default | ✅ | [contrast, reduced motion, keyboard/touch] |
| VII. Documented, with diagrams | ✅ | [docs pages touched] |
| VIII. Spec first | ✅ | |

## Design

[Key decisions, alternatives considered and why they lost. Mermaid diagrams welcome.]

## Project Structure

```text
[files and folders this feature adds or changes]
```

## Complexity Tracking

> Fill only if the Constitution Check has exceptions that must be justified.

| Exception | Why needed | Simpler alternative rejected because |
|---|---|---|
