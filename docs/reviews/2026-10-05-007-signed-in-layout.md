# Signed-in layout

Date: October 5, 2026

## Increment

Home, Activity, Accounts, and record detail keep the light shell. This
pass fixes the cramped Home layout, shortens the path to the first
transaction and the account list, and makes a row and a panel say what
they are.

## Changes

- Home keeps net worth and this month in one column until the page is
  wide enough for two cards. Inside a card, the three monthly amounts
  stay stacked until that card can hold them, and assets and liabilities
  sit under the chart until the card can place them beside it.
- On a phone, Activity keeps search on screen. Account, category, and
  status sit behind Filters, with a count and a removable chip for each
  active choice. The wide layout still shows those controls in one row.
- A transaction row uses the merchant name when the bank sent one, and
  the stored name otherwise. Account and category sit under that title.
  The full statement stays in detail. A transfer or adjustment keeps one
  mark, without a second explanation under the amount.
- The Accounts chart is shorter, so the account list starts higher. The
  desktop add control says Add account. A phone still uses the plus icon.
- A top-level detail panel uses a close icon from 768px up and keeps the
  back arrow on a phone. Merchant history, change category, and add
  category use the back arrow because they are nested steps.

## Data changes

None.

## Agent verification

- `pnpm exec tsc --noEmit` and `pnpm lint` in `frontend/` passed.
- `pnpm test` in `frontend/` passed, including the list-title and filter
  count cases.
- Prettier was run on the files this increment changed.
- Did not click through the app. Signed-in routes need Clerk, and visual
  checks stay with Zach.

## Manual verification

Use the household you already have. No database changes.

1. Open Home and set the window to about 1024px wide.
   Expected: net worth and this month are stacked, not side by side.
   Income, spending, and difference are each fully visible. The chart is
   wide enough to read, and assets and liabilities do not squeeze it
   into a strip.
2. Widen Home until the two cards sit side by side, then narrow it to a
   phone.
   Expected: long amounts stay readable in either arrangement. On a
   phone the cards remain stacked.
3. Open Activity on a phone.
   Expected: search is visible. Account, category, and status are behind
   Filters. The first transaction is higher on the screen than before.
4. Open Filters, choose an account, a category, and Pending, then close
   the panel.
   Expected: the button shows Filters (3). Each choice appears as a chip
   that can be removed. Reset clears them. Search still filters on its
   own.
5. Widen Activity to a laptop.
   Expected: search, account, category, status, and Reset sit on one
   row. The Filters button is gone.
6. Find a bank transaction with a long statement, and a transfer.
   Expected: the row shows a short name when the bank sent one, with the
   account and category underneath. The transfer has one Transfer mark.
   Opening it still shows the full statement.
7. Open Accounts on a laptop, then on a phone.
   Expected: the chart is shorter and the account list starts higher.
   The laptop header says Add account. The phone header keeps the plus
   icon.
8. On a laptop, open a transaction and an account. From the transaction,
   open history or change category.
   Expected: the detail panel closes with an X. The nested step uses a
   back arrow. On a phone, the detail panel itself still uses the back
   arrow.

## Approval

Zach approved this increment on October 5, 2026.

## Pending decision

Keyboard focus in detail panels, Home category links that keep the
reporting period, the Difference explanation, and manual-account
onboarding stay unstarted. Which of those to do next is the decision.
Plaid sync reconciliation tests stay paused while the UI plan is the
active local work.
