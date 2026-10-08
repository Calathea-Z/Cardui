<!-- BEGIN:nextjs-agent-rules -->

# This is NOT the Next.js you know

This version has breaking changes — APIs, conventions, and file structure may all differ from your training data. Read the relevant guide in `node_modules/next/dist/docs/` (resolved from this file's directory; in monorepos the `next` package may not be visible from the repo root) before writing any code. Heed deprecation notices.

This block is written and re-added by `next dev` — verify at `node_modules/next/dist/server/lib/generate-agent-files.js`. Removing it from a diff only re-creates the uncommitted change; committing it with your work keeps the tree clean.

<!-- END:nextjs-agent-rules -->

Repository-wide notes are in the root `AGENTS.md`. Current work is in
`docs/README.md`. The full text of each rule is in `.cursor/rules/`.

- Security: `.cursor/rules/security.mdc`. Never put a Clerk secret, a
  Plaid token, or a session token in a `NEXT_PUBLIC_` value, a log, or
  source control.
- UI governance: `.cursor/rules/ui-governance.mdc`
- Dead code: `.cursor/rules/dead-code.mdc`
- UI primitives: `.cursor/rules/ui-primitives.mdc`
- Frontend layers: `.cursor/rules/frontend-layers.mdc`
- Frontend conventions: `.cursor/rules/frontend-conventions.mdc`
- Frontend currency: `.cursor/rules/frontend-currency.mdc`
- Frontend types: `.cursor/rules/frontend-types.mdc`
- Frontend method comments: `.cursor/rules/frontend-method-comments.mdc`
