# Accounts action menu

Date: October 5, 2026

## Increment

The more-options menu on Accounts was opening under the sidebar. The labels were cut off, so Refresh all accounts and Manage institutions could not be used. The menu now stays in the page column.

## Changes

- The menu lines up with the right edge of the more-options button. When that would run under the sidebar or off the left of the screen, it shifts right until the whole menu fits.
- The menu is drawn above the page, so the sidebar does not cover it. Escape or a press outside closes it, and focus returns to the button.
- The account menu opened from the email closes on a press outside that list, or on Escape. The account name still toggles it. Opening the list focuses Income, and Tab moves through the other destinations. The focused row is marked.

## Data changes

None.

## Agent verification

- `node ./scripts/run-action-menu-placement-tests.mjs` in `frontend`: 3 passed. That covers a menu beside the sidebar, a menu at the right edge of the page, and a menu at the left edge of a phone.
- `pnpm exec tsc --noEmit` in `frontend` passed.
- Prettier check passed on the menu files.
- Did not click through the app.

## Manual verification

Refresh Accounts so this build loads. No data changes.

1. On the desktop width, open Accounts and choose the more-options button beside Add account.
   Expected: the menu shows Refresh all accounts and Manage institutions in full, in the page, not under the sidebar. Either item can be chosen. Escape closes the menu.
2. On a phone-width window, open the same more-options button.
   Expected: the menu stays on screen and both labels can be read.
3. On a phone-width window, open the account menu from the email in the top bar. Press the page behind it.
   Expected: the menu closes. Escape closes it too. The email still toggles it.
4. Open that menu again and press Tab.
   Expected: focus starts on Income and each Tab moves to the next destination. The focused row is marked. Shift+Tab moves back.

## Approval

Zach approved this increment. It was merged in PR #20 on October 5, 2026, and
the approval was recorded on October 7, 2026.

## Pending decision

Debt capture is approved in `docs/reviews/2026-10-05-014-debts.md`. Overlapping worker and manual sync remains open. Phase 2 item 5 is next.
