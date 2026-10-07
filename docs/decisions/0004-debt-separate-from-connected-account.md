# 0004. A debt stays separate from a connected account

Status: Accepted
Date: 2026-10-06
Updated: 2026-10-07

## Context

A person types debt terms by hand. A bank sync must never overwrite what
they typed.

## Decision

A connected card or loan and a debt are separate records. Connecting an
account does not create a debt, and sync never writes a debt. A debt may
later follow a connected account's balance and credit limit. The person
chooses that, and can override either value. APR, minimum, due date, and
statement balance stay entered by the person. Plaid Liabilities is
deferred and not planned.

## Consequences

Linked debts need freshness and stale states, and depend on sync
correctness first.

Source: `docs/reviews/2026-10-05-014-debts.md`,
`docs/design/linked-manual-debts.md`.
