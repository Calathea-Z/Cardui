# Recurring bill suggestions

Date: October 5, 2026

## Increment

Bills can notice a payment that looks regular and ask before it becomes a bill. Three similar payments, with a steady gap, show up as a suggestion. Saving the bill is the confirmation. Leaving it out hides that pattern. A suggestion is never stored as a bill on its own.

## Changes

- A suggestion needs three payments in the last three years. The gap has to match every week, every two weeks, twice a month, every month, every quarter, or every year. One missed gap is not suggested. The latest payment has to still look recent for that schedule.
- The amount is one typical payment. Each charge has to stay within a dollar of that amount, or within 10 percent when that is larger. A monthly total is not stored.
- Transfers, income, pending rows, archived rows, balance reconciliations, and amounts in another currency are ignored.
- The merchant name is the pattern. The same name with different spacing or letter case is one pattern. When every charge left one account, that account is filled in. Otherwise the suggestion has no account.
- Suggested from activity sits above the saved bills. A suggestion card uses the muted fill, and the row says Suggestion next to the schedule. A saved bill stays on the white card. Add as bill opens the form with the amount, schedule, date, and account filled in. Essential or flexible stays unset until the person chooses it. Not a bill asks first, then hides that pattern. No bill is created.
- A saved bill with the same name, or one saved from that suggestion, hides the pattern. Deleting that bill brings the suggestion back. A later edit keeps the link to the pattern.
- `20261005191328_AddRecurringSuggestionConfirmation` adds nullable `SuggestionKey` on `Obligations` and the `ObligationSuggestionDismissals` table. The dismissal key is unique per household. Deleting the household deletes those rows.

## Data changes

`20261005191328_AddRecurringSuggestionConfirmation` is applied. Existing bills gained an empty `SuggestionKey`. The dismissals table is empty. Accounts, transactions, and bill amounts are unchanged.

The checklist below can add a bill or hide a suggestion in the household you already have. Not a bill does not change an account balance. Add as bill creates a bill only after you save the form.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~RecurringSuggestionsTests|FullyQualifiedName~ObligationSuggestionServiceTests|FullyQualifiedName~ObligationRulesTests|FullyQualifiedName~ObligationsServiceTests"`: 27 passed. That covers a monthly payment as one amount, weekly through yearly schedules, twice a month, a steady 14-day gap, varying amounts, a missed gap, a stopped pattern, transfers, income, pending, archived, reconciliations, another currency, mixed accounts, a bill or dismissal that hides the pattern, household isolation, saving the pattern key, an edit that keeps the key, delete bringing the suggestion back, and a blank dismissal.
- `pnpm exec tsc --noEmit` in `frontend` passed.
- Prettier check passed on the bills files that this increment changed.
- `dotnet ef database update` applied `20261005191328_AddRecurringSuggestionConfirmation`. Release was used so the build would not need the debug `api.exe` lock. The migration adds `SuggestionKey` and `ObligationSuggestionDismissals` only.
- Did not click through the app.

## Manual verification

Restart the API before these steps so it loads this build. The database already has the new column and table. The API that is running now is the older build. These steps use the household you already have. Income and the dashboard should stay the same.

If activity does not already have three similar payments on a steady gap, the suggestion section stays hidden. That is the expected empty case. Saved bills stay as they are.

1. Open Bills from the account menu.
   Expected: saved bills are unchanged and stay on a white card. When a pattern matches, Suggested from activity appears above them. Each suggestion card is the muted fill and says Suggestion next to the schedule. It shows one payment, the account or No account, and the next due date. It does not say Essential or Flexible, and there is no monthly figure.
2. Choose Add as bill on a suggestion. Leave Essential or flexible on Select and save.
   Expected: the save does not stick. The message says to choose essential or flexible. The suggestion is still there.
3. Choose Essential or Flexible and save.
   Expected: the suggestion is gone. The card is a saved bill with that amount as each payment. The account balance is unchanged.
4. Remove that bill and confirm.
   Expected: the bill is gone. The same suggestion comes back. The account balance is unchanged.
5. Choose Not a bill on that suggestion and confirm.
   Expected: the suggestion is gone. No bill is created. Refresh Bills and it stays gone.
6. On a phone-width window, open Add as bill, then a choice list and a date.
   Expected: the form is a full-screen sheet. The choice and the date use the app's list and calendar. Escape closes an open list first, then the sheet.

## Approval

Zach QA'd and approved this increment on October 5, 2026. Suggestions from his activity behaved as described, and the muted suggestion card was the separation he wanted.
`20261005191328_AddRecurringSuggestionConfirmation` was applied the same day.

## Pending decision

Overlapping worker and manual sync remains open. Phase 2 item 4 is next: debts, with an optional linked account, dated balance, APR, minimum, due date, revolving or installment type, and unknown terms left unknown.
