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
