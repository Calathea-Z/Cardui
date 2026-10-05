# Accounts chart header

Date: October 5, 2026

## Increment

The Accounts balance chart uses the same header and text controls as Home.
The chart math and the account list are unchanged. Home's chart grid and
the under-768 page padding were left as noted.

## Changes

- The Accounts chart header is a sentence-case series name and a large
  figure, with the period change under it. The series names are "Net
  worth" and "Credit cards". Account group names in the list are
  unchanged.
- The series and range controls sit under the chart as text. The pressed
  label uses the same green as a favorable change. The dark primary
  green read as black at this size. The filled pills are gone. Home's
  range control is that same text treatment, and it no longer has a
  second pill style.
- The series choice now lives with the chart. It starts on net worth.

## Data changes

None.

## Agent verification

- `pnpm exec tsc --noEmit` and `pnpm lint` on the changed files in
  `frontend/` passed.
- `pnpm test` in `frontend/` passed.
- Prettier was run on the files this increment changed.
- Did not click through the app. Signed-in routes need Clerk.

## Manual verification

Use the household you already have. No database changes.

1. Open Accounts on a laptop.
   Expected: the chart panel says "Net worth" in sentence case, then a
   large figure, then the period change. Under the chart, Net worth,
   Cash, Investments, Credit cards, and Loans are text. The selected
   one is the same green as the period change. 1W, 1M, 3M, 6M, 1Y,
   and All are the same kind of text. The selected range is that green.
   There are no filled pills.
2. Choose Credit cards, then 1Y.
   Expected: the header says "Credit cards", the figure and the line
   follow that series, and the period change says past year. The
   account list below does not change.
3. Open Home.
   Expected: the selected range is the same green as a favorable
   change. The chart grid is still the faint line. Page padding under
   768px is still 24px.

## Approval

Zach approved this increment on October 5, 2026.

## Pending decision

Plaid sync reconciliation tests stay paused while the UI plan is the
active local work. Whether that engineering increment resumes is still
the next decision. The Home chart grid and the under-768 page padding
were left on purpose.
