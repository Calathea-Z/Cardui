# Cardui sync worker

One-shot worker for scheduled Plaid account and transaction syncs.

The worker:

- Loads connected Plaid items from PostgreSQL.
- Skips an item that has no household.
- Runs the existing account and transaction sync flow for each item.
- Skips an item when a sync is already running. That skip is not a failure.
- Logs per-item success or failure.
- Exits with code `1` if any item fails.

## Local Run

The worker uses its own user secrets (`UserSecretsId` in `worker.csproj`).
`Properties/launchSettings.json` sets the environment to Development so those
secrets are loaded on `dotnet run`.

```powershell
dotnet run --project worker
```

Required configuration (worker user secrets, env vars, or `DATABASE_URL`):

- `ConnectionStrings__DefaultConnection` or `DATABASE_URL`

Plaid credentials are optional. Without `Plaid__ClientId`, `Plaid__Secret`, and
`Plaid__Environment`, the worker starts and skips bank sync. If connected items
exist and those values are missing, it exits with code 1. Set the same three
values, plus optional `Plaid__ClientName`, when sync should run. A partial set
stops startup.

The worker uses the same Data Protection key directory as the API. In
production set `DataProtection__KeysPath` and `DataProtection__CertificatePath`
on both.

## Railway Cron

Create a separate Railway service for this worker and point it at
`worker/Dockerfile` with the repository root as the build context.

Use a daily cron schedule. Railway uses a five-field crontab expression in UTC,
for example:

```text
0 9 * * *
```

That runs once per day at 9:00 UTC. Adjust the hour for your preferred sync
time.
