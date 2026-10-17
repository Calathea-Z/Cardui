Date: 2026-10-16
Status: Approved 2026-10-16
PR: None

# Planning call limits

## Increment

Phase 4 item 9. A planning-model call has a timeout, a short retry, and a
size cap. The log keeps the attempt, the status, and token counts. When the
call cannot be answered, the open draft stays and the person can continue
on Plan. No model or price is chosen. There is no chat screen and no live
OpenAI call.

## Decision

- `IPlanningCalls.AskAsync` asks for one reply. There is no route and no
  screen. The reply is not stored. Nothing financial is written, and no
  money is moved.
- Each attempt stops after 30 seconds. A transient failure can be posted
  again, up to three attempts, with a tenth of a second between them.
  Three hung attempts can take about 90 seconds. The caller cancelling
  stops the wait.
- A retry is for status 408, 429, 500, 502, 503, or 504, a timeout the
  caller did not cancel, or a network failure. A client error, an empty
  reply, an unreadable body, and an unexpected exception are posted once.
  A completed response is not posted again.
- The cost limit is a size limit. The message and the developer text are
  each at most 24,000 characters. The request asks for at most 1,024
  output tokens. No dollar price is set, and usage is not stored. When the
  provider reports integer token counts, those numbers are logged. A
  missing count does not fail the call.
- A log line may include the attempt, the status, the provider error type
  and code, the exception type name, the token counts, and the fallback
  reason. A code that contains a space, a bank access token, or more than
  80 characters is written as redacted. The API key, the message, the
  developer text, the reply, the provider error text, and the draft are
  left out. The message shown for a failure does not include the provider
  body.
- A provider failure, a model that is not configured, or text past the cap
  returns a notice that the draft is unchanged and the person can continue
  on Plan. The open draft id is included when the household has one. The
  conversation, that draft, and an accepted plan stay as they were.
- A bank access token is still refused before a request is posted. A closed
  conversation is rejected. Another household's conversation is not found.
  A missing household is rejected. The caller cancelling is not turned into
  the manual notice.

## Changes

- `PlanningCallRules` decides which failures can be tried again, how long
  the text may be, and which words may be logged.
- `OpenAiPlanningModel` posts within those limits and reads token counts
  from the response.
- `IPlanningCalls` asks once and, when the call cannot be answered, leaves
  the draft in place.

## Data changes

None. No schema change and no migration. `PlanningModel:ApiKey` and
`PlanningModel:Model` stay blank. No money movement.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~PlanningCall|FullyQualifiedName~OpenAiPlanningModel"`: 28 passed, 0 failed. Five cover the transient statuses, the three-attempt cap, the character boundary, a bank token, and log lines that keep codes and token counts. Four cover a failure that leaves the draft and the accepted plan, a reply that is not saved, a request past the cap, a model that is not configured, a closed conversation, a token, cancellation, another household, and a missing household. Nineteen are the client tests. Eight of those cover a transient status then a reply, three rate-limit failures, a timeout, a network failure, an unexpected failure that is not retried, text past the cap, an unreadable body, and an error code that is redacted. The other eleven are the existing client tests.
- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release`: 715 passed, 0 failed.
- `dotnet ef migrations has-pending-model-changes --project .\api --startup-project .\api`: no model changes since the last migration.
- No live OpenAI call was made.
- No application screen changed, so no browser check was run.

## Manual verification

No screen changed. Nothing to apply to the local database.

## Approval

Approved by Zach on October 16, 2026. This approves the timeout, the retry,
the size cap, the safe log, and the fallback that leaves the draft in place.
It does not start Phase 5, and it does not choose a model or a price.

## Pending decision

None in this slice. Which model to use, and what it costs, stays open.
There is still no chat screen, so the phase exit's full conversation is
not on a page. The next numbered roadmap item is Phase 5 item 1, add next
actions, milestones, and assumptions needing confirmation to Plan.

## Paste-ready prompt

```text
Phase 4 item 9 is approved. Start Phase 5 item 1.

Read AGENTS.md, docs/README.md, docs/reviews/README.md,
docs/reviews/2026-10-16-017-planning-call-limits.md, and docs/roadmap.md
first.

Planning call limits are approved. No model or price is chosen. There is
still no chat screen. Detail panel keyboard focus stays tracked. Leave
PlanningModel:ApiKey and PlanningModel:Model blank. A live OpenAI call is
not part of the next increment unless the review says it is.

Phase 5 item 1 adds next actions, milestones, and assumptions needing
confirmation to Plan. The debt charts and the cash outlook are already on
that page. Ask before generating or applying a migration.
```
