# Clerk sign-in and household owner

Date: October 3, 2026

## Increment

Added Clerk Hobby sign-in for the household owner, API verification of the
Clerk session token, and the household-owner model. Zach generated and
applied `AddHouseholdOwner`. Existing financial data is not scoped or
reassigned.

## Changes

- Installed `@clerk/nextjs` and protected every page except sign-in and
  sign-up. Signed-in pages keep the existing app shell. The desktop sidebar
  stays on screen, and the account row shows the email in the sidebar's own
  light text beside the Clerk button. On a narrow window the account button
  is in the header. The mobile menu is a fixed overlay and does not scroll
  past the account row. Clerk's menu and sign-in card use the site's dark
  surface, light text, and green primary button. Clerk branding and the
  development-mode label remain.
- The frontend sends the short-lived Clerk session token to the API.
- The API verifies that token against `Clerk:Issuer` (or an optional PEM
  public key) and the configured frontend origins. It does not store the
  Clerk secret.
- Added `Household`, with one unique Clerk user id per household. The first
  authenticated call to `POST /api/households/current` creates "My household"
  for that owner. A later call returns the same household.
- Recorded setup for the Clerk keys and `Clerk:Issuer` in `README.md`. CI
  builds the frontend with placeholder Clerk keys that are not a real
  application.

## Agent verification

- `dotnet test .\Cardui.sln -c Release`: 70 passed, 0 failed. New tests cover
  one household per owner and rejection of a token with the wrong origin,
  wrong signing key, or expired lifetime.
- `pnpm lint` in `frontend`: passed.
- `pnpm build` in `frontend`, with the CI placeholder Clerk keys set in the
  process environment: passed.
- Opened `http://localhost:3000`. The app redirected to `/sign-in`, and the
  Clerk sign-in form was visible on the dark page.
- Saved `Clerk:Issuer` in the API User Secrets from the local publishable key.
  The value is not in the repository.
- Did not generate the EF Core migration. Did not run the worker.

No existing financial rows were changed. A household row is created when a
signed-in owner first hits `POST /api/households/current`.

## Manual verification

Zach generated `AddHouseholdOwner`, created a Clerk account from the local
sign-in page without using the Clerk dashboard, and on October 3, 2026 said
the account control and mobile menu were good enough.

Confirmed by Zach:

1. Unsigned visitors reach the Clerk sign-in page. The development-mode
   banner is expected.
2. After sign-in, the dashboard loads and there is no household error banner.
3. Existing accounts and transactions still appear. They are not assigned to
   the new household yet.
4. The desktop account row stays on screen. The mobile account button is in
   the header. The mobile menu does not scroll past the account row.
5. Clerk's menu is dark enough to use. Clerk branding and the orange
   development-mode label remain.

Do not commit `frontend/.env` or `frontend/.env.local`.

Zach approved this increment on October 3, 2026.

## Remaining considerations

- Accounts, transactions, summaries, jobs, and other endpoints still accept
  unauthenticated calls. Household isolation is the next increment. Start it
  in a new chat from `AGENTS.md` and this review.
- Existing rows do not have an owner yet.
- Hobby keeps a 7-day session, has no multifactor authentication, and shows
  Clerk branding. The Clerk application name is still the default until it is
  renamed in Clerk.
- Claim the local Clerk application from `frontend` with
  `pnpm dlx clerk@latest auth login` when you want it on your Clerk account.
