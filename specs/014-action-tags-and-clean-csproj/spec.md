# Feature Specification: Plain action tags and clean project files

**Feature**: `014-action-tags-and-clean-csproj` | **Created**: 2026-11-05 | **Status**: Implemented

## Original Request

> I don’t like pinning gh action to hashes. Prefer just v like before.
>
> Also can you clean up the csproj file. It’s pretty messy and hard to digest. Keep it as simple and clean as possible

- **Source**: Message from Garrett
- **Date**: 2026-11-05

## User Scenarios & Testing

### User Story 1 - Actions use plain major tags (Priority: P1)

**Acceptance Scenarios**:

1. **Given** `.github/workflows/deploy.yml`, **When** it is read, **Then** every `uses:` is `owner/action@vN` with no commit SHA or version comment.

### User Story 2 - The app's project file is easy to read (Priority: P1)

**Acceptance Scenarios**:

1. **Given** `src/Blazor2048/Blazor2048.csproj`, **When** it is read, **Then** it holds only the version, app settings, packages and references, and imports the docs and build-info targets.
2. **Given** a publish before and after the cleanup, **When** the outputs are compared, **Then** the file list, docs content and footer build info are the same.

## Requirements

### Functional Requirements

- **FR-001**: Actions MUST use plain major-version tags (reverses spec 013 FR-004).
- **FR-002**: Shared settings (`net10.0`, nullable, implicit usings, not packable) MUST live once in `Directory.Build.props`; test-project settings in `tests/Directory.Build.props`.
- **FR-003**: The docs build and footer build info MUST move to `src/Blazor2048/build/Docs.targets` and `BuildInfo.targets` with unchanged behavior.
- **FR-004**: Package versions MUST stay in the `.csproj` files so Dependabot can update them.

## Success Criteria

- **SC-001**: All suites pass (at least 369 tests) and the deploy shows the new commit in the footer.
- **SC-002**: `Blazor2048.csproj` shrinks from 97 lines to 25.
