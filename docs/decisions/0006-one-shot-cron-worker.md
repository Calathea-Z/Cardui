# 0006. One-shot sync worker on a cron schedule

Status: Accepted
Date: 2026-10-03
Updated: 2026-10-17

## Context

Connected accounts need a daily sync without a process running all day.

## Decision

The worker runs one sync pass over every connected Plaid item and exits. A
platform cron starts it once a day. A failed item sets exit code 1. An
item without a household is skipped.

## Consequences

A second sync does not start while `PlaidItemSync.HoldsItem` still holds
the item. The lease is 30 minutes. The deployed schedule is not verified
yet.

Source: `worker/README.md`, `api/Domain/Plaid/PlaidItemSync.cs`.
