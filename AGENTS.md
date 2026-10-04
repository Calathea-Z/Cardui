# Cardui agent notes

Read this file, `docs/reviews/README.md`, and the latest review before
starting work.

## Review handoff

When Zach approves an increment, or asks for the wrap-up, give a commit
message and a paste-ready prompt for the next chat. The prompt should name
this file, the latest review, `docs/reviews/README.md`, and the pending
decision. Do not commit unless asked. Do not start the next increment in
that reply.

## Migrations

Update models and the DbContext, then ask before generating or applying an
EF Core migration. After approval, run `dotnet ef` from `api/` in
PowerShell. Do not drop or wipe data without a separate approval.

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
type used by only one caller may be `internal`. See
`.cursor/rules/backend-type-files.mdc`.

## Backend domain rules

Put a pure business rule in `Domain`. Put work that loads or saves rows in
`Services`. Do not add a `Helpers` folder. See
`.cursor/rules/backend-domain-rules.mdc`.

## Backend method responsibility

Each method in `api/` and `worker/` does one job. A method that sequences
several jobs calls one method per job. A pure calculation goes in `Domain`.
Skip generated EF Core migrations. See
`.cursor/rules/backend-method-responsibility.mdc`.
