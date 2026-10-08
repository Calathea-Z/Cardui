# Cardui agent notes

Read this file, `docs/README.md`, `docs/reviews/README.md`, and the latest
review before starting work.

## Docs map

- `docs/README.md`: the Now / Next / Open decisions block, the single
  source for current work, and the map of every document.
- `docs/roadmap.md`: product direction, the phase backlog, and what each
  phase built.
- `docs/decisions/`: short decision records for product and architecture
  choices.
- `docs/design/`, `docs/reference/`, `docs/checklists/`: designs,
  standing rules for the data, and manual walkthroughs.
- `docs/reviews/`: one report per increment, indexed in
  `docs/reviews/README.md`. Closed phases are in `docs/reviews/archive/`.
- `docs/archive/`: superseded documents kept for reference.

Document conventions are in `docs/README.md`. Review-report conventions
are in `docs/reviews/README.md`.

## Rules

The full text of each rule is in `.cursor/rules/`. This file only points
at them.

- Review handoff: `.cursor/rules/handoff.mdc`
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
