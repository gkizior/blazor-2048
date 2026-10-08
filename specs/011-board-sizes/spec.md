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

Second follow-up (the name easter egg), verbatim:

> Changing the size should also update the “name” of the game too. So all the spots that say 2048 should change to the appropriate number . This is kinda quirky Easter egg of sorts

Third follow-up (secret tiny boards, 2026-10-08, after board sizes shipped in `7630ee7`), verbatim:

> It would be funny to allow size of 1 and 0.
>
> Where you just win right away

**History of the lower bound.** The original request said "Don't do 1x1 tho or 0 or neg", so the
first version rejected 1, 0 and negatives. The third follow-up reverses that **for 1 and 0 only**:
they become Custom-only easter eggs that win instantly. Negatives, decimals and text are still
rejected, the playable range stays 2 to 16, and the preset menu does not change.

- **Source**: Message from Garrett, relayed to the implementing assistant (request and three follow-ups)
- **Date**: 2026-10-08

The relayed task added these requirements (condensed): a split button (main part = new game at the
current size, caret = menu of 4x4 (default/classic) through 10x10 plus "Custom…"); an accessible
menu (keyboard, ARIA, Escape and click-outside to close, focus management) in pure Blazor; custom N
must be an integer ≥ 2 with a clear inline message otherwise, and a measured maximum; an NxN
engine with allocation-conscious data structures; a best score per size; the last size remembered;
the existing 4x4 best score migrated; a board that fits every viewport with readable tiles; hard
performance and memory testing with `tools/PerfTrace`; and tests at every level. The second
follow-up was relayed with this target rule: the win tile is 2^(N+7) (4x4 → 2048, 5x5 → 4096,
6x6 → 8192, 7x7 → 16384, 8x8 → 32768, 9x9 → 65536, 10x10 → 131072; custom sizes follow the same
formula, so 2x2 → 512 and 3x3 → 1024), and every visible "2048" in the game UI shows the current
target, while the repo name, PWA manifest and static docs stay "Blazor 2048". The third follow-up
was relayed with: Custom… accepts 0 and 1, and starting either is an immediate, funny and tasteful
win (1x1: a single tile that is already the target, 2^(1+7) = 256; 0x0: an empty board, a name such
as 128 = 2^7, and a message like "You won by not playing."); no crashes or moves at 0 or 1 cells;
no best score for them and no instant win restored on reload; Custom-only (not in the menu).

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

**Independent Test**: Choose Custom…, enter 12, start; enter -1, see the error.

**Acceptance Scenarios**:

1. **Given** the Custom… dialog, **When** a whole number from 2 to the maximum is entered, **Then** a new N×N game starts.
2. **Given** the dialog, **When** `-1`, `-3`, `abc`, `1.5`, `7.5` or an empty value is entered, **Then** no game starts and an inline message explains the valid range; the input is marked invalid. (`1` and `0` were rejected here until the third follow-up; see User Story 7.)
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

### User Story 6 - The game is named after its target (Priority: P2)

The easter egg: the game's "name" is the tile you are playing for, so a 6x6 game is "8192".

**Independent Test**: Pick 6x6 and read the header, the tab title and the hint.

**Acceptance Scenarios**:

1. **Given** a 4x4 game, **When** it loads, **Then** the header, the tab title and the hint say 2048, as before.
2. **Given** the menu, **When** 6x6 is chosen, **Then** the header title, the document title (via `PageTitle`), the hint ("get to 8192!") and the board's accessible name change to 8192 right away.
3. **Given** a 6x6 game, **When** a tile reaches 8192, **Then** the win message names 8192; reaching 2048 there does not win.
4. **Given** a 10x10 game on a phone, **When** the header shows 131072, **Then** the title shrinks to fit next to the scores.
5. **Given** a reload with 6x6 saved, **When** the page loads, **Then** the title says 8192 without first flashing 2048.

### User Story 7 - Secret tiny boards: 1x1 and 0x0 win right away (Priority: P3)

The third follow-up: two sizes nobody would pick on purpose, hidden behind Custom…, that you win
without playing.

**Independent Test**: Choose Custom…, enter 1, start; then enter 0, start.

**Acceptance Scenarios**:

1. **Given** the Custom… dialog, **When** `1` is entered, **Then** a 1x1 board appears holding one tile that is already the target, 256 (2^(1+7)); the name (header, tab title, hint, board label) becomes 256 and a win screen says so right away, in zero moves.
2. **Given** the Custom… dialog, **When** `0` is entered, **Then** an empty board appears, the name becomes 128 (2^(0+7)) and the win screen says "You won by not playing."
3. **Given** either win screen, **When** keys are pressed or the board is swiped, **Then** nothing moves and nothing breaks; the screen offers to play the tiny board again or go back to the last real size.
4. **Given** a 1x1 or 0x0 game, **When** the page is reloaded, **Then** the game starts at the last real size (2 to 16, or 4x4 if none), not at the instant win.
5. **Given** a 1x1 or 0x0 game, **Then** no best score is saved or shown for it, and the real sizes' bests are untouched.
6. **Given** the New Game menu, **Then** it still lists only 4x4 … 10x10 and Custom…; the dialog still describes the range as 2 to 16 (the tiny boards stay a secret).

### Edge Cases

