# 0003. Single planning currency

Status: Accepted
Date: 2026-10-03
Updated: 2026-10-17

## Context

Totals and plans need one unit, and Tortoise has no currency conversion.

## Decision

A household has one planning currency. Totals include rows in that
currency and rows with a blank currency. A row in any other currency is
left out of totals, with a notice, until conversion exists.

## Consequences

Mixed-currency households see partial totals and the notice. Conversion is
a separate later feature.

Accepted October 3, 2026. The increment report is in Git history.
