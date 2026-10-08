# Feature Specification: Testing stack (xUnit v3, bUnit, Playwright for .NET)

**Feature**: `007-testing-stack` | **Created**: 2026-10-08 | **Status**: Implemented

## Original Request

> test with bUnit + Playwright for .NET (plus xUnit)

> New requirement from Garrett: migrate all test projects to xUnit v3 (xunit.v3 package, current xunit.runner.visualstudio, Microsoft.NET.Test.Sdk). Make sure bUnit and Playwright for .NET work with v3. Use Microsoft.Playwright.Xunit.v3 if it exists… IAsyncLifetime returning ValueTask, and TestContext.Current.CancellationToken… Keep CI working and every suite passing, do it as its own commit, and include it in your report.

> upgrade bUnit to the latest stable release… no prereleases. Adapt to any breaking API changes… Make sure it works with xUnit v3, and report the exact version you used.

- **Source**: Message from Garrett, relayed to the implementing assistant (standing rules in the feature task; two follow-up messages). "…" marks text omitted in the relayed copy.
- **Date**: 2026-10-08

## User Scenarios & Testing

### User Story 1 - Every layer has the right kind of test (Priority: P1)

**Acceptance Scenarios**:

1. **Given** the engine, **When** `dotnet test --solution Blazor2048.sln` runs, **Then** xUnit v3 tests cover the rules, tile tracking, palette contrast and the docs builder.
2. **Given** the components, **When** the same command runs, **Then** bUnit tests render them in memory (board, theme, footer, docs page).
3. **Given** a Release publish, **When** `RUN_E2E=1` tests run, **Then** Playwright for .NET drives the real site in Chromium under `/blazor-2048/`.

### User Story 2 - CI gates the deploy (Priority: P1)

**Acceptance Scenarios**:

1. **Given** a push to `main`, **When** any suite fails, **Then** nothing deploys.
2. **Given** a pull request, **When** CI runs, **Then** all suites run and nothing deploys.

### Edge Cases

- E2E tests are opt-in locally (`RUN_E2E=1`) because they need a browser; CI always runs them against the exact artifact it deploys.

## Requirements

### Functional Requirements

- **FR-001**: Test projects MUST use `xunit.v3`, the current `xunit.runner.visualstudio` and `Microsoft.NET.Test.Sdk`, with `OutputType` Exe.
- **FR-002**: Async fixtures MUST use `IAsyncLifetime` returning `ValueTask`; tests MUST use `TestContext.Current.CancellationToken`.
- **FR-003**: E2E tests MUST use `Microsoft.Playwright.Xunit.v3`.
- **FR-004**: bUnit MUST be the latest stable release (2.11.3 at the time of writing).
- **FR-005**: `dotnet test` MUST use Microsoft.Testing.Platform (`global.json`), as xUnit v3 4.x requires.
- **FR-006**: The deploy job MUST depend on the build/test and E2E jobs.

## Success Criteria

- **SC-001**: All three suites green locally and in CI on every deploy.
- **SC-002**: The xUnit v3 migration landed as its own commit ([`4b55f38`](https://github.com/gkizior/blazor-2048/commit/4b55f38)).
