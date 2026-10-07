# 0006. One-shot sync worker on a cron schedule

Status: Accepted
Date: 2026-10-03
Updated: 2026-10-07

## Context

Connected accounts need a daily sync without a process running all day.

## Decision

The worker runs one sync pass over every connected Plaid item and exits. A
platform cron starts it once a day. A failed item sets exit code 1. An
item without a household is skipped.

## Consequences

A second sync does not start while one sync still holds the item. That
guard is approved in
`docs/reviews/2026-10-06-002-sync-correctness.md`. The deployed schedule
is not verified yet.

Source: `worker/README.md`,
`docs/reviews/archive/phase-0/2026-10-03-006-phase-0-baseline-close.md`.
