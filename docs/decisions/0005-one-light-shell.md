# 0005. One light shell

Status: Accepted
Date: 2026-10-05
Updated: 2026-10-08

## Context

Before October 5, 2026, the app was a dark phone layout stretched onto a
laptop, with two navigations.

## Decision

One light theme and one component tree, breaking at 768px. A record opens
as a right-hand panel from 768px up and as a full-screen sheet under it.
Settings stay in the account menu.

## Consequences

The primary destinations and their order are in
[0009](0009-home-accounts-activity-plan-order.md). On October 8, 2026,
Savings joined the account menu at `/savings`. Plan budget joined the same
day at `/living`, after Debts and before Savings.

Source: `docs/design/ui-direction.md`, `.cursor/rules/ui-governance.mdc`,
`docs/reviews/2026-10-05-018-ui-governance.md`.
