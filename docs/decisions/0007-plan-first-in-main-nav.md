# 0007. Plan is first in the main nav

Status: Accepted
Date: 2026-10-07
Updated: 2026-10-07

## Context

Decision 0005 kept three primary destinations and left Plan's place open.
Targets stay a setting at `/targets`.

## Decision

Plan is a primary destination at `/plan`, first in the main nav. The order
is Plan, Home, Accounts, Activity. The desktop sidebar and the phone tab
bar both use that order. Income, Bills, Debts, Targets, Categories,
Connections, and Household stay in the account menu.

## Consequences

A later screen is one of those destinations or a setting. It does not add
a fifth tab. The Plan page does not show payoff dates yet.

Source: `docs/reviews/2026-10-07-013-plan-nav.md`,
`.cursor/rules/ui-governance.mdc`.
