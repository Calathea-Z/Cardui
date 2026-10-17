# 0002. Records independent of Plaid, and Plaid optional

Status: Accepted
Date: 2026-10-04
Updated: 2026-10-17

## Context

A person should be able to use Tortoise without connecting a bank.

## Decision

Accounts and transactions can exist without a Plaid item. External ids are
optional, and every row stores its source and provenance. Manual entry and
CSV import are first-class. The API and worker start without Plaid
credentials. Bank linking and sync stay off until client id, secret, and
environment are all set. A partial set stops startup.

## Consequences

Linked rows keep their Plaid ids. Sync code must leave manual rows alone.

Accepted October 4, 2026. The increment reports are in Git history.
