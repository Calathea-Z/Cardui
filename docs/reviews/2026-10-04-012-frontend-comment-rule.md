# Frontend comment rule

Date: October 4, 2026

## Increment

Recorded the frontend comment rule Zach approved with the method
comments. The rule applies to every Cardui chat. No application code
changed.

## Changes

- The rule is in `AGENTS.md`, `frontend/AGENTS.md`, and
  `.cursor/rules/frontend-method-comments.mdc`.
- A JSDoc block goes on every module-level function, including React
  components, hooks, and helpers. It says what the function does and
  the rule its name leaves out.
- An API client function names the HTTP method and route.
- A type is commented when a field carries a rule, such as amount sign.
- One-line setters, JSX, obvious props, and generated files such as
  `next-env.d.ts` stay uncommented.
- No ESLint documentation plugin was added.

## Data changes

None.

## Agent verification

Documentation only. No application tests.

## Manual verification

Please read the rules, or waive them.

1. Open `.cursor/rules/frontend-method-comments.mdc` and the "Frontend
   method comments" section in `AGENTS.md`.
   Expected: both match the list above, and the Cursor rule applies to
   every chat.
2. Open `frontend/AGENTS.md`.
   Expected: the same comment rule sits with the other frontend notes,
   outside the generated Next.js block.

## Pending decision

Phase 2 item 1 in `docs/Recovery-Application-Action-Plan.md` is next:
capture income sources, take-home amount, cadence, next payment date,
contributor, and reliability.
