# Tasks: Testing stack

**Input**: [spec.md](spec.md), [plan.md](plan.md)

- [x] T001 xUnit, bUnit and Playwright test projects — [`93c5cee`](https://github.com/gkizior/blazor-2048/commit/93c5cee)
- [x] T002 Migrate all test projects to xUnit v3 (Exe, ValueTask lifetimes, TestContext cancellation, MTP runner) — [`4b55f38`](https://github.com/gkizior/blazor-2048/commit/4b55f38)
- [x] T003 `Microsoft.Playwright.Xunit.v3`: `SiteServer` assembly fixture + `AppTest` base — [`4b55f38`](https://github.com/gkizior/blazor-2048/commit/4b55f38)
- [x] T004 bUnit confirmed on the latest stable (2.11.3, already current) and working with xUnit v3 — [`4b55f38`](https://github.com/gkizior/blazor-2048/commit/4b55f38)
- [x] T005 CI: E2E runs against the deployed artifact; deploy needs build + e2e — [`75882e5`](https://github.com/gkizior/blazor-2048/commit/75882e5)
- [x] T006 Animation frame-sampling E2E tests and a perf probe (`tools/PerfTrace`) — see [004](../004-fluid-animations/tasks.md)
