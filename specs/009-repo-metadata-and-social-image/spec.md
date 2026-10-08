# Feature Specification: Repository metadata, README and social image

**Feature**: `009-repo-metadata-and-social-image` | **Created**: 2026-10-08 | **Status**: Implemented

## Original Request

> (1) The GitHub repo description is out of date (it still says net9). Update the description, homepage URL (https://gkizior.github.io/blazor-2048/), and topics… Use `gh repo edit`. Also sweep the README and every doc for stale net9 or old version references, and add CI/deploy/license badges and a screenshot to the README. (2) Add a repo image. Create a 1280x640 social preview PNG… commit it (for example docs/images/social-preview.png)… GitHub has no API for setting the social preview, so just tell me its path… Also use it, or a variant, as the og:image/twitter:image meta in index.html, with absolute URLs under the Pages site.

- **Source**: Message from Garrett, relayed to the implementing assistant. "…" marks text omitted in the relayed copy.
- **Date**: 2026-10-08

## User Scenarios & Testing

### User Story 1 - The repo page is accurate (Priority: P1)

**Acceptance Scenarios**:

1. **Given** the GitHub repo page, **When** it is viewed, **Then** the description says .NET 10 Blazor WebAssembly PWA, the homepage is the Pages site, and topics describe the stack.
2. **Given** the README, **When** it is read, **Then** it has CI and deploy badges, light and dark screenshots, and no .NET 9 references.

### User Story 2 - Links look good when shared (Priority: P2)

**Acceptance Scenarios**:

1. **Given** the site URL is shared, **When** a platform reads its meta tags, **Then** it shows the 1280x640 preview image via absolute `og:image`/`twitter:image` URLs.

## Requirements

### Functional Requirements

- **FR-001**: Description, homepage and topics MUST be set with `gh repo edit`.
- **FR-002**: README and docs MUST NOT contain stale .NET 9 references.
- **FR-003**: README MUST show CI, deploy and license badges and a screenshot.
- **FR-004**: A 1280x640 social preview PNG MUST be committed at `docs/images/social-preview.png`.
- **FR-005**: `index.html` MUST reference it with absolute URLs under `https://gkizior.github.io/blazor-2048/`.

## Success Criteria

- **SC-001**: `https://gkizior.github.io/blazor-2048/docs-content/images/social-preview.png` returns the image.
- **SC-002**: Garrett uploads the PNG in Settings → Social preview (no API exists).

## Assumptions

- The license badge waits for a LICENSE file, which Garrett has not chosen yet ([NEEDS CLARIFICATION: license]).
