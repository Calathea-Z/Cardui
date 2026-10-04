# Frontend cleanup

Date: October 4, 2026

## Increment

Removed unused frontend code, aligned shared file names and formatting
with the existing Prettier config, and pointed merchant history at the
shared currency formatter.

## Changes

- Deleted unused UI files `badge.tsx` and `table.tsx`, and the unused
  `CardFooter` export.
- Deleted API client methods with no caller, including health, category
  by id, group and subgroup by id, transaction by id, the server copies
  of merchant history and transaction updates, and the browser group
  client. Removed the DTO types that only those methods used.
- Deleted `AddSubGroupDrawer`. No screen opened it, and it was the only
  caller of `createSubGroup`.
- Renamed shared components to kebab-case: `app-shell.tsx`,
  `page-api-error-banner.tsx`, and `api-unavailable-banner.tsx`.
- Feature barrels now export only `CategoriesClient` and
  `TransactionsClient`.
- Merchant history totals use `formatCurrency`. A period whose
  transactions share one currency uses that code. A mixed period uses
  USD.
- Turned off `allowJs`. Removed the unused `hooks` alias from
  `components.json`.
- Added `pnpm format` and `pnpm format:check`. CI runs the check after
  lint. Prettier was applied across `frontend/`.
- Removed an unused `cn` import in the mobile shell that lint reported.

Large components were left as they are. The audit said to split them
the next time those screens change.

## Agent verification

- `pnpm test` in `frontend`: 18 passed, 0 failed.
- `pnpm lint` in `frontend`: passed, no warnings.
- `pnpm exec tsc --noEmit` in `frontend`: passed.
- `pnpm build` in `frontend`: passed. Next.js compiled and type-checked.
- No database or data changes.

## Manual verification

Please check these, or waive them.

1. Open Transactions, open a transaction, and open its merchant history.
   Expected: average and total amounts show a currency. A period in one
   currency uses that code. A mixed period shows USD.
2. Open Categories.
   Expected: the category list and form still work. There is no control
   for creating a sub-group, which matches the previous screens.
3. Open the dashboard and Accounts.
   Expected: the shell, navigation, and page error banner look the same.
   Those files were renamed and reformatted.

## Pending decision

Frontend governance rules are proposed in the chat summary. They are
not in `AGENTS.md` yet.
