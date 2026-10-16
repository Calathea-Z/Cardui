Date: 2026-10-15
Status: Approved 2026-10-15
PR:

# Card purchases and payments

## Increment

Phase 3 item 9. A credit-card purchase counts in its category. The payment that settles it changes cash and the card balance, and it is not spending a second time. A mortgage, a card fee, and a bill autopay stay spending.

## What you see first

On Activity, the row still answers what the transaction was. A card payment shows a neutral amount and a **Card payment** mark, including when its category is still a spending category. A purchase stays in its category. A refund on the card still reduces that category.

Home's spending total and Spending targets use the same rule. The purchase is counted once. The payment is left out. A phone and a wide layout use the same row. The mark wraps with the other stamps.

## Decision

- A purchase on a card counts in its category, raises the card balance, and does not reduce cash. The same purchase on a cash account counts once and reduces cash.
- A card payment from cash reduces cash. The credit on the card reduces the amount owed. Neither is income or spending.
- The payment is recognized from its description, such as "payment thank you" or "credit crd autopay", even when the category is still a spending category. A credit on a card that reads as a payment is the same. A refund, a repayment, a card fee, a mortgage, and a bill autopay are not payments.
- New imports and transfer sync categorize a described card payment as a transfer when the category is still open. A category the person chose is left stored as they set it. Spending totals still leave the payment out.
- A repeated card payment is not suggested as a bill. A savings transfer is not a card payment.

## Changes

- Domain: `CardActivityRules` classifies a purchase, a refund, and a card payment, with the cash and debt change of each.
- Activity, Spending targets, and bill suggestions use that rule. Transfer sync can pair a card payment with its cash leg.
- Activity list: a card payment is marked and is not shown as spending or income.

## Data changes

None. No migration. Stored categories change only when transfer sync promotes an open card-payment description to Transfers.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~CardActivityRules|FullyQualifiedName~TransactionActivityCalculator|FullyQualifiedName~TransactionCategoryClassifier|FullyQualifiedName~RecurringSuggestions|FullyQualifiedName~CategoryTargetsServiceTests|FullyQualifiedName~DashboardService"`: 53 passed. A $40 grocery purchase on a card, a $40 payment from cash, and the $40 credit on the card count $40 of spending once. Cash falls by $40. The card balance ends where it started. A mortgage and an electric autopay still count. A repayment credit on a card stays a refund. The category-spending test was re-run after the cash payment was raised to $200: Groceries stay at $70, so that payment is not spent again.
- `node ./scripts/run-card-activity-tests.mjs`: 6 passed. `pnpm exec tsc --noEmit`: clean. Prettier and eslint on the changed Activity and Spending-target files: clean.
- The signed-in Activity page was not exercised.

## Manual verification

Restart the API if it was running before this change. There is no migration. Spending totals change for a card payment that was still in a spending category.

1. Open Activity and find a card payment, including one still categorized as spending.
   Expected: The amount is neutral and the row says Card payment. It does not read as income or as spending.
2. Find the purchase that payment settled, on the card.
   Expected: It stays in its category. Home's spending and Spending targets include that purchase once, not the payment as well.
3. Find a mortgage payment, a card fee, or a bill autopay.
   Expected: Those still count as spending. A refund on the card still reduces its category.
4. Sync or import a described card payment that is still uncategorized.
   Expected: It is stored as a transfer. A category you already chose is left as you set it, and the spending total still leaves it out.

## Approval

Approved by Zach on October 15, 2026. A heavy QA and UX pass for Phase 3 waits until items 10 and 11 are built.

## Pending decision

None in this slice. Phase 3 items 10 and 11 have not started. Detail panel keyboard focus stays tracked and is not scheduled. Unimplemented Planning UX proposals stay unscheduled.
