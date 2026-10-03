# CI production builds

Date: October 3, 2026

## Increment

Extended the existing GitHub Actions workflow so it also builds the worker and
runs the frontend production build. Database integration checks remain later
work because there is no integration-test suite yet.

## Changes

- Added a Worker build job that runs
  `dotnet build ./worker/worker.csproj --configuration Release`.
- Added `pnpm build` to the frontend job after tests and lint.
- Set `API_BASE_URL` and `NEXT_PUBLIC_API_BASE_URL` to `http://localhost:5235`
  for that build step. The server API client throws when `API_BASE_URL` is
  missing, and the local env file is not in the repository.
- Documented the new commands in the root README.
- The workflow still does not start PostgreSQL, apply migrations, or deploy.

## Agent verification

- `dotnet build .\worker\worker.csproj --configuration Release`: succeeded,
  0 warnings, 0 errors. It compiled the API project reference and the worker.
  It did not run the worker.
- `pnpm build` in `frontend/`: succeeded. Next.js compiled, type-checked, and
  generated the static pages. Account, transaction, dashboard, category, and
  institution routes stayed dynamic, so this build did not call the API.
- `git diff --check` on the workflow and README: passed.

These commands ran on the local Windows machine. The local frontend build used
the existing untracked env file. CI supplies the same non-sensitive local URLs
because that file is not checked in.

No application or database data changed.

## Manual verification

Zach pushed this change to `main` (`4817809`) and reported on October 3, 2026
that the GitHub Actions run succeeded. He approved this increment. This session
confirmed that commit is on `origin/main` and did not read the Actions logs.
No application or database data changed.

## Remaining considerations

- A PostgreSQL integration job is still absent. It would not check behavior
  until integration tests exist.
- The frontend build in CI uses local development URLs so compilation can
  succeed. It is not a deployable production bundle.
- Node.js remains pinned to major version 22. The repository does not pin a
  Node patch.
