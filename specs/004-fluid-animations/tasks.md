# Tasks: Fluid tile animations

**Input**: [spec.md](spec.md), [plan.md](plan.md)

## Phase 1: First version

- [x] T001 Stable tile ids, retired merge sources and `RenderTiles` in the engine — [`06622ea`](https://github.com/gkizior/blazor-2048/commit/06622ea)
- [x] T002 [P] `TileTrackingTests` — [`06622ea`](https://github.com/gkizior/blazor-2048/commit/06622ea)
- [x] T003 Keyed tile layer, transform slides, spawn/pop keyframes, reduced motion — [`c50f22a`](https://github.com/gkizior/blazor-2048/commit/c50f22a)
- [x] T004 [P] E2E: same element slides; rapid keys not dropped; reduced motion — [`c50f22a`](https://github.com/gkizior/blazor-2048/commit/c50f22a)

## Phase 2: Revision: "feels choppy"

- [x] T005 `tools/PerfTrace`: CDP trace + per-frame tile sampling; before runs, local and live — [`6015021`](https://github.com/gkizior/blazor-2048/commit/6015021)
- [x] T006 [US1] Spawn/pop start at opacity 0 and wait for the full slide; `--slide/--spawn/--pop` tokens — [`6015021`](https://github.com/gkizior/blazor-2048/commit/6015021)
- [x] T007 [US2] Animation class fixed for a tile's life (`ClassFor` cache) — [`6015021`](https://github.com/gkizior/blazor-2048/commit/6015021)
- [x] T008 `ShouldRender`, `BoardCells` rendered once, cached position styles — [`6015021`](https://github.com/gkizior/blazor-2048/commit/6015021)
- [x] T009 [P] bUnit: merge sources keep their elements, fixed classes, render counts — [`6015021`](https://github.com/gkizior/blazor-2048/commit/6015021)
- [x] T010 [P] Unit: only transform/opacity animate; keyframes start invisible — [`6015021`](https://github.com/gkizior/blazor-2048/commit/6015021)
- [x] T011 [P] E2E `AnimationE2ETests`: no pops before arrival, no snaps or teleports on rapid input — [`6015021`](https://github.com/gkizior/blazor-2048/commit/6015021)
- [x] T012 Experiments measured and rejected: containment, `will-change`, `@property`, AOT — [`6015021`](https://github.com/gkizior/blazor-2048/commit/6015021) (documented)
- [x] T013 After runs; docs updated; deployed and verified live — [`6015021`](https://github.com/gkizior/blazor-2048/commit/6015021)

## Phase 3: Flaky "rapid keys not dropped" E2E (CI run on `d067410`)

`Rapid_Key_Presses_During_Animations_Are_Not_Dropped` failed once in CI (expected `data-moves` 4, got 3) and passed on rerun.
Root cause: the **test**, not the app. It always pressed ArrowLeft first and expected one move per key. On a fresh board where both
start tiles already sit on the left edge (about 1 board in 20), ArrowLeft correctly does nothing, so only 3 of 4 keys move.
No input was dropped: in every failing run all 4 keydowns reached the focused board, and the first key was the no-op.

| Repro | Runs | Failures | Of those, first ArrowLeft a no-op |
| --- | --- | --- | --- |
| Before: Node harness replaying the test, headless | 200 | 10 (5.0%) | 10 |
| Before: same, 4x CPU throttling | ~165 (run cut short) | 9 (5.5%) | 9 |
| Before: the real .NET test, a fresh process per run, CI-like headless | 150 | 5 (3.3%) | — |
| After: the real .NET test, headless | 150 | 0 | — |
| After: the real .NET test, `E2E_CPU_THROTTLE=4` | 150 | 0 | — |

- [x] T014 [US2] E2E `InputLedger`: record the board after every move and replay the keys against an independent slide oracle, so every key that can move must produce exactly the next board (in order) and keys that can't must change nothing; used by the rapid-key, rapid-retarget and 10x10 rapid tests (the first two had loose "1..N" or "≥ N−1" checks); the rapid-key test starts with a direction that moves its board and presses 8 keys back to back. A build with an "animating" guard (keys ignored for 110 ms) fails all three with "Key N could move the board but was dropped" — [`ca68e40`](https://github.com/gkizior/blazor-2048/commit/ca68e40)
- [x] T015 [P] bUnit: rapid keys in lockstep with a twin `Game` on the same seed (board, score and `data-moves` after every key; an ignored key is not counted); the same with every best-score save left pending, which a "busy while saving" guard fails — [`ca68e40`](https://github.com/gkizior/blazor-2048/commit/ca68e40)
- [x] T016 `E2E_CPU_THROTTLE` option for E2E runs; docs (theming: input order; testing: the ledger); spec US2 scenario 3 says what "not dropped" means for keys that cannot move. No app change: the move is synchronous before the only `await`, there is no busy or animating guard, and `ShouldRender` only skips renders — [`ca68e40`](https://github.com/gkizior/blazor-2048/commit/ca68e40); CI [run 38166579544](https://github.com/gkizior/blazor-2048/actions/runs/38166579544) passed and deployed; live: 30 fresh pages × 8 back-to-back keys, 240 of 240 movable keys applied in order
