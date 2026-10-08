# Feature Specification: Build-info footer

**Feature**: `005-build-info-footer` | **Created**: 2026-10-08 | **Status**: Implemented

## Original Request

The exact wording of this request was not kept in the repository. Its requirements as relayed in
the feature task, condensed:

- A footer with "Garrett Kizior", the .NET runtime version, the app version, the short commit SHA
  linked to the GitHub commit, and the build date.
- Commit and date come from `AssemblyMetadata` set by MSBuild (`GITHUB_SHA` in CI, `git` as the
  local fallback).
- Small, and matching both themes.
- After deploying, the live footer SHA must match the pushed commit.

- **Source**: Message from Garrett, relayed to the implementing assistant
- **Date**: 2026-10-08

## User Scenarios & Testing

### User Story 1 - See exactly what is deployed (Priority: P1)

**Independent Test**: Open the live site and compare the footer SHA with `git rev-parse --short HEAD`.

**Acceptance Scenarios**:

1. **Given** a CI build of commit X, **When** the live site loads, **Then** the footer shows the first 7 characters of X linked to `https://github.com/gkizior/blazor-2048/commit/X`.
2. **Given** any page (game or docs), **When** it loads, **Then** the footer shows "© year Garrett Kizior", the app version, ".NET 10.x" and the build date.
3. **Given** either theme, **When** the footer renders, **Then** it is small, muted and readable.

### Edge Cases

- Local builds outside git show `unknown` instead of failing.
- Local builds stamp only the date so incremental builds stay incremental; CI stamps the full UTC time.

## Requirements

### Functional Requirements

- **FR-001**: The footer MUST show author, app version, .NET runtime, short SHA (linked) and build date.
- **FR-002**: Commit and date MUST come from `[AssemblyMetadata]` attributes written by MSBuild.
- **FR-003**: CI MUST use `GITHUB_SHA`; local builds MUST fall back to `git rev-parse HEAD`.
- **FR-004**: The commit link MUST open in a new tab with `rel="noopener"`.

## Success Criteria

- **SC-001**: After every deploy, the live footer SHA equals the pushed `main` commit.
- **SC-002**: `FooterTests` (bUnit) and the footer E2E test pass.
