# Feature Specification: Board sizes (4x4 to NxN)

**Feature**: `011-board-sizes` | **Created**: 2026-10-08 | **Status**: Draft (written before implementation)

## Original Request

> I want the new game button to be a multi button where you can click the dropdown part of the button and get options to play a 5x5, 6x6 board etc up to 10x10 . And also include an option for custom input but boards must always be same number wide and tall.
>
> Make sure all docs and specs are added/updated.
>
> Doing larger boards should really put the code performance/memory management to the test.
>
> Do lots of testing and make sure all sizing is playable and still fast and snappy. No choppiness
>
> Don't do 1x1 tho or 0 or neg

Follow-up answer, verbatim:

> Default size is 4x4

- **Source**: Message from Garrett, relayed to the implementing assistant (request and follow-up answer)
- **Date**: 2026-10-08

The relayed task added these requirements (condensed): a split button (main part = new game at the
current size, caret = menu of 4x4 (default/classic) through 10x10 plus "Custom…"); an accessible
menu (keyboard, ARIA, Escape and click-outside to close, focus management) in pure Blazor; custom N
must be an integer ≥ 2 with a clear inline message otherwise, and a measured maximum; an NxN
engine with allocation-conscious data structures; a best score per size; the last size remembered;
the existing 4x4 best score migrated; a board that fits every viewport with readable tiles; hard
performance and memory testing with `tools/PerfTrace`; and tests at every level.

## User Scenarios & Testing

### User Story 1 - Pick a bigger board (Priority: P1) 🎯 MVP

Garrett opens the New Game menu and starts a 5x5 to 10x10 game.

**Independent Test**: Open the caret menu, choose 8x8, and play a few moves.

**Acceptance Scenarios**:

1. **Given** the game, **When** the caret part of the New Game button is pressed, **Then** a menu lists 4x4 (classic), 5x5, 6x6, 7x7, 8x8, 9x9, 10x10 and Custom…, with the current size checked.
2. **Given** the menu, **When** 8x8 is chosen, **Then** a new 8x8 game starts with two tiles, the menu closes and focus returns to the board.
3. **Given** an 8x8 game, **When** the main part of the button is pressed, **Then** a new 8x8 game starts (the current size is kept).
4. **Given** a first visit with nothing saved, **When** the game loads, **Then** the board is 4x4.

### User Story 2 - Custom square size (Priority: P1)

**Independent Test**: Choose Custom…, enter 12, start; enter 1, see the error.

**Acceptance Scenarios**:

1. **Given** the Custom… dialog, **When** a whole number from 2 to the maximum is entered, **Then** a new N×N game starts.
2. **Given** the dialog, **When** `1`, `0`, `-3`, `abc`, `7.5` or an empty value is entered, **Then** no game starts and an inline message explains the valid range; the input is marked invalid.
3. **Given** the dialog, **When** a number above the maximum is entered, **Then** the message names the maximum.
4. **Given** the dialog, **When** Escape or Cancel is pressed, **Then** it closes without changing the game.
5. **Given** the dialog, **When** digits or the letters w/a/s/d are typed, **Then** the board does not move.

### User Story 3 - Keyboard and screen reader friendly menu (Priority: P1)

**Acceptance Scenarios**:

1. **Given** focus on the caret, **When** Enter, Space or ArrowDown is pressed, **Then** the menu opens and focus moves to the checked item.
2. **Given** the open menu, **When** ArrowDown/ArrowUp/Home/End are pressed, **Then** focus moves between items (wrapping), and the board does not move.
3. **Given** the open menu, **When** Escape is pressed, **Then** it closes and focus returns to the caret; **When** the user clicks outside or presses Tab, **Then** it closes.
4. **Given** a screen reader, **When** it reads the caret, **Then** it announces a menu button with its expanded state; items are menu item radios with their checked state.

### User Story 4 - Sizes and scores are remembered (Priority: P2)

**Acceptance Scenarios**:

