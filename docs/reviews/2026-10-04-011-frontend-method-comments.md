# Frontend method comments

Date: October 4, 2026

## Increment

Added purpose comments across the frontend, in the same spirit as the
backend method comments. The comments say what a function does and the
rule its name leaves out. Behavior is unchanged. Zach approved this
increment on October 4, 2026, including the rule proposed below. That
rule is not in `AGENTS.md` yet.

## Changes

- Module-level functions, React components, hooks, and helpers now have
  a JSDoc block. A comment states the purpose and any rule, such as a
  fallback, a sign convention, or when a control is available.
- API client functions name the HTTP method and route, then the purpose.
  Example: `POST /api/accounts`.
- Types are documented when a field carries a rule. Stored money amounts
  stay positive for money out and negative for money in. A null import
  column means that column is unused. Empty page-load helpers exist so a
  failed API call can still render.
- A named function inside a component is documented when it branches,
  calls the API, or calculates. One-line setters whose name is the whole
  story were left alone. JSX lines and obvious props were left alone.
- Generated `next-env.d.ts` and the test scripts under
  `frontend/scripts` were left alone. The scripts call modules that are
  now documented.
- A byte-order mark at the start of `frontend/lib/api/types.ts` was
  removed while that file was edited.

## Proposed rule

Add this to `AGENTS.md`, `frontend/AGENTS.md`, and
`.cursor/rules/frontend-method-comments.mdc` if Zach approves. Do not
add an ESLint documentation plugin. That would be a new dependency, and
the backend rule is kept by review in the same way.

When adding or changing TypeScript in `frontend/`, document functions
and the types that carry a rule. Do this in the same change.

- Put a JSDoc block immediately above every module-level function,
  including React components, hooks, and helpers.
- A named function inside a component gets a comment when it branches,
  calls the API, or calculates. A one-line setter whose name is the
  whole story does not.
- An API client function names the HTTP method and route, then what it
  does. Example: `POST /api/accounts`.
- An exported type gets a comment when a field's meaning is a rule,
  such as amount sign, a null that means unused, or a fallback used when
  the API fails. Skip a comment that only repeats the type name.
- Say what it does and the rule the name leaves out, in one or two
  sentences.
- Do not comment every JSX line, every prop, or generated files such as
  `next-env.d.ts`.

```ts
/**
 * POST /api/accounts
 * Creates a manual account for the signed-in household.
 */
export async function createManualAccount(...)

/**
 * Formats a money amount for on-screen text.
 * A missing amount is a dash. An unrecognized currency code uses USD.
 */
export function formatCurrency(...)
```

## Data changes

None. No migration. No records were written.

## Agent verification

- `pnpm exec tsc --noEmit` in `frontend`: passed.
- Prettier was applied to the documented TypeScript files.
- Did not run `pnpm test` or `pnpm lint`. The comments do not change
  behavior.
- Did not click through the app.

## Manual verification

No screen or data change. Spot-check the comments, or waive this list.

1. Open `frontend/lib/api/browser/accounts.ts`.
   Expected: each function names its route, such as `POST /api/accounts`
   and `POST /api/accounts/{id}/reconciliation`, and says what it does.
2. Open `frontend/features/accounts/formatCurrency.ts` and
   `frontend/features/accounts/chartTimeRange.ts`.
   Expected: formatters say what they show. Chart labels drop cents.
   Compact axis labels cover large balances.
3. Open `frontend/lib/api/types.ts`.
   Expected: `TransactionDto` says a positive amount is money out and a
   negative amount is money in. Types that only mirror a name are not
   all commented field by field.
4. Open `frontend/features/transactions/ImportCsvSheet.tsx`.
   Expected: the sheet and its step helpers are commented. One-line
   clicks inside the markup are not.

## Pending decision

Zach approved the rule below on October 4, 2026. Write it into
`AGENTS.md`, `frontend/AGENTS.md`, and
`.cursor/rules/frontend-method-comments.mdc`. Do not add an ESLint
documentation plugin. Phase 2 item 1, income sources, is the next
product increment after that rule is recorded.
