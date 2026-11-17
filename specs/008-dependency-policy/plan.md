# Implementation Plan: Dependency policy and Dependabot

**Feature**: `008-dependency-policy` | **Date**: 2026-10-08 | **Spec**: [spec.md](spec.md)

## Summary

One `dependabot.yml` with four ecosystems (nuget, github-actions, dotnet-sdk and, since spec 013, npm for `tools/mermaid`) on the same schedule and a `minor-and-patch` group
each. `deploy.yml` runs build and E2E for PRs but its deploy job has `if: github.event_name != 'pull_request'`.

## Technical Context

Current versions (last updated 2026-11-17, see [015](../015-dotnet-11/plan.md)):

| Package / Action | Version | License |
|---|---|---|
| Microsoft.AspNetCore.Components.WebAssembly (+ DevServer) | 11.0.0 | MIT |
| xunit.v3 / xunit.runner.visualstudio | 4.0.3 / 4.0.1 | Apache-2.0 |
| Microsoft.NET.Test.Sdk | 18.12.0 | MIT |
| bunit | 2.15.2 | MIT |
| Microsoft.Playwright (+ .Xunit.v3) | 1.64.1 | MIT |
| Markdig / YamlDotNet | 1.6.0 / 18.1.0 | BSD-2-Clause / MIT |
| @mermaid-js/mermaid-cli / mermaid (locked) / puppeteer (npm, `tools/mermaid`) | 12.2.0 / 12.3.0 / 25.17.0 | MIT / MIT / Apache-2.0 |
| actions/checkout, setup-dotnet, setup-node | v7.0.1, v6.1.0, v7.1.0 | MIT |
| upload-artifact, download-artifact | v7.0.2, v8.0.2 | MIT |
| configure-pages, upload-pages-artifact, deploy-pages | v6.0.0, v5.0.0, v5.0.1 | MIT |
| Node (CI) | 24 | MIT |
| .NET SDK (`global.json`) | 11.0.100 | MIT |

Held back: none. Dependabot ignores `semver-major` updates of the .NET SDK and of `Microsoft.AspNetCore.*` / `Microsoft.Extensions.*`, so a move to .NET 12 is Garrett's decision ([015](../015-dotnet-11/spec.md)).

## Constitution Check

| Principle | Status | Notes |
|---|---|---|
| I. Blazor/C# first | ✅ |  |
| II. .NET 11 | ✅ | dotnet-sdk ecosystem keeps `global.json` on the latest 11.0 SDK; majors ignored ([015](../015-dotnet-11/spec.md)). |
| III. Tested | ✅ | Every Dependabot PR runs all suites. |
| IV. Dependencies | ✅ | This spec is the source of principle IV. |
| V. Pages deploy | ✅ | PRs never deploy. |
| VI. Accessible | ✅ |  |
| VII. Documented | ✅ | `docs/build-and-deploy.md`. |
| VIII. Spec first | ⚠️ | Reconstructed after the fact. |

## Project Structure

```text
.github/dependabot.yml
.github/workflows/deploy.yml
global.json
tools/mermaid/package.json, package-lock.json
```
