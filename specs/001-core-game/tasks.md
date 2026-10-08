# Tasks: Core game

**Input**: [spec.md](spec.md), [plan.md](plan.md)

## Phase 1: Setup

- [x] T001 Solution with `src/Game2048.Core`, `src/Blazor2048` and three test projects — [`93c5cee`](https://github.com/gkizior/blazor-2048/commit/93c5cee)

## Phase 2: Engine (US1)

- [x] T002 [US1] Slide/merge rules, spawn, score, win and game over in `src/Game2048.Core/Game.cs` — [`93c5cee`](https://github.com/gkizior/blazor-2048/commit/93c5cee)
- [x] T003 [P] [US1] Engine unit tests in `tests/Blazor2048.Tests/GameTests.cs` — [`93c5cee`](https://github.com/gkizior/blazor-2048/commit/93c5cee)

## Phase 3: UI and input (US1, US2, US3)

- [x] T004 [US1] `GameBoard.razor`: board, scores, overlays, arrow keys/WASD — [`93c5cee`](https://github.com/gkizior/blazor-2048/commit/93c5cee)
- [x] T005 [US2] Swipe handling (24 px threshold) and no page scroll while playing — [`93c5cee`](https://github.com/gkizior/blazor-2048/commit/93c5cee)
- [x] T006 [US3] Best score in `localStorage` via `BestScoreStore` — [`93c5cee`](https://github.com/gkizior/blazor-2048/commit/93c5cee)
- [x] T007 [P] bUnit tests in `tests/Blazor2048.ComponentTests/GameBoardTests.cs` — [`93c5cee`](https://github.com/gkizior/blazor-2048/commit/93c5cee)

## Phase 4: PWA and deploy (US4)

- [x] T008 [US4] Manifest, icons and offline service worker — [`93c5cee`](https://github.com/gkizior/blazor-2048/commit/93c5cee)
- [x] T009 GitHub Pages workflow and `scripts/prepare-pages.py` — [`93c5cee`](https://github.com/gkizior/blazor-2048/commit/93c5cee)
- [x] T010 [P] Playwright E2E tests in `tests/Blazor2048.E2ETests` — [`93c5cee`](https://github.com/gkizior/blazor-2048/commit/93c5cee)

## Phase 5: Polish

- [x] T011 Retarget every project to .NET 10 — [`bc0de4d`](https://github.com/gkizior/blazor-2048/commit/bc0de4d)
