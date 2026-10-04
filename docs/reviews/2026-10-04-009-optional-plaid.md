# Optional Plaid startup

Date: October 4, 2026

## Increment

The API and worker start without Plaid credentials. Manual accounts,
transactions, and CSV import do not need a bank connection. Bank linking and
sync stay off until the client id, secret, and environment are all set. A
partial set still stops startup. Zach approved this increment on October 4,
2026. The CSV import UX pass follows this increment and is not part of it.

## Changes

- Blank `Plaid:ClientId`, `Plaid:Secret`, and `Plaid:Environment` pass startup
  validation. `Plaid:ClientName` and `Plaid:DefaultClientUserId` keep their
  defaults and do not count as configuration.
- A partial set, an invalid environment, or a webhook URL without credentials
  still fails startup.
- Listing connected institutions reads the database and does not call Plaid.
  Creating a link token, exchanging a token, syncing, disconnecting, and
  webhook key lookup throw `PlaidNotConfiguredException` when credentials are
  absent. The response is 503 with detail "Bank linking is not configured."
- Startup logs whether Plaid is configured. The log names the environment and
  does not include the client id or secret.
- The worker starts without credentials and skips bank sync. If connected
  items exist and credentials are missing, it exits with code 1.
- `README.md` and `worker/README.md` describe Plaid as optional for startup.

## Data changes

None. Restarting the API rewrapped stored Plaid access tokens and changed 0
rows. The database was not migrated or dropped. Local user secrets were not
changed, so this machine still has Plaid credentials.

## Agent verification

- `dotnet test .\Cardui.sln`: 188 passed, 0 failed. New tests cover a blank
  configuration, a partial configuration, a webhook without credentials, a
  host that starts with no Plaid settings, and a host that refuses a client
  id alone.
- `dotnet build .\worker\worker.csproj`: succeeded, 0 warnings.
- The API was restarted on `http://localhost:5235` because the previous
  process locked the build. `GET /api/health` returned `status` `ok`. The
  startup log said Plaid is configured for the environment already in user
  secrets.
- The frontend was already listening on `http://localhost:3000`. The sign-in
  page title is Tortoise.
- Did not commit.

## Manual verification

Zach checked this on October 4, 2026, with local Plaid secrets still set.
The screen stayed the same. The blank-credential startup, the 503 when bank
linking is called without credentials, and the worker exit when connected
items exist without credentials were covered by the tests above.

1. Confirmed. The signed-in app still works, and the browser tab title is
   Tortoise.
2. Confirmed. Accounts load, including linked institutions.
3. Confirmed. Add account still offers Enter manually and Connect with Plaid.
4. Confirmed. Transactions still lists existing rows, with Import CSV and
   Add transaction present.
