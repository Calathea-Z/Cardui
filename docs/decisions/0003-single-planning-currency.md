# 0003. Single planning currency

Status: Accepted
Date: 2026-10-03
Updated: 2026-10-07

## Context

Totals and plans need one unit, and Cardui has no currency conversion.

## Decision

A household has one planning currency. Totals include rows in that
currency and rows with a blank currency. A row in any other currency is
left out of totals, with a notice, until conversion exists.

## Consequences

Mixed-currency households see partial totals and the notice. Conversion is
a separate later feature.

Source: `docs/reviews/archive/phase-1/2026-10-03-018-financial-profile.md`.
