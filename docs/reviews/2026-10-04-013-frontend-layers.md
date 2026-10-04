# Frontend layers

Date: October 4, 2026

## Increment

Split the screens that mixed display, client state, and API calls.
Each screen keeps its controls. Writes and loads now sit in hooks, and
pure rules sit in camelCase modules.

## Changes

- CSV import: `useImportCsv` owns the wizard and the import requests.
  The step indicator, column mapping, sample, preview, and undo list
  are display components. File size, empty mapping, and step order stay
  in `importCsv.ts`. Amount and date text stay in `importCsvFormat.ts`.
- Transaction details: edit rules, including who can change a name and
  amount and how money in is stored, live in `transactionEntry.ts`.
  `useTransactionDetails` saves, archives, and restores, and it sends a
  waiting text edit when the detail unmounts.
- Merchant history: `useMerchantHistory` loads history once for the row
  and the sheet. The monthly count stays put when the chart range
  changes. Closing the sheet returns the range to monthly. Period
  currency lives in `merchantHistoryPeriod.ts`.
- Account detail: `useAccountDetail` saves, matches a statement,
  archives, and restores. The sheet renders the form and the actions.
- Household: `useHouseholdProfile` saves the profile and the
  contributors. `ContributorRow` renders one person.
- Add category: `useAddCategoryDrawer` holds the draft and creates the
  category. The sheet keeps its emoji, color, and group pickers.
  `CategoryForm` stays the categories-page editor. Both use
  `sortByOrderThenName`.
- Institutions: `groupAccountsByInstitution` is its own rule module.
- The layer rule is in `AGENTS.md`, `frontend/AGENTS.md`, and
  `.cursor/rules/frontend-layers.mdc`. It applies to every Cardui chat.

## Data changes

None.

## Agent verification

- `pnpm test` in `frontend`: 23 passed, 0 failed. The new case checks
  that group order is sort order, then name.
- `pnpm lint` in `frontend`: passed, no warnings.
- `pnpm exec tsc --noEmit` in `frontend`: passed.
- No database or data changes.

## Manual verification

Zach approved and QA'd this increment on October 4, 2026. The checks below passed.

1. Open Transactions, open a manual or imported transaction, and change
   the name, amount, and notes.
   Expected: the list updates, and a failed save restores the last saved
   values. A linked transaction still shows the original statement and
   hides name and amount editing.
2. Archive that transaction, then open Archived and restore it.
   Expected: archive asks first. Restore runs immediately.
3. Open History on a transaction that has merchant history. Change the
   chart range, then close the sheet and open it again.
   Expected: the row count stays on the monthly total while the range
   changes. The sheet opens on monthly again.
4. Import a small CSV through all four steps, then undo it.
   Expected: the steps, preview checks, and undo list behave as before.
5. Add a category from a transaction.
   Expected: the emoji, color, group, and sub-group pickers are unchanged.
   Save stays off until the form is filled.
6. Open an account, save a manual account, match a statement, and
   archive or restore.
   Expected: save and archive close the sheet. A statement match stays
   open and shows the new balance.
7. On Household, save the profile and add, edit, and remove a
   contributor.
   Expected: the profile refreshes, and the contributor list stays
   sorted by name.
8. Open Institutions.
   Expected: accounts still sit under the bank that synced them.

## Pending decision

None for this increment. Zach approved the user-level rule on October 4,
2026. It is in `~/.cursor/rules/react-layers.mdc` and applies in every
project. Phase 2 item 1 is next: income sources, take-home amount,
cadence, next payment date, contributor, and reliability.
