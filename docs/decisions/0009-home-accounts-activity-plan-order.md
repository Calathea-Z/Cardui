# 0009. Home, Accounts, Activity, then Plan

Status: Accepted
Date: 2026-10-08
Updated: 2026-10-15

## Context

Decision 0007 added Plan as a primary destination and originally placed it
first. The implemented shell places Plan fourth.

## Decision

The primary order is Home, Accounts, Activity, Plan. The desktop sidebar and
phone tab bar use that order. Plan remains a primary destination at `/plan`.
Settings remain in the account menu, and the shell does not add a fifth tab.

Secondary grouping is recorded in
[decision 0010](0010-plan-setup-and-settings.md). This decision's primary
order is unchanged.

## Consequences

Plan's fourth position is intentional and is not a UX or governance defect.

Detail: `.cursor/rules/ui-governance.mdc`.
