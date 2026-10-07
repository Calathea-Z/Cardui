# DotRush solution pin

Date: October 4, 2026

## Increment

Pinned DotRush to this repository's `Cardui.sln` so the language server
does not have to be restarted after every C# edit.

## Changes

- `.vscode/settings.json` sets `dotrush.roslyn.projectOrSolutionFiles`
  to `Cardui.sln`. That is DotRush's current workspace key for the
  project or solution to load.
- `dotnet.defaultSolution` was not added. That key belongs to C# Dev
  Kit, which this increment does not introduce.
- `.gitignore` still ignores other `.vscode` files. It now keeps
  `.vscode/settings.json` so the pin can be committed.
- No application code, migrations, or C# Dev Kit changes.

## Agent verification

- Documentation and editor config only. No application tests.
- Confirmed `Cardui.sln` is at the repository root and includes `api`,
  `worker`, and `tests/Cardui.Tests`.
- Confirmed current DotRush docs still use
  `dotrush.roslyn.projectOrSolutionFiles` (array of `.sln` / `.csproj`
  paths). Older `dotrush.roslyn.projectFiles` is not the current name.

## Manual verification

Please check these, or waive them.

1. After pulling this change, run **Developer: Reload Window** once.
   Expected: DotRush loads `Cardui.sln` without the project/solution
   picker. A later C# edit should not need another reload or extension
   restart for Problems to stay current.
2. Open a C# file, make a small edit, then check **Problems**.
   Expected: errors match the real compile state of `Cardui.sln`, not
   stale or fake diagnostics from an unloaded project.
