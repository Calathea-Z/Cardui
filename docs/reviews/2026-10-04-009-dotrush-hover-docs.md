# DotRush hover documentation

Date: October 4, 2026

## Increment

Restored XML summary hover in DotRush after the `Cardui.sln` pin. The
language server shows method comments only when the compiler is asked
to produce documentation.

## Changes

- Added root `Directory.Build.props` so `api`, `worker`, and
  `tests/Cardui.Tests` set `GenerateDocumentationFile` to `true`.
- Suppressed CS1573, CS1591, and CS1712. Those fire when `/doc` is on
  and a public type has no comment. Models and DTOs are often
  undocumented; leaving the warnings on would refill Problems after
  the pin.
- DotRush has no hover-doc setting. Current keys cover the loaded
  solution, metadata for referenced projects (default off), and
  compile-after-load. None of those turn summaries on.
- Did not add C# Dev Kit, `dotnet.defaultSolution`, or application
  code. Runtime behavior is unchanged. Builds now write `*.xml` next
  to the assemblies under `bin/` (already gitignored).

## Why hover went blank

DotRush hover uses Roslyn Quick Info. Quick Info reads XML comments
from source when `/doc` is enabled, and from a sibling `.xml` file
when a project is seen as metadata (worker and tests reference `api`).

None of the csproj files set `GenerateDocumentationFile` or
`DocumentationFile`. After the pin, DotRush loads `Cardui.sln` through
MSBuild. That path does not pass `/doc` unless documentation output is
on, so hover kept the signature and dropped the `<summary>`. Loose or
single-project sessions can still parse comments; the pinned solution
does not unless this property is set.

`/// <inheritdoc />` on service implementations also needs that
documentation mode so the interface summary can appear on the
implementation.

## Agent verification

- Editor and build-output config only. No application tests.
- Confirmed current DotRush `package.json` has no hover or XML-doc
  toggle. `dotrush.roslyn.projectOrSolutionFiles` stays the pin from
  review 008.
- Confirmed `api`, `worker`, and `tests/Cardui.Tests` did not generate
  XML docs before this change.
- `dotnet build ./Cardui.sln -c Release`: succeeded, 0 warnings, 0
  errors. `api.xml`, `worker.xml`, and `Cardui.Tests.xml` were written
  under each project's `bin/Release/net10.0/`. `api.xml` includes the
  `IAccountsService.GetAccountsAsync` summary; the implementation
  member keeps `<inheritdoc />` for Quick Info to expand.

## Manual verification

Please check these, or waive them.

1. Pull this branch, then run **Developer: Reload Window** once so
   DotRush re-evaluates MSBuild (including `Directory.Build.props`).
   Expected: `Cardui.sln` stays pinned. No extra C# Dev Kit prompt.
2. Open `api/Services/Interfaces/IAccountsService.cs` and hover a
   documented method.
   Expected: the XML `<summary>` appears under the signature.
3. Open `api/Services/Implementations/AccountsService.cs` and hover a
   method that uses `<inheritdoc />`.
   Expected: the interface summary appears, not an empty hover.
4. Check **Problems** on a DTO or model file that has no XML comments.
   Expected: no new CS1591 flood. Real compile errors still show.
