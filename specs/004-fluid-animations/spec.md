# Feature Specification: Fluid tile animations

**Feature**: `004-fluid-animations` | **Created**: 2026-10-08 | **Status**: Implemented (revised 2026-10-08)

## Original Request

**First version.** The exact wording of the original animation request was not kept in the
repository. Its requirements as relayed in the feature task (dark mode, animations, footer, docs),
condensed:

- Smooth slide, merge and spawn animations, done in Blazor and CSS.
- Tile elements stay stable across moves (`@key`) so tiles slide instead of being recreated.
- Respect `prefers-reduced-motion`.
- Fast key presses must never be dropped.

**Revision ("feels choppy").** Opening and rules, verbatim:

> Garrett says the new animations make blazor-2048 feel choppy and wants them more fluid. Find the real cause and fix it, without hurting responsiveness.

> Keep the earlier rules: Blazor first with no custom JS or JS libraries, respect reduced motion, and no dropped inputs.

The rest of that message, condensed: investigate with evidence (Playwright for .NET performance
traces of rapid moves, locally and on the live site, with 4x CPU throttling and a mobile viewport;
dropped frames, long tasks, style/layout work, Blazor render time per move); check merge sources,
slide/spawn delays, durations and easing (about 100–120 ms, snappy ease-out), retargeting on rapid
input, re-render cost (`ShouldRender`, `@key`), `top`/`left`/`will-change`/`box-shadow`, and
AOT/wasm-tools; update bUnit and Playwright tests; report before/after measurements.

- **Source**: Message from Garrett, relayed to the implementing assistant
- **Date**: 2026-10-08 (first version and revision)

## User Scenarios & Testing

### User Story 1 - Merges read clearly (Priority: P1)

Two tiles slide together, and only when they meet does the doubled tile pop.

**Independent Test**: Slow the animation tokens 10x and film one merge (see the Evidence section in [plan.md](plan.md)).

**Acceptance Scenarios**:

1. **Given** two equal tiles in a row, **When** they merge, **Then** both source tiles slide on their own elements into the target cell.
2. **Given** a merge, **When** the sources are still sliding, **Then** the merged tile is invisible; it pops only after the slide.
3. **Given** a new tile, **When** it spawns, **Then** it scales in after the slide, from transparent.

### User Story 2 - Rapid input stays fluid (Priority: P1)

**Acceptance Scenarios**:

1. **Given** a slide in progress, **When** the next key arrives, **Then** the tile retargets from where it is (no jump).
2. **Given** a spawn or pop in progress, **When** the next move happens, **Then** that animation finishes instead of snapping to full size.
3. **Given** six keys 40 ms apart, **When** they are pressed, **Then** six moves are applied (no dropped input).

### User Story 3 - Reduced motion (Priority: P1)

**Acceptance Scenarios**:

1. **Given** `prefers-reduced-motion: reduce`, **When** tiles move, **Then** there are no transitions or keyframe animations.

### Edge Cases

- With keys about 50 ms apart, a few merges can pop before their sources land, because the next move redirects the sources first.
- Under 4x CPU throttling, the first one or two frames of a move can still drop while the main thread runs the move.

## Requirements

### Functional Requirements

- **FR-001**: Tiles MUST be keyed by a stable tile id and positioned with `transform` from `--r`/`--c` custom properties.
- **FR-002**: Only `transform` and `opacity` may animate (no `top`/`left`, `box-shadow` or `filter` animation).
- **FR-003**: Slide MUST use `--slide` (110 ms) with a snappy ease-out; spawn (`--spawn`, 150 ms) and pop (`--pop`, 180 ms) MUST start after the full slide.
- **FR-004**: Spawn and pop keyframes MUST start at `opacity: 0` so nothing shows during their delay.
- **FR-005**: A tile's animation class MUST be fixed for its whole life.
- **FR-006**: The board MUST NOT re-render for no-op moves, other keys, or after the best-score save; the static cells MUST render once.
- **FR-007**: No custom JavaScript or JS animation libraries.
- **FR-008**: `prefers-reduced-motion: reduce` MUST disable transitions and animations.

## Success Criteria

- **SC-001**: At a normal pace, 0% of merges pop before their sources arrive (was 100%).
- **SC-002**: 0 spawn/pop scale snaps during rapid input (was about 80 in 48 moves).
- **SC-003**: Median input-to-DOM time does not regress (desktop 3.9 → 3.4 ms; 4x throttled 18–20 → 13.5–18.7 ms).
- **SC-004**: All suites green, including the frame-sampling E2E tests.
