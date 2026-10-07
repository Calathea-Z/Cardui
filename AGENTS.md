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

Every document outside `docs/reviews/` starts with `Status:` and
`Updated:`. File and folder names are lowercase kebab-case.

## Review reports

Write each report at `docs/reviews/YYYY-MM-DD-NNN-short-name.md`, with a
number not yet used for that date. Start it with `Date`, `Status`, and
`PR` lines. In the same change, add it to `docs/reviews/README.md` newest
first and update the status block in `docs/README.md`. Do not rewrite an
old report; add a dated correction note instead. When a roadmap phase
closes, its reports may move to `docs/reviews/archive/<phase>/` with every
link updated. See `docs/reviews/README.md`.

## Review handoff

When Zach approves an increment, or asks for the wrap-up, give a commit
message and a paste-ready prompt for the next chat. The prompt should name
this file, `docs/README.md`, the latest review, `docs/reviews/README.md`,
and the pending decision or next roadmap item. Do not commit unless asked.
Do not start the next increment in that reply. See
`.cursor/rules/handoff.mdc`.

## Security

Never log, return, or store a Plaid access token, client secret, or session
token in plaintext. Keep financial queries in the signed-in household. Mark
an anonymous endpoint on purpose, and check a webhook signature first.
Production refuses wildcard hosts, localhost origins, and unencrypted
data-protection keys. Local servers bind to loopback. See
`.cursor/rules/security.mdc`.

## Dead code

Until Zach says Cardui has reached MVP and has shipped, delete code that
nothing uses in the same change. A renamed route does not keep a redirect
or an alias. See `.cursor/rules/dead-code.mdc`.

## Migrations

Update models and the DbContext, then ask before generating or applying an
EF Core migration. After approval, run `dotnet ef` in PowerShell from the
repository root with `--project .\api --startup-project .\api`. Do not
drop or wipe data without a separate approval. When a column, type, or API
is no longer used, remove it in the same change.

## Backend method comments

Document every method in `api/` and `worker/` with an XML summary that says
what it does. Public service methods are documented on the interface;
implementations use `/// <inheritdoc />`. Private methods have their own
summary. Do not document constructors. Controller actions list the HTTP
method and route.
Skip generated EF Core migrations. See
`.cursor/rules/backend-method-comments.mdc`.

## Backend private methods

Put every private method in `api/` and `worker/` inside
`#region Private Methods` at the end of its type. Fields, constructors,
and public methods stay above the region. Skip the region when a type has
no private methods, and skip generated EF Core migrations. See
`.cursor/rules/backend-private-methods.mdc`.

## Backend type files

Give each model, DTO, domain value, and options type in `api/` or `worker/`
its own file. Do not nest one inside a class, service, controller, or
calculator, and do not declare it in the same file as that behavior. A
type used by only one caller may be `internal`. A Domain type goes in
`Domain/<feature>` for the feature that owns it. A rule that several
features read stays at the `Domain` root. See
`.cursor/rules/backend-type-files.mdc`.

## Backend enums

A closed set that Cardui defines and branches on is an enum in its own
file. Store and return the member name, not the number. A value an outside
system can extend, a user-defined key, or a slug stays a string. See
`.cursor/rules/backend-enums.mdc`.

## Backend domain rules

Put a pure business rule in `Domain`. Put work that loads or saves rows in
`Services`. A Domain type goes in `Domain/<feature>` for the feature that
owns it. A rule that several features read stays at the `Domain` root.
Do not add a `Helpers` folder. See
`.cursor/rules/backend-domain-rules.mdc`.

## Backend method responsibility

Each method in `api/` and `worker/` does one job. A method that sequences
several jobs calls one method per job. A pure calculation goes in `Domain`.
Skip generated EF Core migrations. See
`.cursor/rules/backend-method-responsibility.mdc`.

## Backend query access

A read that does not update rows uses `AsNoTracking` or `Select`. Project
the columns the caller needs. Filter transactions and balance snapshots by
account id, and index a growing table by the columns that lookup uses. Ask
before an index migration. See `.cursor/rules/backend-query-access.mdc`.

## UI governance

New Tortoise screens follow the light shell, the account-menu map, and the shared action, state, and accessibility conventions. When a UX choice is uncertain, ask Zach and offer options before building it. See `.cursor/rules/ui-governance.mdc`.

## UI primitives

When a control is reused, decide whether it should be a shared primitive in
`frontend/components/ui`. For a large or specialized control, decide whether
a small primitive is enough or a library is the better fit, and ask before
adding that dependency. Choice lists use `Select` in
`frontend/components/ui/select.tsx`. Do not use a native `<select>`.
Date entry uses `DateField` in `frontend/components/ui/date-field.tsx`.
Do not use a native date input. A two-state setting uses `Switch` in
`frontend/components/ui/switch.tsx`. See `.cursor/rules/ui-primitives.mdc`.

## Frontend layers

A route loads the page. A server load assembles its data. `lib/api` is the
only HTTP. A hook owns client state and those calls. A camelCase module
holds a pure rule. A component renders. A sheet with one form and one
submit may call the API from that submit. See
`.cursor/rules/frontend-layers.mdc`.

## Frontend conventions

Shared component files use kebab-case. A feature barrel exports only the
route component. An API client function exists because a screen calls it.
Prettier stays in CI. See `.cursor/rules/frontend-conventions.mdc`.

## Frontend currency

Cardui money text uses `formatCurrency`. Chart labels use the chart
formatters. This rule stays in this repo. See
`.cursor/rules/frontend-currency.mdc`.

## Frontend types

Use `type` for object shapes, unions, and aliases. A fixed set of values
is a union, derived from the const array when that array is the list. A
value the screen sends or branches on uses that union. One shape has one
name. Component props stay next to the component. Form state stays with
its rule, separate from the API payload. API payloads for one feature
live in `frontend/lib/api/types/<feature>.ts` and are re-exported from
the types index. See `.cursor/rules/frontend-types.mdc`.

## Frontend method comments

Document functions in `frontend/` with a JSDoc block that says what the
function does and the rule its name leaves out. Include React
components, hooks, and helpers. An API client function names the HTTP
method and route. Comment a type when a field carries a rule, such as
amount sign. Skip one-line setters, JSX, obvious props, and generated
files such as `next-env.d.ts`. See
`.cursor/rules/frontend-method-comments.mdc`.
