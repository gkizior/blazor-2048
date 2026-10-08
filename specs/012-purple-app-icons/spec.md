# Feature Specification: Purple app icons

**Feature**: `012-purple-app-icons` | **Created**: 2026-10-08 | **Status**: Implemented (written before the icons were regenerated)

## Original Request

> I want the favicon to match the new purple icon 2048 instead of yellow

- **Source**: Message from Garrett, relayed to the implementing assistant
- **Date**: 2026-10-08

The relayed task added (condensed): regenerate `favicon.png`/`.ico`, the apple-touch-icon and the PWA
manifest icons (192, 512, maskable) from the purple 2048 tile in the current palette, in the style of
the social preview; bump the service-worker cache so the new icons show up; update
`theme_color`/`background_color` if they are still yellow; verify on the live site; ship it with the
board sizes batch (spec 011).

## User Scenarios & Testing

### User Story 1 - The tab icon is the purple 2048 tile (Priority: P1)

**Independent Test**: Open the site and look at the browser tab.

**Acceptance Scenarios**:

1. **Given** the site in a browser tab, **When** it loads, **Then** the favicon is the purple 2048 tile (`#6a3df0`, white text), not the old yellow one.
2. **Given** a browser that asks for `favicon.ico`, **When** it loads it, **Then** it gets 16, 32 and 48 px purple renders.

### User Story 2 - Installed app icons match (Priority: P1)

**Acceptance Scenarios**:

1. **Given** the PWA is installed on Android, **When** the launcher masks the icon (circle, squircle), **Then** the maskable icon fills the shape and "2048" stays inside the safe zone.
2. **Given** "Add to Home Screen" on iOS, **When** the icon is shown, **Then** it is a full-bleed purple square (iOS rounds it; no black corners).
3. **Given** a returning visitor with the old offline cache, **When** the new version activates, **Then** the old cache is dropped and the purple icons are served.

### Edge Cases

- Tiny favicons (16 px): "2048" is drawn heavier and tighter so it stays legible.
- The manifest's `theme_color`/`background_color` are the app's light background (`#f6f3ee`), not yellow, so they stay.

## Requirements

### Functional Requirements

- **FR-001**: `favicon.png` (32 px), `favicon.ico` (16/32/48 px), `apple-touch-icon.png` (180 px, full bleed), `icon-192.png`, `icon-512.png` (rounded tile) and `icon-maskable-192.png`/`icon-maskable-512.png` MUST show the purple 2048 tile from the palette (`--tile-2048-bg: #6a3df0`) with white "2048" in Inter, like the social preview.
- **FR-002**: The icons MUST be generated from a committed source (`scripts/app-icons.html`) so they can be regenerated.
- **FR-003**: The manifest MUST list the maskable icons with `"purpose": "maskable"`; `index.html` MUST link `favicon.ico`, `favicon.png` and the 180 px apple-touch-icon.
- **FR-004**: The service worker's offline cache name MUST change (revision bump) so returning visitors get the new icons.
- **FR-005**: The app name, manifest name and docs stay as they are (spec 011's name easter egg does not touch them).

## Success Criteria

- **SC-001**: A unit test checks every manifest icon exists with the declared pixel size, the maskable icons are declared, and `favicon.ico` holds 16/32/48 px images.
- **SC-002**: The live site serves the purple icons (checked after deploy), and the browser tab shows the purple favicon.

## Assumptions

- "The new purple icon 2048" is the purple 2048 tile from spec 003's palette, as drawn in the social preview (spec 009).
