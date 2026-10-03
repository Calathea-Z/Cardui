# Development setup and original-MVP acceptance

Date: October 2, 2026

## Increment

Established a repeatable Windows development setup and a manual acceptance walkthrough
for the existing account, transaction, balance, dashboard, and synchronization baseline.

## Changes

- Added a repository-level setup guide for PostgreSQL, API User Secrets, checked-in
  migrations, frontend environment URLs, local startup, the one-shot worker, verification,
  and safe shutdown.
- Replaced the generated frontend README with Cardui-specific configuration and commands.
- Added an original-MVP acceptance checklist with explicit result labels, expected
  outcomes, data-change disclosures, known Phase 0 gaps, and evidence-handling guidance.
- Kept all credential examples as unusable placeholders and directed secrets to .NET
  User Secrets.

## Agent verification

- Confirmed the installed .NET SDK is 10.0.301, matching `global.json`.
- Confirmed the installed pnpm version is 10.33.0, matching `frontend/package.json`.
- Confirmed `docker compose config --quiet` accepts the checked-in Compose configuration.
- Ran `git diff --check`; it passed.
- Checked the edited documentation for IDE diagnostics; none were reported.
- Application tests and builds were not run because this increment changes documentation
  only.

## Manual verification

Zach reviewed the root setup guide, frontend README, and original-MVP acceptance
checklist on October 2, 2026 and confirmed they are accurate.

No application or database data changed during this increment.

## Remaining considerations

- The full original-MVP acceptance walkthrough still needs to be executed with controlled
  Plaid sandbox data.
- Plaid configuration remains mandatory at API startup.
- Dashboard spending summaries, consistent financial totals, and CI remain later Phase 0
  increments.
