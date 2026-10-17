# 0010. Plan setup and Settings

Status: Accepted
Date: 2026-10-15
Updated: 2026-10-15

## Context

Decision 0009 keeps Home, Accounts, Activity, and Plan as the four primary
destinations and leaves settings in the account menu. That menu listed every
editor. On a short desktop window the expanded list crowded the primary
destinations. On a phone it worked as a second navigation.

## Decision

The primary order in decision 0009 stands. After those four destinations,
the desktop sidebar has two compact secondary links: Plan setup and
Settings. They are not a fifth primary destination.

Plan setup groups the existing editors without merging their data or
removing their routes:

1. Income: pay sources and household contribution shares.
2. Bills and debt payments, as separate lists.
3. Living costs: the one flexible monthly amount on Plan budget.
4. Money to protect: cash to keep, emergency savings, and named goals.
5. Extra money preferences: variable-pay and windfall allocation.

Settings is Categories, Connections, and Household. On a phone, Plan setup
is reached from Plan, and Settings is the account menu. Spending targets is
reached from Activity. Categories is also reached from Activity. Connections
is also reached from Accounts.

## Consequences

A new screen is a primary destination, a Plan setup editor, or a setting.
The shell does not add a fifth tab. Existing editor routes stay reachable.

Accepted October 15, 2026. The shell rule is in
`.cursor/rules/ui-governance.mdc`.
