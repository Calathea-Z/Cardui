# Frontend types

Date: October 4, 2026

## Increment

Gave each frontend shape one name and one home, and recorded that as a
standing rule so closed values stay unions.

## Changes

- API payloads moved from `frontend/lib/api/types.ts` into feature files
  under `frontend/lib/api/types/`. The index re-exports them, so existing
  imports still resolve.
- `source` and `provenance` are unions of the API's known values.
  CSV amount sign and date order are unions too. Manual account types
  come from `MANUAL_ACCOUNT_TYPES`, and the form labels have to cover
  every member.
- The dashboard view and widgets use `DashboardPageData`. The
  `SafeApiResult` alias is gone. Page loads use `PageLoadState`.
- The Cardui rule is in `AGENTS.md`, `frontend/AGENTS.md`, and
  `.cursor/rules/frontend-types.mdc`. It applies to every Cardui chat.
- The portable rule is in `~/.cursor/rules/typescript-types.mdc`. It
  applies in every project.

## Data changes

None.

## Agent verification

- `pnpm exec tsc --noEmit` in `frontend`: passed.
- `pnpm lint` in `frontend`: passed, no warnings.
- `pnpm test` in `frontend`: 23 passed, 0 failed.
- No database or data changes. Screens were not opened in a browser.
  The selects still offer the same choices.

## Manual verification

Zach approved and QA'd this increment on October 4, 2026. The checks below passed.

1. Add a manual account and switch the type between Cash, Investment,
   Credit card, and Loan.
   Expected: the opening-balance hint changes for a credit card or loan.
   The account saves.
2. Open that account and change the type again.
   Expected: the saved type is selected. Saving keeps the new type.
3. Start a CSV import and change how dates and positive amounts are read.
   Expected: Month first, Day first, Money out, and Money in still apply
   to the preview.
4. Open the dashboard.
   Expected: the same cards are still there.

## Pending decision

None for this increment. Phase 2 item 1 is still next: income sources,
take-home amount, cadence, next payment date, contributor, and reliability.
