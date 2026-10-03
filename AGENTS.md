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
