# Cardui

Cardui is a personal financial recovery application. The repository contains:

- `api/`: .NET 10 HTTP API and EF Core migrations.
- `frontend/`: Next.js 16 / React 19 web application.
- `worker/`: one-shot scheduled Plaid synchronization worker.
- `tests/Cardui.Tests/`: .NET unit tests.

The current application requires PostgreSQL and Plaid configuration to start. Keep all
database and Plaid credentials in .NET User Secrets or environment variables; do not add
them to checked-in settings or documentation.

## Prerequisites

- Windows PowerShell.
- .NET SDK 10.0.301 (pinned by `global.json`).
- Docker Desktop with Docker Compose.
- Node.js and pnpm 10.33.0 (pinned by `frontend/package.json`).
- EF Core CLI available as `dotnet ef`.
- Plaid sandbox credentials for account-link and synchronization checks.

Run commands below from the repository root unless a step says otherwise.

## First-time setup

### 1. Start PostgreSQL

```powershell
docker compose up -d postgres
docker compose ps
```

The Compose service stores its data in the `cardui_pgdata` volume and exposes PostgreSQL
on local port `5433`.

### 2. Configure the API

Set the API's local configuration without writing secrets into the repository:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<local PostgreSQL connection string>" --project .\api
dotnet user-secrets set "Plaid:ClientId" "<Plaid sandbox client ID>" --project .\api
dotnet user-secrets set "Plaid:Secret" "<Plaid sandbox secret>" --project .\api
dotnet user-secrets set "Plaid:Environment" "sandbox" --project .\api
dotnet user-secrets set "Plaid:ClientName" "Cardui" --project .\api
```

Use the local database values defined by `docker-compose.yml` when constructing the
connection string. Do not paste the resulting connection string into source files,
documentation, screenshots, or logs.

Restore packages and apply the checked-in migrations:

```powershell
dotnet restore .\Cardui.sln
dotnet ef database update --project .\api --startup-project .\api
```

Migration generation is intentionally not part of setup. Model changes and new
migrations are handled as separate reviewed work.

### 3. Configure the frontend

Create `frontend/.env.local` with non-sensitive local URLs:

```powershell
@"
API_BASE_URL=http://localhost:5235
NEXT_PUBLIC_API_BASE_URL=http://localhost:5235
"@ | Set-Content .\frontend\.env.local
```

Install the locked frontend dependencies:

```powershell
Push-Location .\frontend
pnpm install --frozen-lockfile
Pop-Location
```

## Run locally

Use separate PowerShell terminals for the API and frontend.

API:

```powershell
dotnet run --project .\api --launch-profile http
```

Confirm the API is ready:

```powershell
Invoke-RestMethod http://localhost:5235/api/health
```

Expected response: `status` is `ok`.

Frontend:

```powershell
Push-Location .\frontend
pnpm dev
Pop-Location
```

Open `http://localhost:3000`.

## Worker

The worker has a separate User Secrets store. Configure it with the same categories of
local database and Plaid values before running it:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<local PostgreSQL connection string>" --project .\worker
dotnet user-secrets set "Plaid:ClientId" "<Plaid sandbox client ID>" --project .\worker
dotnet user-secrets set "Plaid:Secret" "<Plaid sandbox secret>" --project .\worker
dotnet user-secrets set "Plaid:Environment" "sandbox" --project .\worker
dotnet user-secrets set "Plaid:ClientName" "Cardui" --project .\worker
dotnet run --project .\worker
```

The worker performs one synchronization pass and exits. See `worker/README.md` for
deployment scheduling notes.

## Verification commands

```powershell
dotnet test .\Cardui.sln --no-restore
```

```powershell
Push-Location .\frontend
pnpm test
pnpm lint
pnpm build
Pop-Location
```

The original-MVP manual walkthrough is in
[`docs/Original-MVP-Acceptance-Checklist.md`](docs/Original-MVP-Acceptance-Checklist.md).

## Continuous integration

`.github/workflows/ci.yml` runs on pull requests and on pushes to `main`. It uses
the .NET SDK pinned in `global.json`, Node.js 22, and the pnpm version pinned in
`frontend/package.json`.

- API tests: `dotnet test ./Cardui.sln --configuration Release`
- Frontend: `pnpm test` and `pnpm lint` from `frontend/` after
  `pnpm install --frozen-lockfile`

The workflow does not start PostgreSQL, apply migrations, or run `pnpm build`.

## Stop local services

```powershell
docker compose stop
```

This preserves the PostgreSQL volume. Do not use `docker compose down --volumes` unless
you explicitly intend to delete local database data.
