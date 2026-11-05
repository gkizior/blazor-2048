# Implementation Plan: Dependency policy and Dependabot

**Feature**: `008-dependency-policy` | **Date**: 2026-10-08 | **Spec**: [spec.md](spec.md)

## Summary

One `dependabot.yml` with four ecosystems (nuget, github-actions, dotnet-sdk and, since spec 013, npm for `tools/mermaid`) on the same schedule and a `minor-and-patch` group
each. `deploy.yml` runs build and E2E for PRs but its deploy job has `if: github.event_name != 'pull_request'`.

## Technical Context

Current versions (last updated 2026-11-05):

| Package / Action | Version | License |
|---|---|---|
| Microsoft.AspNetCore.Components.WebAssembly (+ DevServer) | 10.0.14 | MIT |
| xunit.v3 / xunit.runner.visualstudio | 4.0.2 / 4.0.1 | Apache-2.0 |
| Microsoft.NET.Test.Sdk | 18.11.0 | MIT |
| bunit | 2.13.3 | MIT |
| Microsoft.Playwright (+ .Xunit.v3) | 1.64.1 | MIT |
| Markdig / YamlDotNet | 1.6.0 / 18.1.0 | BSD-2-Clause / MIT |
| @mermaid-js/mermaid-cli / mermaid (locked) / puppeteer (npm, `tools/mermaid`) | 12.2.0 / 12.3.0 / 25.16.0 | MIT / MIT / Apache-2.0 |
| actions/checkout, setup-dotnet, setup-node | v7.0.1, v6.1.0, v7.1.0 (SHA-pinned) | MIT |
| upload-artifact, download-artifact | v7.0.2, v8.0.2 (SHA-pinned) | MIT |
| configure-pages, upload-pages-artifact, deploy-pages | v6.0.0, v5.0.0, v5.0.1 (SHA-pinned) | MIT |
| Node (CI) | 24 | MIT |
| .NET SDK (`global.json`) | 10.0.403 | MIT |

Held back: none.

## Constitution Check

| Principle | Status | Notes |
|---|---|---|
| I. Blazor/C# first | ✅ |  |
| II. .NET 10 | ✅ | dotnet-sdk ecosystem keeps `global.json` on the latest 10.0 SDK. |
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
