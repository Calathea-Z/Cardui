# Cardui

Cardui is a personal financial recovery application. The repository contains:

- `api/`: .NET 10 HTTP API and EF Core migrations.
- `frontend/`: Next.js 16 / React 19 web application.
- `worker/`: one-shot scheduled Plaid synchronization worker.
- `tests/Cardui.Tests/`: .NET unit tests.

The current application requires PostgreSQL to start. Plaid credentials are required
only to link or sync a bank. Keep all database and Plaid credentials in .NET User
Secrets or environment variables; do not add them to checked-in settings or documentation.

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
on `127.0.0.1:5433`. It does not listen on other network interfaces.

### 2. Configure the API

Set the API's local configuration without writing secrets into the repository:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<local PostgreSQL connection string>" --project .\api
dotnet user-secrets set "Clerk:Issuer" "<Clerk Frontend API origin, such as https://your-instance.clerk.accounts.dev>" --project .\api
```

Plaid credentials are optional. Without `Plaid:ClientId`, `Plaid:Secret`, and
`Plaid:Environment`, the API starts and manual accounts, transactions, and CSV
import work. Bank linking and sync stay off until all three are set. A partial
set stops startup. Add them when bank linking should work:

```powershell
dotnet user-secrets set "Plaid:ClientId" "<Plaid sandbox client ID>" --project .\api
dotnet user-secrets set "Plaid:Secret" "<Plaid sandbox secret>" --project .\api
dotnet user-secrets set "Plaid:Environment" "sandbox" --project .\api
dotnet user-secrets set "Plaid:ClientName" "Cardui" --project .\api
```

`Clerk:Issuer` is the https origin of the Clerk Frontend API. The API uses it to
download Clerk's public signing keys and check session tokens. Do not put the
Clerk secret key in the API configuration. An optional `Clerk:JwtPublicKey`
User Secret can hold Clerk's PEM public key when the API should verify tokens
without calling Clerk. API routes other than `/api/health` require that
session. Financial reads and writes use the household id resolved on the
server for the signed-in owner.

Plaid access tokens are encrypted with ASP.NET Data Protection before they are
stored. Local API and worker processes share the gitignored
`.data-protection-keys` directory. Production must set
`DataProtection:KeysPath` and `DataProtection:CertificatePath`. Set
`Plaid:WebhookUrl` to the public `https` URL of `POST /api/plaid/webhook`
when Plaid should report a revoked bank connection. Production also requires
public `https` values for `AllowedHosts`, `Cors:AllowedOrigins`, and
`Clerk:AuthorizedParties`.

Use the local database values defined by `docker-compose.yml` when constructing the
connection string. Do not paste the resulting connection string into source files,
documentation, screenshots, or logs.

Restore packages and apply the checked-in migrations:

```powershell
dotnet restore .\Cardui.sln
dotnet ef database update --project .\api --startup-project .\api
```

Setup applies the checked-in migrations. New migrations are generated from
model changes after Zach approves the `dotnet ef` command.

### 3. Configure the frontend

Create `frontend/.env.local` with the local API URLs and the Clerk keys for this
application. Keep the Clerk secret out of source control. The publishable key
is public, but it still belongs in the local env file rather than the
repository.

```powershell
@"
API_BASE_URL=http://localhost:5235
NEXT_PUBLIC_API_BASE_URL=http://localhost:5235
NEXT_PUBLIC_CLERK_PUBLISHABLE_KEY=<Clerk publishable key>
CLERK_SECRET_KEY=<Clerk secret key>
NEXT_PUBLIC_CLERK_SIGN_IN_URL=/sign-in
NEXT_PUBLIC_CLERK_SIGN_UP_URL=/sign-up
"@ | Set-Content .\frontend\.env.local
```

This machine may already have a Clerk development application from `clerk init`.
Those values stay in the gitignored env file. Claim that application later, if
you want it on your Clerk account, from `frontend/` with `pnpm dlx clerk@latest auth login`.

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

The worker has a separate User Secrets store. Configure the database before
running it. Plaid credentials are required only when connected items should
sync. Without them, the worker starts and skips bank sync. If connected items
exist and credentials are missing, it exits with code 1. Configure Plaid with
the same values as the API when sync should run:

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
dotnet build .\worker\worker.csproj --configuration Release
```

```powershell
Push-Location .\frontend
pnpm test
pnpm lint
pnpm format:check
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
- Worker build: `dotnet build ./worker/worker.csproj --configuration Release`
- Frontend: `pnpm test`, `pnpm lint`, `pnpm format:check`, and `pnpm build`
  from `frontend/` after `pnpm install --frozen-lockfile`

The frontend production build sets `API_BASE_URL` and `NEXT_PUBLIC_API_BASE_URL`
to `http://localhost:5235`. It also sets placeholder Clerk keys so the sign-in
shell can compile. Those placeholders are not a Clerk application. This workflow
does not deploy that build. It does not start PostgreSQL or apply migrations.

## Stop local services

```powershell
docker compose stop
```

This preserves the PostgreSQL volume. Do not use `docker compose down --volumes` unless
you explicitly intend to delete local database data.
