Date: 2026-10-16
Status: Approved 2026-10-16
PR: None

# Persisted conversations

## Increment

Phase 4 item 2. A household can save one open planning conversation and
resume it on a later day. The store keeps the turns, a compact summary,
unanswered questions, notes, and links to saved plan versions. No model or
price is chosen. There is no chat screen, tool, or live OpenAI call. Items
3–9 have not started.

## Decision

- One household has at most one open conversation. Start returns that
  conversation. Close ends it and leaves it readable. The next start begins
  an empty conversation. It does not copy the earlier summary, questions, or
  notes.
- A turn is from the person or the assistant, up to 8,000 characters.
  Sequence starts at 1. This increment stores the text it is given. It does
  not call the planning model, and it does not log the text.
- The compact summary is one optional text, up to 4,000 characters. Null
  clears it. The turns stay. Nothing here writes a summary from a model.
- A question is one sentence, up to 500 characters. The same open question,
  ignoring case, stays the one already stored. Answering sets the time and
  leaves it out of the resume. The row stays. The same question can be asked
  again after that. Resume lists unanswered questions, oldest first.
- A note is a name and a value on the conversation. The name is up to 80
  characters and the value up to 500. The same name, ignoring case, replaces
  the value and keeps the spelling that was stored first. A note does not
  change allocation, pay, bills, debts, or a saved plan.
- A link points at a plan version in the same household. It does not accept,
  copy, or edit that version. Linking it again leaves the row as it is.
  Unlinking leaves the version in place. Another household's conversation or
  plan is not found.
- A closed conversation can be read. A later change is rejected.
- There is no route and no screen. Later items call `IConversationService`.
  Consent, and what may be sent to the model, stay in item 8.

## Changes

- `IConversationService` starts, reads, closes, and updates the household's
  conversation.
- `ConversationRules` checks blank text, length, role, and a closed
  conversation.
- The model adds `Conversations`, `ConversationMessages`,
  `ConversationQuestions`, `ConversationPreferences`, and
  `ConversationPlanLinks`.

## Data changes

`20261016190413_AddConversations` is applied. It adds `Conversations`,
`ConversationMessages`, `ConversationQuestions`, `ConversationPreferences`,
and `ConversationPlanLinks` only. Deleting a household deletes its
conversations, turns, questions, notes, and plan links, and still deletes
its plan versions. Deleting a plan version deletes links to it and leaves
the conversation. One household can have one open conversation
(`IX_Conversations_HouseholdId_Open`). Turn numbers are unique inside a
conversation. A note name is unique inside a conversation. A conversation
can link a plan version once. Status and role are stored as their member
names. No existing table or column changes. No money movement.
`PlanningModel:ApiKey` and `PlanningModel:Model` stay blank.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~ConversationRules|FullyQualifiedName~ConversationService"`: 15 passed, 0 failed. Four cover blank text, length, role, a closed conversation, and case. Eleven cover one open conversation, a resume three days later, a close that stays readable and a new empty start, turn order, summary replace and clear, an answered question left out of the resume, a note that leaves the plan and allocation unchanged, a link that stays inside the household, a closed conversation that rejects changes, a second household, and household delete.
- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release`: 587 passed, 0 failed.
- `dotnet ef migrations has-pending-model-changes --project .\api --startup-project .\api`: no model changes since the last migration.
- `dotnet ef database update --project .\api --startup-project .\api`: the local database was already up to date, including `20261016190413_AddConversations`.
- No live OpenAI call was made.
- No application screen changed, so no browser check was run.

## Manual verification

No screen changed. The migration is applied on the local database. Restart
the API if it was running before `AddConversations`.

## Approval

Approved by Zach on October 16, 2026. This approves the conversation store.
It does not start item 3, and it does not choose a model or a price.

## Pending decision

None in this slice. The next roadmap item is Phase 4 item 3, narrow tools.
Which model to use, and what it costs, stays open.

## Paste-ready prompt

```text
Phase 4 item 2 is approved. Start item 3.

Read AGENTS.md, docs/README.md, docs/reviews/README.md,
docs/reviews/2026-10-16-010-persisted-conversations.md, and docs/roadmap.md
first.

Persisted conversations are approved. 20261016190413_AddConversations is applied.
No model or price is chosen. Items 4–9 have not started. Detail panel
keyboard focus stays tracked. Leave PlanningModel:ApiKey and
PlanningModel:Model blank. A live OpenAI call is not part of the next
increment unless the review says it is.

Item 3 gives the assistant narrow tools to read scoped summaries, propose
facts, request calculations, compare supported scenarios, and draft plan
changes. The tools reuse the domain services used by direct editing.
Ask before generating or applying a migration.
```
