# Security hardening

Date: October 4, 2026

## Increment

Closed the security audit: the Windows Next.js advisory, Plaid token
protection, shared category edits, bank disconnect, and the local host,
header, and route gaps.

## Changes

- Next.js and `eslint-config-next` are 16.3.8. Axios is 1.20.0. `shadcn`
  is a dev dependency because only the CLI uses it.
- Stored Plaid access tokens must use the `dp:v1:` protector. Plaintext is
  rejected. The key ring application name is Cardui. API startup and the
  worker rewrap plaintext tokens. The log records a
  count, not the token. Production requires `DataProtection:KeysPath` and
  `DataProtection:CertificatePath`. Local keys go in the gitignored
  `.data-protection-keys` directory.
- System categories cannot be updated. The categories screen disables Edit
  on a system row. Category and sub-group name and key checks are limited
  to that household plus system rows.
- `DELETE /api/plaid/{id}` removes the bank login at Plaid and deletes the
  stored token. Accounts and transactions stay. Institutions asks for
  confirmation first.
- `POST /api/plaid/webhook` is anonymous and checks the Plaid signature
  before it acts. `Plaid:WebhookUrl` is optional. When it is set, link and
  sync register it. Production requires https for that URL.
- Postgres publishes `127.0.0.1:5433` only. `next dev` binds to
  `localhost`, because Next rewrites `127.0.0.1` to that name internally.
  A `DATABASE_URL` keeps `sslmode` and rejects an unknown query parameter.
- The API adds nosniff, frame denial, referrer, a locked-down content
  security policy, and HSTS outside Development. The frontend sends the
  same class of headers and allows Clerk and Plaid. The page proxy calls
  `auth.protect()` except on sign-in and sign-up.
- Plaid routes are rate limited. Anonymous requests get 401 except health
  and the webhook. The worker skips an item with no household. Sync and
  Plaid errors log an error type, not the exception object. The public
  sync response no longer includes the Plaid cursor.
- Production startup rejects `AllowedHosts` of `*`, and rejects localhost
  CORS origins and Clerk authorized parties.

## Data changes

- The Postgres container was recreated so the port bind takes effect. The
  `cardui_pgdata` volume was kept. One household is still present.
- API startup rewrapped 5 stored Plaid access tokens. A second startup
  rewrapped 0, so those values open with the current key ring. Token values
  were not printed.

## Database migration

`20261004142305_ScopeHouseholdCategoryNames` is applied. It drops the
global unique indexes on category Name and Key, and on sub-group Key and
(GroupId, Name). It adds partial unique indexes
`IX_Categories_System_Key`, `IX_Categories_System_Name`,
`IX_Categories_Household_Key`, `IX_Categories_Household_Name`,
`IX_SubGroups_System_Key`, `IX_SubGroups_Household_Key`,
`IX_SubGroups_System_Group_Name`, and
`IX_SubGroups_Household_Group_Name`. The database was not dropped.

## Agent verification

- `dotnet test .\Cardui.sln`: 137 passed, 0 failed.
- `dotnet build .\worker\worker.csproj`: succeeded, 0 warnings.
- `pnpm exec tsc --noEmit` and `pnpm lint` in `frontend`: passed.
- Prettier on the edited frontend files: passed.
- `GET /api/health` returned 200. `GET /api/accounts` without a session
  returned 401.
- Sign-in at `http://localhost:3000/sign-in` rendered the Clerk form.
  Opening `/institutions` while signed out redirected to sign-in. The dev
  log showed Clerk load and no content-security-policy block.
- Frontend response headers include the content security policy, nosniff,
  frame denial, and referrer policy. `X-Powered-By` is absent.
- Disconnect was not clicked while signed in. A bank sync was not sent to
  Plaid after the rewrap.

## Manual verification

Please check these, or waive them.

1. Sign in and open Institutions. Click Disconnect, then Cancel.
   Expected: the warning appears, then it clears, and the bank stays
   connected.
2. Click Sync now on one connected institution.
   Expected: sync succeeds. That confirms the rewrapped token still works
   at Plaid.
3. While signed out, open `http://localhost:3000/accounts` and
   `http://localhost:3000/institutions`.
   Expected: the sign-in page, not those screens.
4. On Categories, a system row has Edit and Delete disabled.
   Expected: you cannot open the editor for a shared category.
5. Optional, and it removes a bank login: on Institutions, click
   Disconnect, then Remove bank link.
   Expected: that institution card disappears. Its accounts and
   transactions remain.

## Proposed security rule

Not added yet. If you approve it, the next change adds
`.cursor/rules/security.mdc` and a short pointer in `AGENTS.md`.

```markdown
# Security

When adding or changing Cardui, keep these boundaries.

- Never log, return, or store a Plaid access token, client secret, or
  session token in plaintext. A stored Plaid token uses the data-protection
  protector. A value without that prefix is rejected.
- A financial query is limited to the signed-in household. System
  categories and sub-groups are not writable by a household. A custom name
  or key is unique inside that household, together with the shared system
  rows.
- An endpoint without a signed-in user is marked anonymous on purpose. A
  webhook checks its signature before it reads the body.
- Production refuses a wildcard host, a localhost CORS origin, a localhost
  Clerk authorized party, and data-protection keys that are not encrypted
  with the configured certificate.
- Local database and app servers bind to loopback. A critical dependency
  advisory that covers the version Cardui runs is upgraded in that change.
```

## Still open

- `Plaid:WebhookUrl` is empty, so Plaid is not calling the new webhook.
  Set it to the public `https` URL of `POST /api/plaid/webhook` when a
  public host exists.
- The frontend content security policy allows `'unsafe-inline'`, and
  `'unsafe-eval'` in development, because Next and Clerk need those.
- The category index migration is applied.
