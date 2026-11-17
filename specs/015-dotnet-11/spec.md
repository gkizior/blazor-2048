# Feature Specification: Upgrade to .NET 11

**Feature**: `015-dotnet-11` | **Created**: 2026-11-17 | **Status**: Implemented (written before the changes)

## Original Request

> Yes, upgrade blazor-2048 to .NET 11

- **Source**: Message from Garrett, answering the question "Move blazor-2048 to .NET 11?"
- **Date**: 2026-11-17

Dependabot had opened [PR #9](https://github.com/gkizior/blazor-2048/pull/9) (SDK 11.0.100) and
[PR #11](https://github.com/gkizior/blazor-2048/pull/11) (DevServer 11.0.0) after .NET 11 became
generally available on 2026-11-10. Both were closed and major bumps were ignored so the move could
be Garrett's decision, as promised when 11 was still an RC (spec 013).

This PR also ticks [014](../014-action-tags-and-clean-csproj/tasks.md) T006: the full suite and
live deploy were confirmed after PR #7 merged as
[`49f518f`](https://github.com/gkizior/blazor-2048/commit/49f518f).

## User Scenarios & Testing

### User Story 1 - The app runs on .NET 11 (Priority: P1)

Every project targets `net11.0`, the SDK is the latest stable 11.0 release, and the Microsoft
Blazor WebAssembly packages match. The published game still works: base href, service worker,
404 fallback, docs viewer, localStorage, and animations.

**Why this priority**: This is the requested upgrade.

**Independent Test**: `dotnet --version` from the repo is 11.0.x; `dotnet list package` shows
11.0.x for the WebAssembly packages; the footer on the published site starts with ".NET 11".

**Acceptance Scenarios**:

1. **Given** the repo, **When** `global.json` and `Directory.Build.props` are read, **Then** the SDK is the latest stable 11.0.x (`rollForward: latestFeature`, Microsoft.Testing.Platform runner kept) and every project targets `net11.0`.
2. **Given** a Release publish, **When** the site is served, **Then** the footer shows ".NET 11", the base href is `/blazor-2048/`, the service worker hash is valid, `404.html` and `.nojekyll` are present, docs open, scores and theme persist, and animations still run.
3. **Given** the full suite with `RUN_E2E=1`, **When** it runs, **Then** every test passes (at least as many as before the upgrade).

### User Story 2 - Docs, specs and repo metadata match the new target (Priority: P1)

**Acceptance Scenarios**:

1. **Given** the README, constitution, docs and specs, **When** they are read, **Then** current-state claims say .NET 11 / `net11.0` / the current 11.0.x versions, and historical quotes or measurements stay labeled as history.
2. **Given** the GitHub repo page, **When** it is viewed after `gh repo edit`, **Then** the description and topics say .NET 11 (not .NET 10).
3. **Given** the social preview source, **When** it is opened, **Then** it says ".NET 11", and the committed `docs/images/social-preview.png` matches.

### User Story 3 - The next major stays a decision (Priority: P2)

**Acceptance Scenarios**:

1. **Given** `.github/dependabot.yml`, **When** a .NET 12 SDK or Microsoft.AspNetCore.* / Microsoft.Extensions.* major is released, **Then** Dependabot does not open an automatic major-bump PR; the upgrade comes to Garrett as a decision, the same way .NET 11 did.

### Edge Cases

- Prerelease 11.x or 12.x packages are never used.
- Package licenses stay free to use; a license change holds the bump back.
- GitHub Actions stay on plain `@vN` tags.
- Project files stay simple and clean (spec 014).

## Requirements

### Functional Requirements

- **FR-001**: `global.json` MUST pin the latest stable 11.0.x SDK, keep `rollForward: latestFeature`, and keep the Microsoft.Testing.Platform test runner.
- **FR-002**: `Directory.Build.props` MUST set `TargetFramework` to `net11.0` for every project. No file may hardcode `net10.0` as the current target (Playwright install paths, workflow comments, and docs included).
- **FR-003**: `Microsoft.AspNetCore.Components.WebAssembly` and `.DevServer` MUST be the latest stable 11.0.x. Every other package MUST stay on its latest stable version compatible with `net11.0`, with a free-to-use license.
- **FR-004**: CI MUST install a 11.0.x SDK (from `global.json` or `dotnet-version: 11.0.x`). Actions stay on plain major-version tags.
- **FR-005**: `.github/dependabot.yml` MUST drop the "stay on .NET 10" comments and instead ignore `semver-major` updates of the `dotnet-sdk` ecosystem and of `Microsoft.AspNetCore.*` / `Microsoft.Extensions.*`, so a .NET 12 major is held for a decision.
- **FR-006**: The constitution's principle II MUST be amended from .NET 10 to .NET 11 (governance: version bump and a note in the affected specs).
- **FR-007**: README, docs (build-and-deploy, docs-system, architecture, testing, game-engine), the plan template, repo description and topics, `index.html` / manifest meta, the social preview, and every spec that states the current target or versions MUST be updated. Historical quotes stay verbatim; historical measurements stay labeled.
- **FR-008**: Breaking changes, new analyzer warnings and obsoletions MUST be fixed in this change. A before/after publish file list MUST be compared and any difference explained.
- **FR-009**: Spec 014 task T006 MUST be checked off in this PR, linking PR #7 (`49f518f`) and the confirmed deploy.

## Success Criteria

- **SC-001**: All test suites pass (at least 372 tests) including E2E, and CI on `main` deploys the merge commit.
- **SC-002**: The live footer at https://gkizior.github.io/blazor-2048/ shows .NET 11 and the merge SHA.
- **SC-003**: `dotnet list package --outdated` reports nothing held back except the ignored next major, and every bumped package's license is still free to use.

## Assumptions

- The latest stable SDK at implementation time is 11.0.100 and the WebAssembly packages are 11.0.0 (no 11.0.1 yet). If a newer stable patch ships before this merges, that newer version is used.
- Re-rendering the social preview image is required because it shows the .NET version. GitHub has no API for the repo social preview, so Garrett still needs to upload the new PNG in Settings.
