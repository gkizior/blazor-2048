# Implementation Plan: Dependency policy and Dependabot

**Feature**: `008-dependency-policy` | **Date**: 2026-10-08 | **Spec**: [spec.md](spec.md)

## Summary

One `dependabot.yml` with three ecosystems on the same schedule and a `minor-and-patch` group
each. `deploy.yml` runs build and E2E for PRs but its deploy job has `if: github.event_name != 'pull_request'`.

## Technical Context

Current versions (last updated 2026-11-02):

| Package / Action | Version | License |
|---|---|---|
| Microsoft.AspNetCore.Components.WebAssembly (+ DevServer) | 10.0.14 | MIT |
| xunit.v3 / xunit.runner.visualstudio | 4.0.2 / 4.0.1 | Apache-2.0 |
| Microsoft.NET.Test.Sdk | 18.11.0 | MIT |
| bunit | 2.13.3 | MIT |
| Microsoft.Playwright (+ .Xunit.v3) | 1.64.1 | MIT |
| Markdig / YamlDotNet | 1.5.1 / 18.1.0 | BSD-2-Clause / MIT |
| actions/checkout, setup-dotnet, setup-node | v7, v6, v7 | MIT |
| upload-artifact, download-artifact | v7, v8 | MIT |
| configure-pages, upload-pages-artifact, deploy-pages | v6, v5, v5 | MIT |
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
```
