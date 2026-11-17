# Tasks: Upgrade to .NET 11

**Input**: [spec.md](spec.md), [plan.md](plan.md)

- [x] T001 Spec, plan and tasks
- [x] T002 Tick spec 014 T006 (PR #7 / `49f518f`, full suite and deploy confirmed)
- [x] T003 Pin SDK 11.0.100 in `global.json`; retarget `Directory.Build.props` to `net11.0`
- [x] T004 Bump WebAssembly + DevServer to 11.0.0; confirm other packages are already latest stable; verify licenses
- [x] T005 Update the workflow (`11.0.x`, Playwright `net11.0` path) and Dependabot comments (ignore .NET 12 majors)
- [x] T006 Amend constitution principle II to .NET 11 (v1.2.0); update the plan template
- [x] T007 Sweep README, docs, meta, social preview, and every spec that states the current target or versions; add 015 to the docs sidebar and spec index
- [x] T008 Update footer / E2E tests and comments that assert ".NET 10"
- [x] T009 Full suite with `RUN_E2E=1`; compare publish file lists; 10x10 PerfTrace sanity check
- [ ] T010 Open the PR, squash-merge after CI, confirm the live footer shows .NET 11 and the merge SHA; update the repo description and topics
