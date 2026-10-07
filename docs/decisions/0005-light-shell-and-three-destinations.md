# 0005. One light shell and three primary destinations

Status: Accepted
Date: 2026-10-05
Updated: 2026-10-07

## Context

Before October 5, 2026, the app was a dark phone layout stretched onto a
laptop, with two navigations.

## Decision

One light theme and one component tree, breaking at 768px. The primary
destinations are Home, Accounts, and Activity. Income, Bills, Debts,
Categories, Connections, and Household are settings in the account menu.
A record opens as a right-hand panel from 768px up and as a full-screen
sheet under it.

## Consequences

A new screen is a primary destination or a setting, not a fourth tab.
Where Plan goes when it has a real screen is an open decision in
`docs/README.md`.

Source: `docs/design/ui-direction.md`, `.cursor/rules/ui-governance.mdc`,
`docs/reviews/2026-10-05-018-ui-governance.md`.
