<!-- BEGIN:nextjs-agent-rules -->

# This is NOT the Next.js you know

This version has breaking changes — APIs, conventions, and file structure may all differ from your training data. Read the relevant guide in `node_modules/next/dist/docs/` before writing any code. Heed deprecation notices.
<!-- END:nextjs-agent-rules -->

When a control is reused, decide whether it should be a shared primitive in
`components/ui`. For a large or specialized control, decide whether a small
primitive is enough or a library is the better fit, and ask before adding
that dependency. Choice lists use `Select` from `components/ui/select.tsx`.
Do not use a native `<select>`. See `.cursor/rules/ui-primitives.mdc`.

Shared component files use kebab-case. A feature barrel exports only the
route component. An API client function exists because a screen calls it.
Prettier stays in CI. See `.cursor/rules/frontend-conventions.mdc`.

Money text uses `formatCurrency` from `features/accounts/formatCurrency.ts`.
Chart labels use the chart formatters. That rule is for this repo only. See
`.cursor/rules/frontend-currency.mdc`.