1. **Given** a 6x6 game was chosen, **When** the page is reloaded, **Then** a new game starts at 6x6.
2. **Given** best scores on 4x4 and 6x6, **When** switching sizes, **Then** each size shows its own best.
3. **Given** a player with a 4x4 best saved before this feature, **When** the new version loads, **Then** the 4x4 best is still shown.

### User Story 5 - Every size is readable and snappy (Priority: P1)

**Acceptance Scenarios**:

1. **Given** any size on a phone or desktop, **When** the game is shown, **Then** the whole board fits the viewport without scrolling, and swipe and keyboard moves work.
2. **Given** a 10x10 board on a phone, **When** a tile is 131072 or larger, **Then** its label stays readable.
3. **Given** a 10x10 board, **When** keys are pressed rapidly, **Then** every key is applied and animations do not snap or teleport.
4. **Given** 500+ moves and 50 new games, **When** memory is sampled, **Then** it does not keep growing.

### Edge Cases

- A board too small to reach 2048 (2x2, 3x3): the win tile scales down so a win is possible.
- Changing size during a game simply starts a new game; the old game is not saved.
- Storage unavailable (private mode): sizes and bests still work for the session.
- A saved size outside the valid range (edited by hand): fall back to 4x4.

## Requirements

### Functional Requirements

- **FR-001**: The New Game control MUST be a split button: main part = new game at the current size; caret = menu with 4x4 (classic), 5x5 … 10x10 and Custom….
- **FR-002**: The menu MUST use `aria-haspopup="menu"`, `aria-expanded`, `role="menu"` and `role="menuitemradio"` with `aria-checked`; support ArrowUp/ArrowDown/Home/End, Enter/Space, Escape (focus back to the caret), Tab and click-outside to close.
- **FR-003**: Custom… MUST accept a single N (boards are always N×N). Valid N: whole numbers from 2 to `Game.MaxSize`. Anything else MUST be rejected with an inline message (`role="alert"`, `aria-invalid`).
- **FR-004**: `Game.MaxSize` MUST be chosen from measurements (readability on a phone, and PerfTrace timings), recorded in [plan.md](plan.md).
- **FR-005**: The default size MUST be 4x4 when nothing is saved.
- **FR-006**: The engine MUST support every N from 2 to `MaxSize` (moves, merges, spawn, game over, win), using flat arrays and reusable buffers: no LINQ or per-line allocations on the move path.
- **FR-007**: The win tile MUST be 2048, or 2^(N²) on boards too small to reach it (2x2 → 16, 3x3 → 512).
- **FR-008**: Tiles MUST keep `@key` identity and the existing animation model at every size.
- **FR-009**: The best score MUST be stored per size; the last chosen size MUST be remembered; the existing 4x4 best (`blazor2048.best`) MUST carry over.
- **FR-010**: The board MUST scale to the viewport at every size; gaps and tile fonts MUST scale with cell size and label length, and large values on small cells MUST use a compact label (for example `128K`) with the full value kept in `data-value` and the accessible name.
- **FR-011**: No custom JavaScript; all of this is Blazor/C#/CSS.
- **FR-012**: Nothing may grow without bound across moves or new games (tile class caches, retired tiles, event handlers).

## Success Criteria

- **SC-001**: At every size up to `MaxSize`, median key-to-DOM time and Blazor time per move under 4x CPU throttling stay within 1.5x of 4x4, with no snaps or teleports during rapid input.
- **SC-002**: Engine moves allocate no more than a small constant per move, independent of N (measured by a unit test).
- **SC-003**: Over 500+ moves and 50 new games, JS heap and WASM memory reach a plateau (no steady growth).
- **SC-004**: All suites green: engine tests for every size 2…MaxSize, bUnit tests for the split button, menu, dialog and validation, and Playwright tests for presets, custom, invalid input, persistence and 10x10 rapid input.
- **SC-005**: Deployed and verified on the live site, including a phone viewport.

## Assumptions

- Win and "Keep going" behave the same at every size.
- Larger boards keep the 2 (90%) / 4 (10%) spawn rule and spawn one tile per move.
