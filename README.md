# Blazor 2048

The classic **2048** sliding-tile puzzle, built with **.NET 10 Blazor WebAssembly** as an installable PWA.
Play it on your iPhone straight from Safari — no Mac, no App Store, no fees.

**Play:** https://gkizior.github.io/blazor-2048/

## Features

- Standard 4×4 2048: slide tiles, merge equal numbers, reach 2048
- Swipe on touch screens, arrow keys (or WASD) on desktop
- New tile after every move that changes the board: 2 (90%) or 4 (10%)
- Score, plus best score saved on the device
- Win and game-over messages, "Keep going" after 2048, and a New Game button
- Mobile-first layout that fits an iPhone screen with no scrolling or bounce
- Works offline and runs full-screen when added to the home screen

## How it's built

| Path | What's there |
|---|---|
| `src/Game2048.Core` | Pure C# game logic (`Game.cs`): sliding, merging, spawning, win/lose |
| `src/Blazor2048` | Blazor WebAssembly PWA. `Components/GameBoard.razor` is the whole game UI |
| `tests/Blazor2048.Tests` | xUnit tests for the move and merge rules |
| `tests/Blazor2048.ComponentTests` | bUnit tests for the `GameBoard` component (render, keys, swipes, New Game, overlays) |
| `tests/Blazor2048.E2ETests` | Playwright for .NET end-to-end tests against the published app (opt-in locally) |
| `scripts/prepare-pages.py` | Sets `<base href>` for GitHub Pages and adds `404.html` and `.nojekyll` |
| `.github/workflows/deploy.yml` | Builds, tests, publishes, and deploys to GitHub Pages |

Input is handled entirely in Blazor: `@onkeydown` on a focused element for keys, and
`@ontouchstart` / `@ontouchend` with swipe direction worked out in C#. There's no custom
JavaScript. The only JS interop is the call to the browser's built-in `localStorage`
(in `Services/BestScoreStore.cs`), so the best score survives a reload.

## Run it locally

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/gkizior/blazor-2048.git
cd blazor-2048
dotnet test                       # run the unit tests
dotnet run --project src/Blazor2048
```

Then open the URL printed in the terminal (for example `http://localhost:5000`).

## Add it to your iPhone home screen

1. Open **https://gkizior.github.io/blazor-2048/** in **Safari**.
2. Tap the **Share** button (the square with an arrow pointing up).
3. Scroll down and tap **Add to Home Screen**, then tap **Add**.

It now opens full-screen from its own icon, like an app, and works offline.

## Deploying

Every push to `main` runs the GitHub Actions workflow. It builds the app, runs the xUnit and bUnit tests,
runs the Playwright E2E tests against the published site, sets the base path to `/<repo-name>/`, and
deploys with `actions/deploy-pages`. Deployment only happens if all of the tests pass.
