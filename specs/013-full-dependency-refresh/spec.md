# Feature Specification: Full dependency refresh and tracked diagram tooling

**Feature**: `013-full-dependency-refresh` | **Created**: 2026-11-05 | **Status**: Implemented (written before the changes)

## Original Request

> Again I want to to fully update all things on the repo. Libraries and frameworks and anything else. Report back what your plan is and then I’ll approve

> Approved, do all of it

- **Source**: Messages from Garrett (the second as relayed to the implementing assistant)
- **Date**: 2026-11-05

The approved plan (condensed): bump Markdig 1.5.1 → 1.6.0; bump mermaid-cli 12.0.0 → 12.2.0 and
puppeteer 25.12.0 → 25.16.0 and move their pins into a committed `package.json` + `package-lock.json`
so Dependabot tracks them (npm ecosystem); re-render every diagram; pin every GitHub Action to a full
commit SHA; update specs and docs. .NET stays on 10 (LTS, SDK 10.0.403 is the latest) until .NET 11 is GA.

## User Scenarios & Testing

### User Story 1 - Nothing is out of date (Priority: P1)

**Acceptance Scenarios**:

1. **Given** the repo after this change, **When** `dotnet list package --outdated` runs, **Then** it reports nothing.
2. **Given** `tools/mermaid/package.json`, **When** `npm outdated` runs there, **Then** it reports nothing.

### User Story 2 - Diagram tooling is tracked (Priority: P1)

**Acceptance Scenarios**:

1. **Given** a new mermaid-cli or puppeteer release, **When** Dependabot runs on Monday 08:00 America/Chicago, **Then** it opens an npm PR for `tools/mermaid`.
2. **Given** a change to the locked mermaid-cli or mermaid version, **When** DocsBuilder renders diagrams, **Then** every diagram hash changes and all SVGs re-render.

### User Story 3 - Actions are pinned (Priority: P2)

**Acceptance Scenarios**:

1. **Given** `.github/workflows/deploy.yml`, **When** it is read, **Then** every `uses:` is a 40-character commit SHA with a `# vX.Y.Z` comment.

## Requirements

### Functional Requirements

- **FR-001**: Markdig MUST be 1.6.0; mermaid-cli 12.2.0 and puppeteer 25.16.0 MUST be pinned exactly in `tools/mermaid/package.json` with a committed lockfile.
- **FR-002**: DocsBuilder MUST read the versions from the package/lock files (no hardcoded versions) and render with the locally installed `mmdc` (`npm ci`).
- **FR-003**: `.github/dependabot.yml` MUST add the `npm` ecosystem (directory `/tools/mermaid`) on the same weekly schedule with a minor/patch group.
- **FR-004**: Every GitHub Action MUST be pinned to the full commit SHA of its latest release.
- **FR-005**: Specs 006 and 008, the constitution, README and docs MUST describe the new tracking.

## Success Criteria

- **SC-001**: All test suites pass (at least 364 tests) and CI deploys the commit.
- **SC-002**: The live footer shows the new commit and the docs diagrams load in light and dark themes.
