# Cardui agent notes

Linear is the source of truth for current work. Read the selected Linear
issue, this file, `docs/README.md`, and the relevant roadmap and decision
docs before starting work. Do not start from the latest review report.

## Docs map

- `docs/README.md`: durable priorities, open decisions, and the map of
  every document. It does not choose the next issue.
- `docs/roadmap.md`: product direction, the phase backlog, and what each
  phase built.
- `docs/decisions/`: short decision records for product and architecture
  choices.
- `docs/design/`, `docs/reference/`, `docs/checklists/`: designs,
  standing rules for the data, and manual walkthroughs.
- `docs/reviews/`: open investigations only, listed in
  `docs/reviews/README.md`. Closed increment reports are in Git history.
  The folder goes away when it is empty.
- `docs/archive/`: superseded documents kept for reference.

Document conventions are in `docs/README.md`. The task-review rule is in
`.cursor/rules/handoff.mdc`.

## Rules

The full text of each rule is in `.cursor/rules/`. This file only points
at them.

- Task review: `.cursor/rules/handoff.mdc`
- Security: `.cursor/rules/security.mdc`
- Dead code: `.cursor/rules/dead-code.mdc`
- Migrations: `.cursor/rules/migrations.mdc`
- Backend method comments: `.cursor/rules/backend-method-comments.mdc`
- Backend private methods: `.cursor/rules/backend-private-methods.mdc`
- Backend type files: `.cursor/rules/backend-type-files.mdc`
- Backend enums: `.cursor/rules/backend-enums.mdc`
- Backend domain rules: `.cursor/rules/backend-domain-rules.mdc`
- Backend method responsibility: `.cursor/rules/backend-method-responsibility.mdc`
- Backend query access: `.cursor/rules/backend-query-access.mdc`
- UI governance: `.cursor/rules/ui-governance.mdc`
- UI primitives: `.cursor/rules/ui-primitives.mdc`
- Frontend layers: `.cursor/rules/frontend-layers.mdc`
- Frontend conventions: `.cursor/rules/frontend-conventions.mdc`
- Frontend currency: `.cursor/rules/frontend-currency.mdc`
- Frontend types: `.cursor/rules/frontend-types.mdc`
- Frontend method comments: `.cursor/rules/frontend-method-comments.mdc`
