# Phase 1 sign-in decision

Date: October 3, 2026

## Increment

Locked how the single household owner signs in. No schema, migration, or
application code was added.

## Decision

Clerk Hobby is the Phase 1 sign-in provider.

- One Clerk user is the signed-in owner of one household.
- Contributors are household facts. Partner invitations are out of this phase.
- Household ownership stays in PostgreSQL.
- The API derives identity from a verified Clerk session token and looks up
  that user's household on the server.
- The frontend sends the short-lived token to the API because the Next.js app
  and the API are on different origins.
- Clerk stores the owner's email, name, and session data. Financial records
  stay in this app's database.
- Multifactor authentication, passkeys, a configurable session lifetime, and
  removal of Clerk branding wait until a Clerk Pro upgrade.
- The Hobby session lifetime stays fixed at 7 days.
- Two household accounts are tested with a second browser profile, because
  Hobby does not keep two accounts signed in at once in the same browser.

API-owned email and password was the alternative if the owner's email had to
remain only in this database. Zach chose Clerk Hobby on October 3, 2026.

## Changes

- Recorded the decision on Phase 1 in
  `docs/roadmap.md`.

## Agent verification

- Documentation-only change. Application tests, builds, and the worker were
  not run.
- Compared Clerk Hobby limits with Clerk's pricing page on October 3, 2026:
  free up to 50,000 monthly retained users; multifactor authentication and
  custom session duration require Pro.

No application or database data changed.

## Manual verification

No app behavior changed. Zach approved this written decision on October 3, 2026.

## Remaining considerations

- The next increment is Clerk sign-in in the frontend, verification of the
  session token in the API, and the household-owner model. Zach still runs
  migration generation. That increment has not started.
- Existing personal data is not yet assigned to an owner.
