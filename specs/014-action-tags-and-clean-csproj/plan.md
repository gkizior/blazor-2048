# Implementation Plan: Plain action tags and clean project files

**Feature**: `014-action-tags-and-clean-csproj` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

## Summary

- Workflow: `uses: actions/<name>@<sha> # vX.Y.Z` back to `@vN`.
- `Directory.Build.props`: `TargetFramework`, `Nullable`, `ImplicitUsings`, `IsPackable` for all projects.
- `tests/Directory.Build.props`: imports the root props; `OutputType=Exe` and `using Xunit`.
- `Blazor2048.csproj`: one PropertyGroup, one package ItemGroup, one reference ItemGroup, two imports.
- `build/Docs.targets`: DocsBuilder project reference, inputs, `BuildDocs` and `IncludeDocsContent`.
- `build/BuildInfo.targets`: `AddBuildInfo`, now using `AssemblyMetadata` items. The explicit
  `RepositoryUrl` attribute was a duplicate (the SDK already emits it from `$(RepositoryUrl)`) and is gone.

Verified: publish before/after has the same 418 files and identical docs content; generated assembly
attributes match apart from the removed duplicate.

## Constitution Check

| Principle | Status | Notes |
|---|---|---|
| I. Blazor/C# first | ✅ | No code change. |
| II. .NET 10 | ✅ | `net10.0` now set once. |
| III. Tested | ✅ | Full suite with E2E. |
| IV. Dependencies | ✅ | Versions unchanged; Dependabot still edits the `.csproj` files. Constitution updated for tags. |
| V. Pages deploy | ✅ | Same workflow, tags only. |
| VI. Accessible | ✅ | No UI change. |
| VII. Documented | ✅ | build-and-deploy, docs-system, README, specs 005/008/013. |
| VIII. Spec first | ✅ | This spec. |
| IX. Snappy and lean | ✅ | Same output. |

## Project Structure

```text
Directory.Build.props
tests/Directory.Build.props
src/Blazor2048/Blazor2048.csproj
src/Blazor2048/build/Docs.targets
src/Blazor2048/build/BuildInfo.targets
.github/workflows/deploy.yml
```
