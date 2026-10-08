# Game engine

All rules live in `src/Game2048.Core/Game.cs` and `BoardSize.cs`, plain C# with no UI dependencies,
so they can be unit tested directly and reused by any front end.

## Board sizes

Boards are always square, N×N ([spec 011](../specs/011-board-sizes/spec.md)). `BoardSize` holds the
rules:

| Member | Value |
|---|---|
| `Min` / `Max` | 2 and 16 (1×1 cannot move; 16 is the measured limit, see below) |
| `Default` | 4 (the classic board, used when nothing is saved) |
| `Presets` | 4…10, the sizes in the New Game menu |
| `TryParse(text, out size, out error)` | Validates the Custom… input: whole numbers only, with a message for empty, too small (1, 0, negatives), too big, decimals and non-numbers |
| `WinningTile(n)` | The target, 2^(N+7): 512 (2×2), 1024 (3×3), **2048 (4×4)**, 4096 … 131072 (10×10) … 8388608 (16×16) |

The target is also the game's "name": the UI shows it wherever the classic game says 2048 (the easter
egg). 2^(N+7) fits an `int` up to N = 23, so `WinningTile` cannot overflow at `Max`. A 2×2 board
can never reach its 512 (its largest possible tile is 32) and 1024 is the largest tile a 3×3 board can
hold, which is part of the joke.

Why 16: the tile labels on a 390 px wide phone are the limit, not speed. Cells are about 26 px at
12×12, 19.5 px at 16×16 and 15.6 px at 20×20; with compact labels (`16K`) 16×16 is still readable,
20×20 is not. Performance at 16×16 after the optimizations below is within the spec's budget of 4×4
(numbers in the [plan](../specs/011-board-sizes/plan.md)).

## State

| Member | Meaning |
|---|---|
| `Size` | N for an N×N board. `NewGame(n)` changes it. |
| `this[row, col]` | The value at a cell, `0` for empty. |
| `Board` | A copy of the board as `int[N, N]` (for tests and tools; it allocates). |
| `Score` | Sum of every merged tile's value. |
| `WinningTile` | The target for this size (see above). |
| `HasWon` / `KeepPlaying` | Reached the target / chose "Keep going". |
| `IsGameOver` | No empty cells and no equal neighbours. |
| `Tiles` | Live tiles with stable ids, ordered by id (a new list; for tests). |
| `RetiredTiles` | Tiles merged away by the last move, positioned on their merge target. |
| `RenderTiles` | `RetiredTiles` + live tiles, ordered by id. This is what the board draws; one reused list. |
| `SpawnedCell` | Where the last random tile went. |

## Memory layout

Big boards made the engine's data structures matter, so a move allocates nothing:

- Values, tile ids and per-cell flags (new / merged) are **flat arrays** indexed `row * N + col`.
- A move reads each line into **reusable line buffers** (`_lineIndex`, `_lineValues`, …, length N),
  runs `SlideLine` on spans, and writes into a **second set of arrays** that is swapped in when the
  board changed. Nothing is copied back.
- Retired tiles and the render list are two `List<Tile>` that are cleared and refilled; `Tile` is a
  `readonly record struct`, so filling them does not allocate. The render list is rebuilt lazily,
  once per change, and sorted in place with a cached comparison.
- `AddRandomTile` counts empty cells and picks the k-th one (no list of empty cells); `CanMove` and the
  win check run over the flat array (no LINQ).
- Buffers are allocated only by `NewGame(n)` when N changes, and capacity grown for a big board is
  trimmed when switching back down.

`AllSizesTests.Moves_And_The_Render_List_Do_Not_Allocate` measures it with
`GC.GetAllocatedBytesForCurrentThread()`: under one byte per move on 4×4, 10×10 and the largest
board. The first, straightforward generalization (`int[,]`, LINQ, per-line arrays and lists) allocated
3.7 KB per move on 4×4, 12.5 KB on 10×10 and 25.6 KB on 16×16, and was 3–5 times slower (a move plus
building the render list, .NET 10 JIT: 10.3 → 1.4 µs on 4×4, 10.1 → 3.0 µs on 16×16).

## A move

Every move is reduced to the same one-dimensional problem: slide one line toward index 0.
`FillLineIndex` lists a row or column's flat indexes in the order tiles travel, so **left, right, up
and down share one implementation**, at any N.

```mermaid
flowchart TD
    accTitle: Move and merge flow
    Start(Move direction) --> Over{Game over?}
    Over -- yes --> Ignore(Ignored)
    Over -- no --> Lines[For each of the N rows/columns:<br/>flat indexes in travel order]
    Lines --> Slide[SlideLine on reusable buffers: compact,<br/>merge equal neighbours once]
    Slide --> Track[Record where each tile went:<br/>keep id on slide, retire both halves on merge]
    Track --> Changed{Board changed?}
    Changed -- no --> NoOp("Return false, state untouched")
    Changed -- yes --> Commit[Swap in the new arrays; score,<br/>fresh ids for merged tiles]
    Commit --> Spawn["Spawn a 2 (90%) or 4 (10%)<br/>in a random empty cell"]
    Spawn --> Check["HasWon if a merge reached 2^(N+7)<br/>IsGameOver = !CanMove()"]
    Check --> Done(Return true)
```

### Merge rules

`SlideLine` (spans, no allocation; used by moves) and `SlideRow` (arrays; used by tests) implement
the classic rules, for lines of any length:

- Tiles slide as far as they can toward the edge.
- Two equal tiles merge into one with double the value, and the score grows by that value.
- **A tile merges at most once per move**, so `[2, 2, 2, 2]` becomes `[4, 4, 0, 0]`, not `[8, 0, 0, 0]`.
- Merges are resolved from the edge the tiles move toward: `[2, 2, 2, 0]` moving left gives `[4, 2, 0, 0]`.

## Tile identity (for animations)

The UI animates by tile **id**, so the engine tracks where every tile goes:

```mermaid
stateDiagram-v2
    accTitle: Tile lifecycle
    [*] --> New: spawned (fresh id)
    New --> Idle: next move
    Idle --> Idle: slides (same id)
    Idle --> Retired: merged into a neighbour
    New --> Retired: merged on the next move
    Retired --> [*]: dropped on the following move
    Idle --> Merged: is the result of a merge (fresh id)
    Merged --> Idle: next move
```

- A tile that slides **keeps its id**, so its DOM element is reused and CSS can transition it.
- A merge **retires both source tiles** (they slide onto the target cell) and creates a **new tile**
  with a new id, so its "pop" animation plays exactly once.
- Ids only increase. Rendering in id order means a keyed list never reorders existing elements,
  which would cancel their transitions.
- A move that changes nothing returns `false` and leaves all state (including animation flags)
  untouched.

## Randomness and testing

The constructor takes an optional `Random`, so tests use a seeded instance and get the same tiles
every run. `SetBoard` loads an exact position for scenario tests. See [Testing strategy](testing.md).
