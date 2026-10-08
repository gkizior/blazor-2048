# Feature Specification: In-app DocFX-style docs with Mermaid

**Feature**: `006-in-app-docs` | **Created**: 2026-10-08 | **Status**: Implemented

## Original Request

The exact wording of this request was not kept in the repository. Its requirements as relayed in
the feature task, condensed:

- A Docs button that opens a DocFX-style docs UI: a sidebar listing every `.md` in the repo, a
  content pane, and navigation.
- Real docs in `/docs` (architecture, engine, components, theming/animations, testing,
  build/deploy) with Mermaid diagrams.
- Markdown rendered in C# at build time.
- Weigh pre-rendering Mermaid against a lazy JS library.
- Must work under the `/blazor-2048/` base path and offline.

- **Source**: Message from Garrett, relayed to the implementing assistant
- **Date**: 2026-10-08

## User Scenarios & Testing

### User Story 1 - Read the docs inside the app (Priority: P1)

**Independent Test**: Click the book icon, browse pages, follow links, come back to the game.

**Acceptance Scenarios**:

1. **Given** the game, **When** the Docs button is pressed, **Then** `/docs` opens with a sidebar of every Markdown file in the repo, grouped into sections.
2. **Given** a docs page, **When** a link to another `.md` is followed, **Then** that page opens inside the viewer.
3. **Given** a docs page, **When** "Play" is pressed, **Then** the game returns with the same board.
4. **Given** a deep link such as `/blazor-2048/docs/game-engine`, **When** it is opened directly, **Then** that page renders.

### User Story 2 - Diagrams that match the theme (Priority: P1)

**Acceptance Scenarios**:

1. **Given** a page with a Mermaid diagram, **When** it is shown, **Then** a rendered SVG appears with no diagram JavaScript in the page.
2. **Given** dark mode, **When** the page is shown, **Then** the dark variant of each diagram is displayed.

### User Story 3 - Works offline (Priority: P2)

**Acceptance Scenarios**:

1. **Given** the site was visited once, **When** the device is offline, **Then** docs pages and their diagrams still load.

### Edge Cases

- Unknown slugs show a "not found" state with a way back.
- The sidebar filter matches page titles and headings.

## Requirements

### Functional Requirements

- **FR-001**: Every `.md` file in the repo MUST appear in the sidebar (ordered by `docs/toc.yml`, the rest under "More").
- **FR-002**: Markdown MUST be converted to HTML in C# at build time.
- **FR-003**: Mermaid MUST be pre-rendered to light and dark SVGs; no Mermaid JavaScript at runtime.
- **FR-004**: Links MUST be rewritten for in-app routes, GitHub source links and images, under the `/blazor-2048/` base path.
- **FR-005**: The viewer MUST provide breadcrumbs, previous/next links, an "On this page" outline and a link to the source on GitHub.
- **FR-006**: Docs content MUST be cached by the service worker for offline use.

## Success Criteria

- **SC-001**: The docs build produces no warnings for the real repository (unit test).
- **SC-002**: E2E: docs open from the game, show a loaded diagram SVG, deep links work, no console errors.
