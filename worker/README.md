# Tortoise sync worker

One-shot worker for Plaid account and transaction sync.

The worker is implemented for local execution. It has not been deployed, and nothing schedules it.

The worker:

- Loads connected Plaid items from PostgreSQL.
- Skips an item that has no household.
- Runs the existing account and transaction sync flow for each item.
- Skips an item when a sync is already running. That skip is not a failure.
- Logs per-item success or failure.
- Exits with code `1` if any item fails.

Database setup, shared configuration, and application startup are in the repository root [`README.md`](../README.md).

## Local Run

Run this from the repository root. The worker uses its own user secrets (`UserSecretsId` in `worker/worker.csproj`). `worker/Properties/launchSettings.json` sets the environment to Development so those secrets are loaded on `dotnet run`.

```powershell
dotnet run --project .\worker
```

The worker needs a database connection in its user secrets, an environment variable, or `DATABASE_URL`:

- User secret: `ConnectionStrings:DefaultConnection`
- Environment variable: `ConnectionStrings__DefaultConnection` or `DATABASE_URL`

The root README has the local PostgreSQL setup. Set the worker's connection string to that same database.

Plaid credentials are optional. Without `Plaid:ClientId`, `Plaid:Secret`, and `Plaid:Environment`, the worker starts and skips bank sync. If connected items exist and those values are missing, it exits with code 1. Set the same three values, plus optional `Plaid:ClientName`, when sync should run. A partial set stops startup. In environment variables, those names are `Plaid__ClientId`, `Plaid__Secret`, `Plaid__Environment`, and `Plaid__ClientName`.

The worker uses the same Data Protection key directory as the API. A deployment must set `DataProtection:KeysPath` and `DataProtection:CertificatePath` on both. Leave `DataProtection:ApplicationName` set to `Cardui`.

## Future Deployment — Railway Cron

These instructions are for a later deployment. No Railway service is configured.

Create a separate Railway service for this worker and point it at `worker/Dockerfile` with the repository root as the build context.

Railway uses a five-field crontab expression in UTC. `0 9 * * *` is an example of a potential daily schedule, not an active production configuration. That expression would run once per day at 9:00 UTC. Choose the hour when a schedule is actually created.
