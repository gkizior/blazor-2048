# Implementation Plan: Build-info footer

**Feature**: `005-build-info-footer` | **Date**: 2026-10-08 | **Spec**: [spec.md](spec.md)

## Summary

An MSBuild target (`AddBuildInfo` in `Blazor2048.csproj`) emits `AssemblyMetadata` for
`BuildCommit`, `BuildDate` and `RepositoryUrl`. `BuildInfo.FromAssembly` reads them plus the
informational version and `RuntimeInformation.FrameworkDescription`; `AppFooter` renders them.

## Technical Context

- **Primary Dependencies**: none added
- **Testing**: `FooterTests` (bUnit), `FeatureE2ETests` footer test (Playwright)

## Constitution Check

| Principle | Status | Notes |
|---|---|---|
| I. Blazor/C# first | ✅ | Pure Razor + MSBuild. |
| II. .NET 10 | ✅ | Shows the running .NET runtime. |
| III. Tested | ✅ | bUnit + E2E; live SHA verified after each deploy. |
| IV. Dependencies | ✅ | None. |
| V. Pages deploy | ✅ | CI provides `GITHUB_SHA`. |
| VI. Accessible | ✅ | Muted but readable in both themes. |
| VII. Documented | ✅ | `docs/components.md`, `docs/build-and-deploy.md`. |
| VIII. Spec first | ⚠️ | Reconstructed after the fact. |

## Project Structure

```text
src/Blazor2048/Blazor2048.csproj         AddBuildInfo target, Version
src/Blazor2048/Services/BuildInfo.cs
src/Blazor2048/Components/AppFooter.razor
tests/Blazor2048.ComponentTests/FooterTests.cs
```
