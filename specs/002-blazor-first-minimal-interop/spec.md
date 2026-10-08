# Feature Specification: Blazor first, minimal JavaScript interop

**Feature**: `002-blazor-first-minimal-interop` | **Created**: 2026-10-08 | **Status**: Implemented (standing rule)

## Original Request

> Garrett's standing rules: Blazor/C# first with minimal JS interop (he considers heavy interop cheating, so no custom JS unless unavoidable), test with bUnit + Playwright for .NET (plus xUnit), target .NET 10, and deploy through the existing Pages workflow.

Restated with the animation work:

> Keep the earlier rules: Blazor first with no custom JS or JS libraries, respect reduced motion, and no dropped inputs.

- **Source**: Message from Garrett, relayed to the implementing assistant (feature task for dark mode, animations, footer and docs; and the animation choppiness task)
- **Date**: 2026-10-08

## User Scenarios & Testing

### User Story 1 - The app is C#, not JavaScript (Priority: P1)

Garrett reads the repo and finds the UI, state, theming, animation and docs written in C#/Razor
and CSS, with no hand-written JavaScript and no JS libraries.

**Independent Test**: List the files under `src/Blazor2048/wwwroot` and the interop calls in `src/`.

**Acceptance Scenarios**:

1. **Given** the published site, **When** its scripts are listed, **Then** only Blazor's framework files and the template's service worker are present.
2. **Given** the source, **When** searching for `IJSRuntime` use, **Then** only `BrowserStorage` (`localStorage.getItem`/`setItem`) appears, plus Blazor's built-in `FocusAsync`.
3. **Given** a feature that could use a JS library (Markdown, Mermaid), **When** it is built, **Then** it is done in C# at build time instead.

### Edge Cases

- Tests may run JavaScript in the browser (Playwright `EvaluateAsync`); the app itself may not.

## Requirements

### Functional Requirements

- **FR-001**: The app MUST NOT ship custom `.js` files or JavaScript libraries.
- **FR-002**: Browser APIs without a Blazor equivalent MUST be wrapped in one small C# service (`BrowserStorage`) and documented.
- **FR-003**: Theme switching, animation and docs rendering MUST work without JavaScript beyond Blazor's runtime (CSS state rendered by Blazor; Markdown and Mermaid rendered at build time).
- **FR-004**: Each new interop use MUST be justified in its feature plan's Constitution Check.

## Success Criteria

- **SC-001**: Exactly two interop entry points in app code: `localStorage.getItem` and `localStorage.setItem`.
- **SC-002**: The deployed docs show Mermaid diagrams with no diagram JavaScript loaded.

## Assumptions

- Blazor's own `blazor.webassembly.js` and the PWA template's registration line are part of the framework, not custom JS.
