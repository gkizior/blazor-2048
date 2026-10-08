# Implementation Plan: Purple app icons

**Feature**: `012-purple-app-icons` | **Date**: 2026-10-08 | **Spec**: [spec.md](spec.md)

## Summary

Draw the icons with HTML/CSS (`scripts/app-icons.html`, the same approach as the social preview),
render each variant with headless Chromium at its exact size with a transparent background, and
pack `favicon.ico` from 16/32/48 px renders. Variants: `#any` (rounded tile, transparent corners),
`#full` (full-bleed square for iOS), `#maskable` (full bleed, text inside the 80% safe zone) and
`#any,tiny` (heavier text for 16–48 px).

| File | Size | Variant |
|---|---|---|
| `favicon.png` | 32 | any, tiny |
| `favicon.ico` | 16, 32, 48 | any, tiny |
| `apple-touch-icon.png` | 180 | full |
| `icon-192.png`, `icon-512.png` | 192, 512 | any |
| `icon-maskable-192.png`, `icon-maskable-512.png` | 192, 512 | maskable |

The service worker's cache name gets a revision (`offline-cache-r2-<version>`); old caches still
match the `offline-cache-` prefix, so activation deletes them. The 192/512 apple-touch-icon links
are dropped: they pointed at icons with transparent corners, which iOS fills with black.

## Constitution Check

| Principle | Status | Notes |
|---|---|---|
| I. Blazor/C# first | ✅ | Static assets only; no app JavaScript. |
| II. .NET 10 | ✅ | No change. |
| III. Tested | ✅ | Unit test for manifest icons and `favicon.ico`; checked live after deploy. |
| IV. Dependencies | ✅ | None added (Chromium and Pillow are only used offline to render). |
| V. Pages deploy | ✅ | Ships with the normal deploy. |
| VI. Accessible | ✅ | White on `#6a3df0` (about 7:1). |
| VII. Documented | ✅ | This spec; the source file explains how to regenerate. |
| VIII. Spec first | ✅ | Written before the icons were committed. |
| IX. Snappy and lean | ✅ | Small PNGs (all icons together about 63 KB; icons are not on the startup path). |