- Small custom boards follow the same formula: 2x2 → 512 and 3x3 → 1024. A 2x2 board can never
  reach 512 (its largest possible tile is 32), and 1024 is the largest tile a 3x3 board can hold, so
  those wins are practically out of reach; that is accepted as part of the joke.
- The target at the maximum custom size must not overflow: 2^(MaxSize+7) must fit in an `int`
  (MaxSize ≤ 23); a unit test guards it.
- Before the app boots, the static loading screen and `index.html` title say 2048 (the size is not
  known without running code); they switch as soon as the game renders.
- Changing size during a game simply starts a new game; the old game is not saved.
- Storage unavailable (private mode): sizes and bests still work for the session.
- A saved size outside the valid range (edited by hand), including 0 or 1: fall back to 4x4.
- 0x0 has no cells: the engine must not divide by N or index an empty array (spawn, moves, game over
  and the render list all handle zero cells), and the CSS sizing must not divide by zero (the empty
  board takes the size of a 1x1 board).
- `-0` is not a size anyone means: like any input with a minus sign it is rejected, so only a plain
  `0` opens the empty board.

## Requirements

### Functional Requirements

- **FR-001**: The New Game control MUST be a split button: main part = new game at the current size; caret = menu with 4x4 (classic), 5x5 … 10x10 and Custom….
- **FR-002**: The menu MUST use `aria-haspopup="menu"`, `aria-expanded`, `role="menu"` and `role="menuitemradio"` with `aria-checked`; support ArrowUp/ArrowDown/Home/End, Enter/Space, Escape (focus back to the caret), Tab and click-outside to close.
- **FR-003**: Custom… MUST accept a single N (boards are always N×N). Valid N: whole numbers from 2 to `BoardSize.Max`, plus the secret sizes 0 and 1 (FR-015). Anything else (negatives, decimals, text, empty, above the maximum) MUST be rejected with an inline message (`role="alert"`, `aria-invalid`).
- **FR-004**: `BoardSize.Max` MUST be chosen from measurements (readability on a phone, and PerfTrace timings), recorded in [plan.md](plan.md).
- **FR-005**: The default size MUST be 4x4 when nothing is saved.
- **FR-006**: The engine MUST support every N from 2 to `MaxSize` (moves, merges, spawn, game over, win), using flat arrays and reusable buffers: no LINQ or per-line allocations on the move path.
- **FR-007**: The win tile (the target) MUST be 2^(N+7): 2048 at 4x4, 4096 at 5x5 … 131072 at 10x10; 512 at 2x2 and 1024 at 3x3.
- **FR-008**: Tiles MUST keep `@key` identity and the existing animation model at every size.
- **FR-009**: The best score MUST be stored per size; the last chosen size MUST be remembered; the existing 4x4 best (`blazor2048.best`) MUST carry over.
- **FR-010**: The board MUST scale to the viewport at every size; gaps and tile fonts MUST scale with cell size and label length, and large values on small cells MUST use a compact label (for example `128K`) with the full value kept in `data-value` and the `title`.
- **FR-011**: No custom JavaScript; all of this is Blazor/C#/CSS.
- **FR-012**: Nothing may grow without bound across moves or new games (tile class caches, retired tiles, event handlers).
- **FR-013**: Every visible "2048" in the game UI (header title, document title via `PageTitle`, hint, win message, board accessible name) MUST show the current target and update when the size changes. The repository name, PWA manifest, in-app and static docs keep the name "Blazor 2048"/"2048 Docs"; the easter egg is documented in the docs.
- **FR-014**: A subtle touch is allowed if tasteful: the title number flips in when the size changes (no motion with `prefers-reduced-motion`).
- **FR-015**: Sizes 0 and 1 (`BoardSize.IsSecret`) MUST start an instant win: 1x1 holds one tile equal to its target (256), 0x0 is empty (target 128); `HasWon` is set at once, no tile spawns, no move succeeds. The win screen MUST have its own tasteful wording and offer "Play again" and a way back to the last real size, without "Keep going".
- **FR-016**: Secret sizes MUST NOT be saved as the last size or get a best score; a reload starts the last real size. They MUST NOT appear in the preset menu, and the dialog's range text stays "2 to 16".

## Success Criteria

- **SC-001**: At every size up to `BoardSize.Max`, median key-to-DOM time and Blazor time per move under 4x CPU throttling stay within 1.5x of 4x4, with no snaps or teleports during rapid input.
- **SC-002**: Engine moves allocate no more than a small constant per move, independent of N (measured by a unit test).
- **SC-003**: Over 500+ moves and 50 new games, JS heap and WASM memory reach a plateau (no steady growth).
- **SC-004**: All suites green: engine tests for every size 2…MaxSize, bUnit tests for the split button, menu, dialog and validation, and Playwright tests for presets, custom, invalid input, persistence and 10x10 rapid input.
- **SC-005**: Deployed and verified on the live site, including a phone viewport.
- **SC-006**: bUnit and Playwright tests show the name following the size (title, tab title, hint, win message) and persisting across a reload.
- **SC-007**: xUnit, bUnit and Playwright tests cover 0 and 1 (instant win, name, no moves, no crash, not persisted, not in the menu) and confirm `-1` and `1.5` are still rejected.

## Assumptions

- Win and "Keep going" behave the same at every size; only the target changes.
- Larger boards keep the 2 (90%) / 4 (10%) spawn rule and spawn one tile per move.
