# Feature Specification: Dependency policy and Dependabot

**Feature**: `008-dependency-policy` | **Created**: 2026-10-08 | **Status**: Implemented

## Original Request

> (1) Add a simple .github/dependabot.yml covering the nuget ecosystem (root, all projects) and github-actions, on a weekly schedule. Group minor and patch updates per ecosystem… confirm the workflow triggers on pull_request without deploying from PRs). (2) Standing rule: every package should be on the highest stable version, never prerelease, as long as its license is free to use (MIT, Apache-2.0, BSD, MS-PL, etc., not commercial/paid like newer FluentAssertions 8+, Moq-style concerns, etc.). As part of this batch, bump every NuGet package and every GitHub Action to its latest stable version (`dotnet list package --outdated` helps), check each license, and report any you held back and why. Include the package/version table in your report.

> every ecosystem in .github/dependabot.yml should use schedule interval weekly, day monday, time "08:00", timezone "America/Chicago". A follow-up check runs at 8:57 Monday Central, right after it. If Dependabot supports the dotnet-sdk ecosystem (for global.json), add it on the same schedule.

- **Source**: Message from Garrett, relayed to the implementing assistant (two follow-up messages). "…" marks text omitted in the relayed copy.
- **Date**: 2026-10-08

## User Scenarios & Testing

### User Story 1 - Updates arrive on schedule (Priority: P1)

**Acceptance Scenarios**:

1. **Given** Monday 08:00 America/Chicago, **When** Dependabot runs, **Then** it checks `nuget`, `github-actions` and `dotnet-sdk` and opens at most one grouped minor/patch PR per ecosystem.
2. **Given** a Dependabot PR, **When** CI runs, **Then** every suite runs and nothing deploys.
3. **Given** CI passed, **When** the 08:57 follow-up check runs, **Then** the PR can be merged and deployed from `main`.

### User Story 2 - Only free, stable packages (Priority: P1)

**Acceptance Scenarios**:

1. **Given** any package, **When** it is added or bumped, **Then** it is the highest stable version and its license is free to use.
2. **Given** a package whose new major goes commercial, **When** it is reviewed, **Then** it is held back (or replaced) and the reason is recorded.

## Requirements

### Functional Requirements

- **FR-001**: `.github/dependabot.yml` MUST cover `nuget` (directory `/`, all projects), `github-actions` and `dotnet-sdk`.
- **FR-002**: Every ecosystem MUST run weekly, Monday, 08:00, America/Chicago.
- **FR-003**: Minor and patch updates MUST be grouped per ecosystem.
- **FR-004**: The workflow MUST run tests on `pull_request` and MUST NOT deploy from PRs.
- **FR-005**: All NuGet packages and Actions MUST be on their latest stable versions with free licenses; prereleases are forbidden.

## Success Criteria

- **SC-001**: `dotnet list package --outdated` reports nothing.
- **SC-002**: Dependabot's first run after setup opened no PRs (everything current).
