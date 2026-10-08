# Feature Specification: Spec-driven development (Spec Kit layout)

**Feature**: `010-spec-driven-development` | **Created**: 2026-10-08 | **Status**: Implemented

## Original Request

The full message is condensed here; its last paragraph is quoted verbatim.

- Mirror the GitHub Spec Kit layout (`.specify/memory/constitution.md`,
  `.specify/templates/`, `specs/NNN-feature-name/{spec,plan,tasks}.md`) without Copilot or the
  `specify` CLI.
- A constitution with Garrett's principles: Blazor/C# first with minimal interop and no custom JS;
  .NET 10; xUnit v3 + latest stable bUnit + Playwright for .NET with all suites green before deploy;
  latest stable free-license packages kept current by Dependabot weekly (Monday 08:00 Central) and
  merged after CI passes; deploy through the GitHub Pages workflow; accessibility (contrast,
  reduced motion); docs with Mermaid.
- Specs 001–009 reconstructed for the features built so far, each with user stories,
  Given/When/Then acceptance criteria, FR-### requirements, success criteria, and the original
  request quoted verbatim with its source and date; a plan matching what was built; tasks checked
  off and linked to commits.
- Templates, `docs/spec-driven-development.md` with a Mermaid flowchart, a "Specs" section in the
  in-app docs sidebar, and a README link. Optionally a check that every spec folder is complete.
- Do it after the animation fix is deployed, in its own commits.

> Going forward, every new feature request from Garrett gets a spec folder first. Keep all tests green, commit, push, confirm the deploy, and include this in your report (file tree plus the commit SHA).

- **Source**: Message from Garrett, relayed to the implementing assistant
- **Date**: 2026-10-08

## User Scenarios & Testing

### User Story 1 - Every feature has a spec (Priority: P1)

**Acceptance Scenarios**:

1. **Given** the repo, **When** `specs/` is listed, **Then** every feature built so far has a numbered folder with `spec.md`, `plan.md` and `tasks.md`.
2. **Given** a spec, **When** it is read, **Then** it quotes the original request (or says plainly that the wording was not kept) with source and date.
3. **Given** a tasks file, **When** a task is done, **Then** it is checked and links to its commit.

### User Story 2 - Specs are part of the docs (Priority: P2)

**Acceptance Scenarios**:

1. **Given** the in-app docs, **When** the sidebar is shown, **Then** a "Specs" section lists the workflow page, the constitution, the templates and every spec, with its plan and tasks nested under it.
2. **Given** the README, **When** it is read, **Then** it links to the specs and the workflow page.

### User Story 3 - Incomplete specs fail the build (Priority: P2)

**Acceptance Scenarios**:

1. **Given** a `specs/NNN-name/` folder without `plan.md`, **When** the unit tests run, **Then** they fail.
2. **Given** a new spec folder missing from `docs/toc.yml`, **When** the unit tests run, **Then** they fail.

## Requirements

### Functional Requirements

- **FR-001**: `.specify/memory/constitution.md` MUST hold the project principles, quality gates and governance.
- **FR-002**: `.specify/templates/` MUST contain spec, plan and tasks templates; the spec template MUST include an Original Request section.
- **FR-003**: `specs/NNN-feature-name/` folders MUST each contain `spec.md`, `plan.md` and `tasks.md`.
- **FR-004**: Each spec MUST have Given/When/Then scenarios, `FR-###` requirements and success criteria.
- **FR-005**: The docs sidebar MUST show a Specs section with nested plan/tasks pages.
- **FR-006**: A unit test MUST check spec folder completeness, naming, required sections and TOC coverage.
- **FR-007**: New feature requests MUST get a spec folder before implementation.

## Success Criteria

- **SC-001**: `SpecsTests` pass in CI, and the live docs show the Specs section.
- **SC-002**: Specs 001–010 exist; gaps in the historical record are stated, not invented.

## Assumptions

- No Spec Kit tooling (`specify` CLI, agent prompts) is used; only the file layout and templates.
