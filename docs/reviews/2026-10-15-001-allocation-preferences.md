Date: 2026-10-15
Status: Approved 2026-10-15
PR:

# Allocation preferences

## Increment

The rest of Phase 3 item 6. A household can save how extra variable pay and a windfall should be split, then review that split. The rules are preferences. Cardui does not move money, pay a debt, or change an account balance. Temporary Plan cash scenarios stay temporary.

## What you see first

The primary question is how extra variable pay and a windfall should be treated. The next action is **Save allocation**, beside the page title. Variable income and Windfall are the two rules under it. Each rule's review, including sources that are left out, sits with that rule.

On a phone the title and save button stack, and the two percent fields stack. From 640px up the percent fields sit side by side. The page stays one column at `max-w-3xl`.

## Decision

- Allocation is a setting at `/allocation`, in the account menu after Savings. It is not a Plan tab. Nothing saved here changes Plan.
- Variable income is the amount above typical pay on a source marked Variable, when a strong payment is recorded. Steady and uncertain pay stay whole and are not listed.
- A variable source in another currency is named and left out. A variable source with no strong amount above typical is named and not split.
- A windfall rule is the same two percents. The review states the percents and does not invent a dollar amount. A windfall tried on Plan still stays in cash.
- Each rule stores extra debt and savings as whole percents from 0 to 100. Together they cannot pass 100. The rest stays available and is not a third field.
- Before the first save, both rules show zero and the page says no rule is saved. Saving zero is a stored choice to leave the whole amount available.
- Cents round away from zero. When rounding would pass the amount, savings is reduced first, then debt. One cent at 50/50 reviews as extra debt.
- Saving creates no transaction, debt payment, or savings deposit.

## Changes

- Domain: `AllocationPreferenceRules` checks the percents, splits one amount, and lists variable sources for review.
- API: `GET /api/allocation` and `PUT /api/allocation`. The response includes the stored percents and the variable-source facts. The page applies the percents in the fields so an unsaved edit can be reviewed before save.
- Frontend: Allocation setting, account-menu entry, and copy that says money is not moved.
- Design: `docs/design/allocation.md`. Plan still does not save a temporary scenario.

## Data changes

`20261015193812_AddAllocationPreferences` adds `AllocationPreferences`, one row per household, with the four percents and `UpdatedAt`. Deleting the household deletes the row. No existing table changes. No balance, transaction, or debt row changes. The local database already had this migration applied.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~AllocationPreference"`: 7 passed. The reject rules cover a percent outside 0–100 and a pair that adds past 100. The split keeps $10.00 at 50/50, $10.00 at 33/33, and one cent at 50/50. Steady and uncertain pay are skipped. Another currency and a missing strong surplus are left out.
- `node ./scripts/run-allocation-tests.mjs`: 4 passed. `pnpm exec tsc --noEmit`: clean. Prettier and eslint on the changed Allocation files: clean.
- Not run: the full .NET suite, the full frontend suite, and the signed-in page. `dotnet ef database update` found the local database already up to date, including `20261015193812_AddAllocationPreferences`.

## Manual verification

Restart the API if it was running before this change. The migration is already applied locally.

1. Open the account menu and choose **Allocation**.
   Expected: Save allocation is the action. Variable income and Windfall each have Extra debt percent and Savings percent. Both start at 0 until a rule is saved, and the page says no rule is saved yet. It says Cardui does not move money.
2. Enter 40 and 40 for Variable income, and 20 and 30 for Windfall, then save.
   Expected: The page says the allocation was saved and that money was not moved. Variable income says 20 percent stays available. Windfall says 20 percent would be extra debt, 30 percent would be savings, and 50 percent would stay available.
3. If a source is marked Variable and has a strong payment above typical, in the planning currency:
   Expected: That source names the surplus and how this rule would split it. The sentence says the money is not moved. Accounts, Debts, and Savings balances are unchanged.
4. Enter 60 and 50 for either rule and save.
   Expected: The field asks again. The stored rule is unchanged.
5. Open Plan and try a one-time windfall.
   Expected: The preview still keeps that amount in cash. The saved allocation is unchanged.
6. Leave Allocation and open it again.
   Expected: The saved percents are still there.

## Approval

Approved by Zach on October 15, 2026.

## Pending decision

None in this slice. Phase 3 item 6 is complete. Phase 3 items 7–11 have not started.
