# Tasks: Board sizes (4x4 to NxN)

**Input**: [spec.md](spec.md), [plan.md](plan.md)

## Phase 1: Spec

- [x] T001 Spec, plan and tasks for 011; constitution 1.1.0 (principle IX) — [`932c156`](https://github.com/gkizior/blazor-2048/commit/932c156)
- [x] T001a Spec update: the name follows the target, 2^(N+7) (second follow-up) — [`18252c3`](https://github.com/gkizior/blazor-2048/commit/18252c3)

## Phase 2: Engine (US1, US2, US5)

- [x] T002 Engine supports N×N (2…MaxSize): flat arrays, reusable line buffers, scaled win tile — [`1ddc9a9`](https://github.com/gkizior/blazor-2048/commit/1ddc9a9)
- [x] T003 [P] Engine tests for every size: moves, merges, game over, win, random play invariants, allocations — [`1ddc9a9`](https://github.com/gkizior/blazor-2048/commit/1ddc9a9)

## Phase 3: UI (US1–US4)

- [x] T004 Split New Game button with menu and Custom… dialog; validation — [`7a0e097`](https://github.com/gkizior/blazor-2048/commit/7a0e097)
- [x] T005 Per-size best score, last size, 4x4 migration — [`7a0e097`](https://github.com/gkizior/blazor-2048/commit/7a0e097)
- [x] T006 Board, gaps and tile labels scale with N; compact labels for large values — [`7a0e097`](https://github.com/gkizior/blazor-2048/commit/7a0e097)
- [x] T006a The name easter egg: title, tab title, hint, win message and accessible name show 2^(N+7) — [`7a0e097`](https://github.com/gkizior/blazor-2048/commit/7a0e097)
- [x] T007 [P] bUnit tests: split button, menu keyboard/ARIA, dialog, validation, persistence — [`7a0e097`](https://github.com/gkizior/blazor-2048/commit/7a0e097)
- [x] T008 [P] Playwright tests: presets, custom, invalid input, persistence, 10x10 rapid input — [`01e23b7`](https://github.com/gkizior/blazor-2048/commit/01e23b7)

## Phase 4: Performance (US5)

- [x] T009 PerfTrace: board size and memory modes — [`e9cbab9`](https://github.com/gkizior/blazor-2048/commit/e9cbab9)
- [x] T010 Measure before optimizing (4, 6, 8, 10, candidate maximums) — [`e9cbab9`](https://github.com/gkizior/blazor-2048/commit/e9cbab9)
- [x] T011 Optimize render and engine paths; measure after; choose MaxSize — [`1ddc9a9`](https://github.com/gkizior/blazor-2048/commit/1ddc9a9), [`7a0e097`](https://github.com/gkizior/blazor-2048/commit/7a0e097)

## Phase 5: Polish

- [x] T012 Docs: architecture, game engine, components, theming/animations, testing, diagrams — [`7630ee7`](https://github.com/gkizior/blazor-2048/commit/7630ee7)
- [x] T013 All suites green; deploy verified live, including a phone viewport — CI [run 37799672887](https://github.com/gkizior/blazor-2048/actions/runs/37799672887), live check of `7630ee7`

## Phase 6: Secret tiny boards (third follow-up)

- [x] T014 Spec update: 1x1 and 0x0 win right away; history of the lower bound — [`7e381b0`](https://github.com/gkizior/blazor-2048/commit/7e381b0)
- [x] T015 Engine: sizes 0 and 1 (`BoardSize.IsSecret`), instant win, zero-cell guards, target 2^(N+7) from N = 0 — [`56a90c4`](https://github.com/gkizior/blazor-2048/commit/56a90c4)
- [x] T016 UI: Custom… accepts 0 and 1; tiny-board win screens; not saved as last size, no best; empty board layout — [`e431a63`](https://github.com/gkizior/blazor-2048/commit/e431a63)
- [x] T017 [P] Tests: xUnit engine and validation, bUnit dialog and win screens, Playwright 0, 1, -1 and 1.5 — [`56a90c4`](https://github.com/gkizior/blazor-2048/commit/56a90c4), [`e431a63`](https://github.com/gkizior/blazor-2048/commit/e431a63), [`9a7c26f`](https://github.com/gkizior/blazor-2048/commit/9a7c26f)
- [x] T018 Docs: easter egg section, BoardSize table — [`381018b`](https://github.com/gkizior/blazor-2048/commit/381018b)

## Phase 7: Only 1x1 (fourth follow-up)

- [x] T020 Spec update: "Maybe not zero still but 1 should just win immediately"; 0x0 dropped — [`ed07c00`](https://github.com/gkizior/blazor-2048/commit/ed07c00)
- [x] T021 Engine and UI: 1 is the only secret size; 0 rejected again; 0x0 code removed — [`9688326`](https://github.com/gkizior/blazor-2048/commit/9688326)
- [x] T022 [P] Tests: 0 and -1 rejected, 1 an instant win (xUnit, bUnit, Playwright) — [`9688326`](https://github.com/gkizior/blazor-2048/commit/9688326)
- [ ] T023 Docs updated; deploy verified live; 1x1 screenshot
