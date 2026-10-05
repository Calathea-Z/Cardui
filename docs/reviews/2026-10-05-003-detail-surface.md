# Detail surface

Date: October 5, 2026

## Increment

Record detail opens as a right-hand panel from 768px up and a full-screen
sheet under that. `Select` opens as a popover on desktop. Home stays the
light page from the previous increment. `/income` stays a real route in
the account block, not a fourth primary item.

## Changes

- `BottomSheet` accepts `presentation="panel"`. Under 768px the panel
  fills the screen and slides up. From 768px it is a white column on the
  right, 28rem wide, and slides in from that edge. The panel sits above
  the phone header and the tab bar.
- Transaction detail, account detail, add transaction, add account,
  change category, add category, and merchant history use that panel.
  CSV import uses the same panel at 48rem so the column mapping fits.
- `Select` stays a bottom sheet under 768px. From 768px the list is a
  popover anchored to the trigger. It opens above the trigger when the
  room below is small, and it stays inside the viewport.
- The date calendar, and the emoji, color, group, subgroup, and chart
  range pickers, still slide up from the bottom. Their stack is high
  enough to sit on top of a panel.

## Data changes

None.

## Agent verification

- `pnpm exec tsc --noEmit` and `pnpm lint` in `frontend/` passed.
- `pnpm test` in `frontend/` passed, including the new popover placement
  cases.
- Prettier was run on the files this increment changed.
- Did not click through the app. Signed-in routes need Clerk.

## Manual verification

Use the household you already have. No database changes.

1. Open Home on a laptop, then Accounts and Activity.
   Expected: the light shell is unchanged. Sidebar is Home, Accounts,
   and Activity. Income is in the account block, not a fourth item.
   Home is still the net-worth chart, this month, and recent activity.
2. On a laptop, open a transaction from Activity, and an account from
   Accounts.
   Expected: a white panel on the right. The page behind it is dimmed.
   It does not slide up from the bottom.
3. Narrow the window below 768px and open the same transaction.
   Expected: a full-screen sheet with its own back header. It covers
   the tab bar. The tabs are still only Home, Accounts, and Activity.
4. On a laptop, open Add on Activity and use Account or Category.
   Expected: the form is the right-hand panel. The choice list opens
   next to the field, not as a sheet. Choosing a value closes the list.
5. Narrow the window and open that same choice list.
   Expected: the list is a bottom sheet.
6. Open Income from the account block.
   Expected: `/income` still loads. It is not in the tab bar.

## Approval

Zach approved this increment on October 5, 2026.

## Pending decision

The date calendar and the emoji, color, group, subgroup, and chart-range
pickers are still bottom sheets on a laptop. Whether those become
popovers is the next UI decision. Not before this panel and `Select`
look like the same product as Home.
