# Clerk page protection

Date: October 4, 2026

## Increment

Moved the sign-in gate off Clerk's deprecated route matcher and onto the
signed-in pages.

## Changes

- `frontend/proxy.ts` still calls `clerkMiddleware()`. It no longer uses
  `createRouteMatcher` or `auth.protect()`.
- The signed-in layout and every page under `app/(app)` call
  `await auth.protect()` before they load data. That covers the
  dashboard, accounts, budgets, categories, household, institutions,
  and transactions.
- Sign-in and sign-up stay outside that layout, so they remain public.
- The frontend has no server actions and no route handlers, so those
  did not need a separate check.
- The password step showed the account email in a dark color on the
  dark card. That identifier and its edit icon now use the card text
  and primary colors.
- Sign-in field placeholders, including "Enter your email address",
  now use the muted foreground color so they stay readable on the
  dark input.

## Agent verification

- `pnpm exec tsc --noEmit` in `frontend`: passed.
- `pnpm lint` in `frontend`: passed.
- Prettier check on the edited files: passed.
- No database or data changes.
- The signed-out redirect was not clicked through in a browser.

## Manual verification

Please check these, or waive them.

1. While signed out, open `http://localhost:3000/accounts`.
   Expected: the sign-in page, not the accounts screen.
2. Sign in, then open the dashboard, accounts, transactions,
   categories, household, institutions, and budgets.
   Expected: each page loads for the signed-in household.
3. While signed out, open `/sign-in` and `/sign-up`.
   Expected: those pages open, with no redirect loop.
4. On the password step, read the email under the subtitle.
   Expected: the email is light text on the dark card, and the edit
   icon stays green.
5. On the email step, read "Enter your email address" inside the field.
   Expected: the placeholder is a light gray-green, readable on the
   dark input. The same treatment applies to the password placeholder.
