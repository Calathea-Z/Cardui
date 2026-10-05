# Light shell plus Home

Date: October 5, 2026

## Increment

The signed-in app uses the light Tortoise chrome, and Home is one page:
net worth, this month, and recent activity. Accounts and Activity share
that chrome. `/income` stays a real route and is not a fourth primary
nav item.

## Changes

- `frontend/app/globals.css` and `frontend/app/layout.tsx` use the light
  tokens, Inter, and a flat canvas. The dark class, wallpaper, panel
  rail, ink underline, and highlighter on amounts are gone.
- Primary navigation is Home, Accounts, and Activity. The phone tab bar
  is those three. The hamburger drawer is gone. Income, Categories,
  Connections, and Household sit in the account block. `/budgets` stays
  as a URL and is off the nav.
- Home drops the snap carousel. One net-worth chart sits beside this
  month from `lg` up, with recent activity underneath. Asset and
  liability totals are figures next to the chart. Monthly activity
  math and account-group totals are unchanged.
- Home, Accounts, and Activity have a title row. Account and Activity
  actions stay in that row on both widths.

## Data changes

None.

## Agent verification

- `pnpm exec tsc --noEmit` and `pnpm lint` in `frontend/` passed.
- Prettier was run on the files this increment changed. A full
  `pnpm format:check` still reports existing files outside this change.
- Did not start Tortoise. Signed-in routes need Clerk, and this session
  did not click through the app.

## Manual verification

Use the household you already have. No database changes.

1. Open Home on a laptop.
   Expected: a light page, Inter, no ruled wallpaper. Sidebar is Home,
   Accounts, Activity. The account block lists Income, Categories,
   Connections, and Household. Home has a title, a net-worth chart with
   asset and liability figures, this month beside it, and recent
   activity under both. There is no swipe carousel.
2. Narrow the window below 768px.
   Expected: three tabs (Home, Accounts, Activity). No Income or Budgets
   tab. No hamburger that repeats the primary nav. Settings open from
   the account control.
3. Open Accounts and Activity.
   Expected: each has a title. Add, refresh, Import, and Add sit in the
   title row, not in the phone header. List and chart behavior is the
   same as before this increment.
4. Open Income from the account block.
   Expected: `/income` still loads. The page is not redesigned beyond
   the shared light chrome and title style.
5. Open `/budgets` from the address bar.
   Expected: the placeholder is still there. It is not in the sidebar
   or the tab bar.

## Approval

Zach approved this increment on October 5, 2026.

## Pending decision

The next UI decision is the detail surface: a right-hand panel from
768px up, a full-screen sheet under it, and `Select` as a popover on
desktop. Not before Home looks like one product.
