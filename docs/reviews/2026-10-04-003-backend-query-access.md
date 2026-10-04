# Backend query access

Date: October 4, 2026

## Increment

Added the standing rule for how backend queries load rows and how indexes
match those lookups.

## Changes

- The rule is in `AGENTS.md` and
  `.cursor/rules/backend-query-access.mdc`.
- A read that does not update rows uses `AsNoTracking` or `Select`.
- A query projects the columns it needs, and a repeated lookup is loaded
  once before the loop.
- Transaction and balance-snapshot filters seek by account id. The
  household rule stays on accounts.
- An index on a growing table leads with the filtered and sorted columns.
  A column that is almost always the same value does not get its own index.
- A comparison that wraps the column is not a seek unless an index matches
  that expression.
- An index change still needs approval before the EF migration, as in
  `.cursor/rules/migrations.mdc`.

## Agent verification

- Documentation only. No application tests.

## Manual verification

Please read the rule, or waive it.

1. Open `.cursor/rules/backend-query-access.mdc`.
   Expected: the bullets match the list above, and the example filters
   transactions by account id without tracking them.
2. Open the "Backend query access" section in `AGENTS.md`.
   Expected: it points at that rule file.
