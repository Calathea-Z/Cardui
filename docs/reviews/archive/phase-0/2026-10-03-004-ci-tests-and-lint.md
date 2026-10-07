# CI for API tests, frontend tests, and lint

Date: October 3, 2026

## Increment

Added the first GitHub Actions workflow. It runs the existing API test suite,
frontend tests, and frontend lint. Production builds and integration checks
remain later work.

## Changes

- Added `.github/workflows/ci.yml`.
- API tests run `dotnet test ./Cardui.sln --configuration Release` with the
  SDK pinned by `global.json`.
- Frontend checks install from the lockfile with the pnpm version in
  `frontend/package.json`, then run `pnpm test` and `pnpm lint` on Node.js 22.
- The workflow runs on pull requests and on pushes to `main`. It does not start
  PostgreSQL, apply migrations, or run `pnpm build`.
- Documented those commands in the root README.

## Agent verification

- `dotnet test .\Cardui.sln --configuration Release`: 62 passed, 0 failed.
  That command restored the worker and built the API and test project. It did
  not build the worker.
- `pnpm test` in `frontend/`: 16 passed, 0 failed.
- `pnpm lint` in `frontend/`: completed with no reported problems.
- `git diff --check`: passed.

These commands ran on the local Windows machine. The branch was then pushed to
`origin/feature/ci-tests-and-lint`.

## Manual verification

Zach reported on October 3, 2026 that the GitHub Actions tests ran and
succeeded. He approved this increment. No application or database data changed.
This session did not read the Actions logs.

## Remaining considerations

- Phase 0 item 6 still includes production builds and future integration
  checks. This workflow does not run `pnpm build`, build the worker, or start
  PostgreSQL.
- Node.js is pinned to major version 22, matching the local 22.17.1 install.
  The repository does not pin a Node patch.
