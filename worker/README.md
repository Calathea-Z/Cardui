# Cardui Daily Sync Worker

One-shot worker for scheduled Plaid account and transaction syncs.

The worker:

- Loads connected Plaid items from PostgreSQL.
- Runs the existing account and transaction sync flow for each item.
- Logs per-item success or failure.
- Exits with code `1` if any item fails.

## Local Run

Set the same environment variables used by the API, then run:

```powershell
dotnet run --project worker
```

Required configuration:

- `ConnectionStrings__DefaultConnection` or `DATABASE_URL`
- `Plaid__ClientId`
- `Plaid__Secret`
- `Plaid__Environment`
- `Plaid__ClientName`

## Railway Cron

Create a separate Railway service for this worker and point it at
`worker/Dockerfile` with the repository root as the build context.

Use a daily cron schedule, for example:

```text
0 0 9 * * *
```

That runs once per day at 9:00 UTC. Adjust the hour for your preferred sync
time.
