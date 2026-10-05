# Income scenarios and expected raises

Date: October 5, 2026

## Increment

An income source can record a typical net payment, an optional low and
strong payment, and expected raises that start on a date. When that date
arrives, the Income page asks whether the new amount is the pay they
receive now. Dashboard income is unchanged. Nothing is turned into a
monthly amount.

## Changes

- Typical pay is the amount the source already stored. Low and strong are
  optional. Low cannot be above typical. Strong cannot be below it.
- A raise is the new typical amount for one payment from its date. The
  current amount stays until the person answers. The date has to be on or
  after the next payment, and each raise needs its own date.
- On the household's calendar day for that date, the source asks: update
  typical pay, or remove the raise. Update stores the raise amount and
  removes that raise. Remove leaves the current amount. A later raise stays
  a note until its own date.
- If the new typical amount no longer fits low or strong, that scenario is
  cleared and the notice says so.
- Adding a raise to a source that already existed was saved as an update of
  a row that was not there. That insert is explicit now. The first save of
  a new source that includes a raise was already fine.

## Data changes

`20261005150122_AddIncomeScenariosAndRaises` adds nullable
`LowTakeHomeAmount` and `StrongTakeHomeAmount` on `IncomeSources`, and the
`IncomeRaises` table. Deleting a source deletes its raises. One source has
at most one raise on a date. Existing pay amounts stay. The local database
had this migration during QA: low and strong amounts saved before the raise
insert was fixed.

QA added and edited income sources in the existing household, including
Snooze. Accounts, transactions, and dashboard income were not part of this
change.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~IncomeSource"`:
  22 passed, 0 failed. That includes adding a raise onto a source that had
  none.
- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --no-restore`:
  215 passed, 0 failed.
- `node ./scripts/run-income-raise-review-tests.mjs` in `frontend`: 5 passed.
  The date uses the household time zone. A raise is due on its date. Low or
  strong is cleared only when it no longer fits.
- `pnpm exec tsc --noEmit` and eslint on the edited income files passed.
- The raw database error on step 6 of the checklist was reproduced and then
  fixed. Zach confirmed the save worked after that fix.

## Manual verification

Zach started the checklist. Saving a raise on an existing source failed
with a database concurrency error, then succeeded after the insert fix.
The rest of the checklist was not confirmed in this chat. Three follow-ups
came out of the session and are the next chat, not part of this increment.

## Approval

Zach approved this increment on October 5, 2026.

## Pending decision

None for this capture. The next chat is the follow-up from QA: friendly
errors, Sonner toasts, and grouping the income form so the payment is
obvious and low, strong, and raises are optional. Overlapping worker and
manual sync remains open and is not that chat.
