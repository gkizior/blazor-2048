# Tasks: In-app DocFX-style docs with Mermaid

**Input**: [spec.md](spec.md), [plan.md](plan.md)

- [x] T001 `tools/DocsBuilder`: find every `.md`, order by `toc.yml`, render with Markdig, rewrite links — [`28134ea`](https://github.com/gkizior/blazor-2048/commit/28134ea)
- [x] T002 MSBuild `BuildDocs` target; output shipped as static web assets — [`28134ea`](https://github.com/gkizior/blazor-2048/commit/28134ea)
- [x] T003 Mermaid pre-rendering (`render-diagrams`), light/dark SVGs named by hash — [`28134ea`](https://github.com/gkizior/blazor-2048/commit/28134ea)
- [x] T004 [US1] `Docs.razor`: sidebar with filter, content, breadcrumbs, pager, outline, source link — [`28134ea`](https://github.com/gkizior/blazor-2048/commit/28134ea)
- [x] T005 [US1] Docs button in the game header; game state survives the trip (scoped `Game`) — [`28134ea`](https://github.com/gkizior/blazor-2048/commit/28134ea)
- [x] T006 [US3] Service worker caches `.svg` — [`28134ea`](https://github.com/gkizior/blazor-2048/commit/28134ea)
- [x] T007 Seven docs pages with 11 diagrams in `/docs` — [`28134ea`](https://github.com/gkizior/blazor-2048/commit/28134ea)
- [x] T008 [P] `DocsBuilderTests`, `DocsPageTests`, docs E2E tests — [`28134ea`](https://github.com/gkizior/blazor-2048/commit/28134ea)
- [x] T009 Specs section in the sidebar (nested items) — see [010](../010-spec-driven-development/tasks.md)
