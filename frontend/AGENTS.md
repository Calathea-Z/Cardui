<!-- BEGIN:nextjs-agent-rules -->

# This is NOT the Next.js you know

This version has breaking changes — APIs, conventions, and file structure may all differ from your training data. Read the relevant guide in `node_modules/next/dist/docs/` (resolved from this file's directory; in monorepos the `next` package may not be visible from the repo root) before writing any code. Heed deprecation notices.

This block is written and re-added by `next dev` — verify at `node_modules/next/dist/server/lib/generate-agent-files.js`. Removing it from a diff only re-creates the uncommitted change; committing it with your work keeps the tree clean.

<!-- END:nextjs-agent-rules -->

Repository-wide rules, the docs map, review reports, and the review
handoff are in the root `AGENTS.md`. Current work is in `docs/README.md`.

Never put a Clerk secret, a Plaid token, or a session token in a
`NEXT_PUBLIC_` value, a log, or source control. See
`.cursor/rules/security.mdc`.

New Tortoise screens follow the light shell, the account-menu map, and the shared action, state, and accessibility conventions. When a UX choice is uncertain, ask Zach and offer options before building it. See `.cursor/rules/ui-governance.mdc`.

Until Zach says Cardui has reached MVP and has shipped, delete a route or
file that nothing uses. A renamed screen does not keep the old address.
See `.cursor/rules/dead-code.mdc`.

When a control is reused, decide whether it should be a shared primitive in
`components/ui`. For a large or specialized control, decide whether a small
primitive is enough or a library is the better fit, and ask before adding
that dependency. Choice lists use `Select` from `components/ui/select.tsx`.
Do not use a native `<select>`. Date entry uses `DateField` from
`components/ui/date-field.tsx`. Do not use a native date input. A two-state
setting uses `Switch` from `components/ui/switch.tsx`. See
`.cursor/rules/ui-primitives.mdc`.

A route loads the page. A server load assembles its data. `lib/api` is the
only HTTP. A hook owns client state and those calls. A camelCase module
holds a pure rule. A component renders. A sheet with one form and one
submit may call the API from that submit. See
`.cursor/rules/frontend-layers.mdc`.

Shared component files use kebab-case. A feature barrel exports only the
route component. An API client function exists because a screen calls it.
Prettier stays in CI. See `.cursor/rules/frontend-conventions.mdc`.

Money text uses `formatCurrency` from `features/accounts/formatCurrency.ts`.
Chart labels use the chart formatters. That rule is for this repo only. See
`.cursor/rules/frontend-currency.mdc`.

Use `type` for object shapes, unions, and aliases. A fixed set of values
is a union, derived from the const array when that array is the list. A
value the screen sends or branches on uses that union. One shape has one
name. Component props stay next to the component. Form state stays with
its rule, separate from the API payload. API payloads for one feature
live in `lib/api/types/<feature>.ts` and are re-exported from the types
index. See `.cursor/rules/frontend-types.mdc`.

Document functions with a JSDoc block that says what the function does
and the rule its name leaves out. Include components, hooks, and helpers.
An API client function names the HTTP method and route. Comment a type
when a field carries a rule, such as amount sign. Skip one-line setters,
JSX, obvious props, and generated files such as `next-env.d.ts`. See
`.cursor/rules/frontend-method-comments.mdc`.
