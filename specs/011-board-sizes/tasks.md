# Tasks: Board sizes (4x4 to NxN)

**Input**: [spec.md](spec.md), [plan.md](plan.md)

## Phase 1: Spec

- [x] T001 Spec, plan and tasks for 011; constitution 1.1.0 (principle IX) — (spec commit)
- [x] T001a Spec update: the name follows the target, 2^(N+7) (second follow-up) — (spec update commit)

## Phase 2: Engine (US1, US2, US5)

- [ ] T002 Engine supports N×N (2…MaxSize): flat arrays, reusable line buffers, scaled win tile
- [ ] T003 [P] Engine tests for every size: moves, merges, game over, win, random play invariants, allocations

## Phase 3: UI (US1–US4)

- [ ] T004 Split New Game button with menu and Custom… dialog; validation
- [ ] T005 Per-size best score, last size, 4x4 migration
- [ ] T006 Board, gaps and tile labels scale with N; compact labels for large values
- [ ] T006a The name easter egg: title, tab title, hint, win message and accessible name show 2^(N+7)
- [ ] T007 [P] bUnit tests: split button, menu keyboard/ARIA, dialog, validation, persistence
- [ ] T008 [P] Playwright tests: presets, custom, invalid input, persistence, 10x10 rapid input

## Phase 4: Performance (US5)

- [ ] T009 PerfTrace: board size and memory modes
- [ ] T010 Measure before optimizing (4, 6, 8, 10, candidate maximums)
- [ ] T011 Optimize render and engine paths; measure after; choose MaxSize

## Phase 5: Polish

- [ ] T012 Docs: architecture, game engine, components, theming/animations, testing, diagrams
- [ ] T013 All suites green; deploy verified live, including a phone viewport
