# Category targets

Date: October 5, 2026
Status: Approved 2026-10-06
PR:

## Increment

Targets is a setting in the account menu, at `/targets`. A household can set a monthly amount on a spending category, see what was spent and what is left, copy last month's amounts into a new month, and turn rollover on when leftover or overspend should move ahead. Income, transfers, and statement adjustments stay out of spent, the same way they stay out of Home.

## Changes

- A target is one amount for one spending category for one calendar month. Zero is a known target. A blank amount is not stored as zero. Income and transfers cannot hold a target.
- Spent uses the same posted-activity rules as Home. The current month runs through today. A past month is the full month. A later month has not started, so spent stays at zero. Pending transactions, transfers, and statement adjustments are left out. A refund reduces its own category. Another currency is left out and named. Uncategorized spending is shown and cannot be given a target.
- A credit-card payment stored as a transfer is not spent again. A payment stored in a spending category, such as a mortgage, still counts. There is no separate principal split.
- Remaining is the target plus any amount rolled in, minus spent in that category. Spending with no target is named and is not folded into remaining. An over amount is called over.
- Rollover is a switch labeled Rollover, with On or Off beside the track. It stays off until it is turned on for that category. Only the previous calendar month can roll in, and only the remaining from a category whose rollover was on. A missing month resets the carry. Overspend rolls in as a negative amount when rollover was on.
- A month that has not been started shows the nearest earlier month's amounts and rollover choices as a preview. Opening the page does not save them. Use these targets stores that preview. The first save of one category also stores the other copied amounts. Start fresh stores an empty month so a later visit does not copy them back. Leftover money is not copied with the amounts. It arrives only through rollover, and it does not skip a month.
- Removing a target leaves the spending and leaves the month started, so the removed amount is not copied back.
- Targets stays off the phone tab bar. The path is `/targets`.

## Data changes

None until the migration is applied, and none from opening the page after that. Saving a target, using a preview, starting fresh, or removing a target writes the month. It does not change transactions, income, bills, debts, or account balances.

The migration file `20261006031630_AddCategoryTargets` is in the tree and has not been applied. Do not add it again. Apply it with:

```powershell
dotnet ef database update --project .\api --startup-project .\api
```

That adds `CategoryTargetMonths` and `CategoryTargets`. A household has one row per started month. A month has one target per category. The amount is stored in cents. Deleting the household deletes its months. Deleting a category deletes its targets.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~CategoryTarget"`: 20 passed. That covers a rolled chain, rollover left off, a missing month resetting the carry, overspend rolling in as a negative amount, a known zero, a missing target, uncategorized spending, copy-forward keeping the amount separate from leftover money, the current month stopping today, a preview that is not saved, the first save copying the other categories and leaving the prior month, income and transfers refused, a negative amount refused, posted activity (a refund, a transfer card payment, a mortgage in a spending category, pending, a statement adjustment, another currency, and a later date), start fresh, an idempotent copy, a removed target staying removed, and another household left out.
- `node ./scripts/run-category-target-tests.mjs` in `frontend`: 8 passed. A missing target stays open, an over amount is named, spending with no target is not called remaining, a preview names its source and does not skip a month, the current month stops today, rollover money and the choice to roll ahead are separate, and a blank amount is not zero.
- `pnpm exec tsc --noEmit` in `frontend` passed.
- Prettier was applied to the targets files.
- Did not click through the app. The new tables are not in the database yet.

## Manual verification

Approve the migration above, apply it, and restart the API before these steps. Income, bills, debts, and account balances should stay the same.

1. Open Targets from the account menu.
   Expected: The page is Targets, at `/targets`. It is not a fourth tab. Summary shows Target, Spent, and Remaining. The note says spent is posted spending, income and transfers are left out, and a transfer is not spent again. With no targets, Target and Remaining say No targets. Spent for this month matches Home, through today.
2. Set a Groceries target and leave rollover off. Save. Then clear the amount and try to save.
   Expected: Remaining is the target minus what was spent. Zero can be saved. A blank amount is not saved as zero.
3. Turn rollover on for Groceries and save.
   Expected: The control is a switch labeled Rollover. Off shows the word Off and the knob on the left. On shows the word On and the knob on the right. The row says it rolls into next month.
4. Open next month.
   Expected: The amounts are shown as a preview from this month, and the page says they are not saved yet. A category with rollover on includes what is left. A category with rollover off does not. Use these targets.
   Expected: The month is stored. Changing one amount does not change the earlier month.
5. On another new month, choose Start fresh and confirm.
   Expected: That month has no targets. Refreshing does not bring the earlier amounts back. If the previous month rolled money forward, the row names that amount and it is not part of Remaining until a target is set.
6. Remove a target and confirm.
   Expected: The spending stays. Refreshing does not put the target back.
7. On a phone-width window, change the month, use a preview, and open a category.
   Expected: The month control, Use these targets, and the category row are easy to tap. An over amount says over.

## Approval

Zach approved this slice on October 6, 2026, as built. Zach applied `20261006031630_AddCategoryTargets` the same day.

## Correction (October 6, 2026)

The migration is applied. Do not run `dotnet ef database update` for `AddCategoryTargets` again.
