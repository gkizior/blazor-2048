# Implementation Plan: Repository metadata, README and social image

**Feature**: `009-repo-metadata-and-social-image` | **Date**: 2026-10-08 | **Spec**: [spec.md](spec.md)

## Summary

`gh repo edit` for description, homepage and topics; README rewrite with badges and screenshots;
the social image is an HTML page (`scripts/social-preview.html`) rendered by headless Chrome at
1280x640, shipped with the docs content so it has a stable absolute URL on Pages.

## Constitution Check

| Principle | Status | Notes |
|---|---|---|
| I. Blazor/C# first | ✅ | Static meta tags; the image is generated offline. |
| II. .NET 10 | ✅ | Stale net9 references removed. |
| III. Tested | ✅ | Docs build test covers README links and images. |
| IV. Dependencies | ✅ | None. |
| V. Pages deploy | ✅ | Image served from the Pages site. |
| VI. Accessible | ✅ | `og:image:alt` set; screenshots have alt text. |
| VII. Documented | ✅ | README. |
| VIII. Spec first | ⚠️ | Reconstructed after the fact. |

## Complexity Tracking

| Exception | Why needed | Simpler alternative rejected because |
|---|---|---|
| No license badge | No LICENSE file exists | Choosing a license is Garrett's decision |
