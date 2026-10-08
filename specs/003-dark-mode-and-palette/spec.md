# Feature Specification: Dark mode and modern palette

**Feature**: `003-dark-mode-and-palette` | **Created**: 2026-10-08 | **Status**: Implemented

## Original Request

The exact wording of this request was not kept in the repository. These are its requirements as
relayed in the feature task (dark mode, animations, footer, docs), condensed:

- Dark mode and a modern palette, built on CSS custom properties.
- System default via `prefers-color-scheme`, plus a toggle.
- The choice persists through the existing `localStorage` path.
- The theme is applied through a class/attribute on a root element that Blazor renders.
- Good contrast for every tile, including 2048 and above.

Standing rules from the same message, verbatim:

> Blazor/C# first with minimal JS interop (he considers heavy interop cheating, so no custom JS unless unavoidable)

- **Source**: Message from Garrett, relayed to the implementing assistant
- **Date**: 2026-10-08

## User Scenarios & Testing

### User Story 1 - Follow the system theme (Priority: P1)

**Independent Test**: Load the site with the OS in dark mode, then light mode.

**Acceptance Scenarios**:

1. **Given** no saved choice and the OS in dark mode, **When** the site loads, **Then** it renders dark.
2. **Given** no saved choice and the OS in light mode, **When** the site loads, **Then** it renders light.

### User Story 2 - Pick a theme and keep it (Priority: P1)

**Acceptance Scenarios**:

1. **Given** System mode, **When** the toggle is pressed, **Then** the theme cycles System → Light → Dark → System and the button's accessible name says the current and next mode.
2. **Given** Dark was chosen, **When** the page is reloaded, **Then** it is still dark (`localStorage` key `blazor2048.theme`).
3. **Given** the docs page, **When** the theme is toggled there, **Then** the game uses the same theme.

### User Story 3 - Readable tiles everywhere (Priority: P1)

**Acceptance Scenarios**:

1. **Given** either theme, **When** any tile from 2 to 4096+ is shown, **Then** its text contrast is at least 4.5:1.
2. **Given** dark mode, **When** 2 and 4 tiles sit on the board, **Then** they are clearly distinct from empty cells.

### Edge Cases

- Browser chrome colour (`theme-color`) follows the theme, including System mode via media queries.
- If the saved theme differs from the OS, a brief flash of the OS theme on first paint is accepted (avoiding it would need inline JS).

## Requirements

### Functional Requirements

- **FR-001**: All colours MUST be CSS custom properties with light and dark values (`light-dark()`).
- **FR-002**: The default MUST follow `prefers-color-scheme`.
- **FR-003**: A toggle MUST cycle System, Light and Dark, and the choice MUST persist via `BrowserStorage`.
- **FR-004**: The theme MUST be applied as `data-theme` and a `theme-*` class on `.app-root`, rendered by `MainLayout`.
- **FR-005**: Every tile background/text pair MUST meet 4.5:1 contrast in both themes.
- **FR-006**: `<meta name="theme-color">` MUST match the theme.

## Success Criteria

- **SC-001**: `ThemePaletteTests` computes every tile's contrast from `app.css` and passes.
- **SC-002**: E2E: a dark choice survives a reload; System follows the emulated colour scheme.

## Assumptions

- `light-dark()` support (Safari/iOS 17.5+, current Chrome/Edge/Firefox) is acceptable.
