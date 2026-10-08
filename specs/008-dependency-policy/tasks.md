# Tasks: Dependency policy and Dependabot

**Input**: [spec.md](spec.md), [plan.md](plan.md)

- [x] T001 `.github/dependabot.yml`: nuget, github-actions, dotnet-sdk; Monday 08:00 America/Chicago; minor+patch groups — [`75882e5`](https://github.com/gkizior/blazor-2048/commit/75882e5)
- [x] T002 Bump every Action to its latest major; SDK 10.0.401 in `global.json` — [`75882e5`](https://github.com/gkizior/blazor-2048/commit/75882e5)
- [x] T003 Every NuGet package on the latest stable; licenses checked (none held back) — [`bc0de4d`](https://github.com/gkizior/blazor-2048/commit/bc0de4d), [`4b55f38`](https://github.com/gkizior/blazor-2048/commit/4b55f38), [`28134ea`](https://github.com/gkizior/blazor-2048/commit/28134ea)
- [x] T004 Confirm PRs run tests and never deploy (`deploy` job skipped for `pull_request`) — [`75882e5`](https://github.com/gkizior/blazor-2048/commit/75882e5)
- [x] T005 First Dependabot run after setup: no PRs needed (everything current)
