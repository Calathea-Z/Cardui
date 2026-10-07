# Suggested matches

Date: October 7, 2026
Status: Approved 2026-10-07
PR:

## Increment

Follow a connected account now starts with up to three suggestions. Each one names why it was offered, such as the last digits, a shared word, or a close balance. The person still confirms before anything is linked. "Choose another account" lists every eligible account. "None of these" closes the step. A debt that already names an eligible account still goes straight to confirm.

## Decision

A suggestion is a pure rule over accounts the debt is already allowed to follow. A revolving debt is matched with a credit card. An installment debt is matched with a loan. An account of the other kind stays in the full list and is not suggested.

A match needs one of these:

- The account's last digits appear in the debt name as their own number. A digit inside a longer number does not count, and a mask shorter than two digits does not count.
- A word from the account name, its official name, or the institution appears as its own word in the debt name. Words shorter than three letters are skipped, as are "card", "credit", "loan", "account", "bank", "and", "the", and "for". The spelling shown is the debt's.
- The latest dated snapshot is close to the debt's balance. Close means the amounts differ by at most $50, or by 5 percent of the larger amount when that is more. A balance with no date is not a signal. The figure is the bank's amount, including a credit, not the $0 a followed card would count.

The last digits rank first, then more shared words, then a smaller gap. That order is not shown. At most three accounts are suggested. An account with no signal is left out of the suggestions and stays in the full list. When nothing matches, the full list is the first step. Dismissing the suggestions is not stored.

The balance reason uses the same money text as the rest of the app, so a whole dollar amount shows cents: "Balance within $38.00 of yours". The same amount says "Same balance as yours".

## Changes

- `DebtAccountMatcher` ranks the suggestions. The follow-account response adds `suggestionOrder` and `reasons`. The order is 1, 2, or 3, and the screen does not show it.
- The follow steps show the suggestions first. Choosing one opens the existing confirm step. Back from confirm returns to the step the person came from.
- A reference link to an eligible account still skips the suggestions.

## Data changes

No migration. The model matches `20261007162523_AddDebtAccountFollow`. Nothing is written when the suggestions are shown or dismissed. The local API process needs a restart before the new fields are on the follow-account response.

## Agent verification

- `dotnet ef migrations has-pending-model-changes --project .\api --startup-project .\api --configuration Release`: no model changes since the last migration.
- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~Debt"`: 48 passed. That covers the rank, a loan left out of a card's suggestions, a digit inside a longer number, a word inside a longer word, the $50 and 5 percent window, the debt's spelling, and the service returning one suggestion while still listing the other accounts.
- `node ./scripts/run-debt-form-tests.mjs` in `frontend`: 8 passed. That covers the reason sentences and which step Back returns to.
- `eslint` on the changed frontend files passed.
- Did not click through the app.

## Manual verification

Restart the API so the follow-account response includes the suggestions. Following still changes a debt only when the person confirms. Account balances stay as they are.

1. Open a debt that does not already name an eligible account, and whose name includes a connected card's last four digits or a word from that card or its institution. Choose "Follow a connected account".
   Expected: up to three accounts, each with a reason such as "Ends in 4821", "Name includes Chase", or "Balance within $38.00 of yours". No score. A loan is not suggested for a card, and a card is not suggested for a loan.
2. Choose one suggestion, then use Back.
   Expected: the confirm step, then Back returns to the suggestions. The debt is not following yet.
3. Choose "Choose another account", then use Back.
   Expected: every connected card and loan this debt can follow, including accounts that were not suggested. Back returns to the suggestions.
4. Choose "None of these".
   Expected: the steps close. The debt is unchanged. Opening follow again shows the same suggestions.
5. On a debt that already names an eligible account, choose "Follow this account".
   Expected: the confirm step, with no suggestions in between.
6. On a debt whose name and balance are not close to any account, choose "Follow a connected account".
   Expected: the full list, with no empty suggestion step. Skip this if every debt has a suggestion.

## Approval

Zach approved this increment on October 7, 2026, as built.

## Pending decision

None in this slice. After approval, the next roadmap item is linked-debt item 5, credit limit. Domain folders is still awaiting review in `2026-10-07-004-domain-folders.md`.

## Correction

October 7, 2026: Domain folders was approved the same day. See the Approval section of `2026-10-07-004-domain-folders.md`.
