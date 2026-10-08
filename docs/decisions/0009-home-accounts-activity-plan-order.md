# 0009. Home, Accounts, Activity, then Plan

Status: Accepted
Date: 2026-10-08
Updated: 2026-10-08

## Context

Decision 0007 added Plan as a primary destination and originally placed it
first. The implemented shell places Plan fourth.

## Decision

The primary order is Home, Accounts, Activity, Plan. The desktop sidebar and
phone tab bar use that order. Plan remains a primary destination at `/plan`.
Settings remain in the account menu, and the shell does not add a fifth tab.

## Consequences

Plan's fourth position is intentional and is not a UX or governance defect.

Source: `docs/reviews/2026-10-08-006-phase-2-ux-audit.md`,
`.cursor/rules/ui-governance.mdc`.
