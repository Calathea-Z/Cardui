# Frontend conventions

Date: October 4, 2026

## Increment

Recorded the frontend conventions from the cleanup, and kept the
currency formatter as a Cardui rule.

## Changes

- General frontend conventions are in `AGENTS.md`, `frontend/AGENTS.md`,
  and `.cursor/rules/frontend-conventions.mdc`.
- Shared component files use kebab-case. Feature components use
  PascalCase. Logic modules use camelCase.
- A feature barrel exports only the route component.
- An API client function is added when a screen calls it, and removed
  when the last caller goes away.
- Prettier stays in CI. A `components/ui` primitive is added when a
  screen needs it.
- Cardui money text uses `formatCurrency`. Chart labels use the chart
  formatters. That rule is in `.cursor/rules/frontend-currency.mdc`
  and applies to frontend TypeScript files in this repo. It was not
  added as a rule for other projects.

## Agent verification

- Documentation only. No application tests.

## Manual verification

Please read the rules, or waive them.

1. Open `.cursor/rules/frontend-conventions.mdc` and the "Frontend
   conventions" section in `AGENTS.md`.
   Expected: file names, barrels, API clients, Prettier, and unused
   primitives match the list above.
2. Open `.cursor/rules/frontend-currency.mdc`.
   Expected: it names `formatCurrency` and the chart formatters, and
   it says the rule is for Cardui only.
