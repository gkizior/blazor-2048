# Feature Specification: Core game

**Feature**: `001-core-game` | **Created**: 2026-10-08 | **Status**: Implemented

## Original Request

> 2048 puzzle game built with .NET 9 Blazor WebAssembly (PWA), hosted on GitHub Pages

- **Source**: the repository's original GitHub description, set with the first commit
  ([`93c5cee`](https://github.com/gkizior/blazor-2048/commit/93c5cee)). The original message asking for the game is not in the repository, so this
  spec was reconstructed afterwards from that description, the first commit and the follow-up
  standing rule "target .NET 10" (see [002](../002-blazor-first-minimal-interop/spec.md)), applied in [`bc0de4d`](https://github.com/gkizior/blazor-2048/commit/bc0de4d).
- **Date**: 2026-10-08

## User Scenarios & Testing

### User Story 1 - Play 2048 with the keyboard (Priority: P1)

A player opens the site on a desktop and plays 2048 with the arrow keys (or WASD).

**Why this priority**: It is the game.

**Independent Test**: Load the site, press arrow keys, see tiles slide, merge and spawn.

**Acceptance Scenarios**:

1. **Given** a new game, **When** the page loads, **Then** the 4x4 board shows exactly two tiles (2 or 4) and the board has keyboard focus.
2. **Given** a row `2 2 4 0`, **When** the player presses Left, **Then** the row becomes `4 4 0 0`, the score increases by 4 and one new tile appears.
3. **Given** a move that changes nothing, **When** the key is pressed, **Then** no tile spawns and the score is unchanged.
4. **Given** a tile reaches 2048, **When** the move completes, **Then** a "You win!" overlay offers "Keep going" and "New Game".
5. **Given** a full board with no equal neighbours, **When** the last move completes, **Then** "Game over!" appears with "Try again".

### User Story 2 - Play on a phone (Priority: P1)

**Independent Test**: Open the site on an iPhone-sized viewport and swipe.

**Acceptance Scenarios**:

1. **Given** a phone, **When** the player swipes left by at least 24 px, **Then** the board moves left.
2. **Given** a phone, **When** the player taps (moves less than 24 px), **Then** nothing moves and the page does not scroll.
3. **Given** a 390x844 viewport, **When** the game loads, **Then** the whole board fits without scrolling.

### User Story 3 - Best score survives a reload (Priority: P2)

**Acceptance Scenarios**:

1. **Given** a best score of 4096 was reached, **When** the page is reloaded, **Then** "Best" shows 4096.

### User Story 4 - Install and play offline (Priority: P2)

**Acceptance Scenarios**:

1. **Given** the site was opened once, **When** the device is offline, **Then** the game still loads (PWA service worker cache).
2. **Given** a supported browser, **When** the player chooses "Install", **Then** the app installs with its icon and name.

### Edge Cases

- Each tile merges at most once per move (`2 2 2 2` → `4 4`).
- Keys with Alt/Ctrl/Meta are ignored so browser shortcuts keep working.
- After winning and choosing "Keep going", play continues without the overlay.

## Requirements

### Functional Requirements

- **FR-001**: The game MUST implement standard 2048 rules on a 4x4 board in a pure C# engine (`Game2048.Core`).
- **FR-002**: New tiles MUST be 2 (90%) or 4 (10%) in a random empty cell after every move that changes the board.
- **FR-003**: Arrow keys and WASD MUST move the board; swipes of at least 24 px MUST move it on touch devices.
- **FR-004**: The best score MUST persist in browser storage across reloads.
- **FR-005**: The app MUST be an installable PWA that works offline after the first visit.
- **FR-006**: The app MUST deploy to GitHub Pages and work under the `/blazor-2048/` base path, including deep links.
- **FR-007**: The app MUST target .NET 10 (originally .NET 9, retargeted in [`bc0de4d`](https://github.com/gkizior/blazor-2048/commit/bc0de4d)).

## Success Criteria

- **SC-001**: Engine rules are covered by unit tests (slides, merges, spawn, win, game over).
- **SC-002**: The deployed site loads and is playable at <https://gkizior.github.io/blazor-2048/>.
- **SC-003**: The board fits a 390x844 viewport without scrolling (E2E test).

## Assumptions

- Standard 2048 rules; no undo, no animations beyond what [004](../004-fluid-animations/spec.md) adds.
