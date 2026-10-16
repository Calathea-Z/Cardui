Date: 2026-10-16
Status: Approved 2026-10-16
PR: None

# Planning model

## Increment

Phase 4 item 1. The API can call OpenAI through `IPlanningModel` when both
`PlanningModel:ApiKey` and `PlanningModel:Model` are set. No model or price
is chosen. There is no chat screen, saved conversation, or tool. Items 2–9
have not started.

## Changes

- `IPlanningModel` asks for one text reply. The OpenAI adapter posts to the
  Responses API. The model id comes from configuration. The request sets
  `store` to false, sends no tools, and keeps the API key in the header.
- A blank key and model leave guided conversation off, and the API still
  starts. A partial set stops startup. The key is not logged. A provider
  error is logged as a status, type, and code. The response body is not
  logged or returned.
- A call gives up after 30 seconds. Retry, cost limits, and safe logging
  policy stay in item 9. Consent and data minimization stay in item 8.
  This call does not store the exchange at Cardui, and it asks OpenAI not
  to store it either.
- No OpenAI package was added. The adapter uses `HttpClient`.

## Data changes

None. No dependency, route, schema, migration, or financial record changed.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~PlanningModel|FullyQualifiedName~OpenAiPlanningModel|FullyQualifiedName~OptionalPlaidStartup"`: 18 passed, 0 failed. Fifteen cover this increment: blank and partial settings, startup without a model, a partial set rejected at startup, the request body and header, text read from `output_text` and from message content, a hidden provider error, an empty reply, caller cancellation, and the 30-second bound. The other three are the existing optional-Plaid startup tests.
- No live OpenAI call was made.
- No application screen changed, so no browser check was run.

## Manual verification

No screen changed. A live call would spend API credit and is not part of
this review.

## Approval

Approved by Zach on October 16, 2026. This approves the planning-model
client. It does not start item 2, and it does not choose a model or a price.
Both settings stay blank until a review asks for a live call.

## Pending decision

Approve this client before Phase 4 item 2. Item 2 persists conversations.
Do not start it until this is approved. Which model to use, and what it
costs, stays open.

## Paste-ready prompt

```text
Phase 4 item 1 is approved. Start item 2.

Read AGENTS.md, docs/README.md, docs/reviews/README.md,
docs/reviews/2026-10-16-009-planning-model.md, and docs/roadmap.md
first.

The planning-model client is approved. No model or price is chosen.
Items 3–9 have not started. Detail panel keyboard focus stays tracked.
Leave PlanningModel:ApiKey and PlanningModel:Model blank. A live OpenAI
call is not part of this increment unless the review says it is.

Item 2 persists conversations, compact summaries, unanswered questions,
user preferences, and linked plan versions, so a conversation can resume
over multiple days. Ask before generating or applying a migration.
```
