Date: 2026-10-16
Status: Approved 2026-10-16
PR: None

# Plan revision review

## Increment

Phase 4 item 6. A proposed plan revision is shown as the changes to the
kept plan, and it is not accepted unless that review is still current. An
edit outside the conversation, or a conversation that has moved, rejects
the accept. No model or price is chosen. There is no chat screen and no
live OpenAI call. Items 7–9 have not started.

## Decision

- `IPlanRevisions.ReviewAsync` returns the changes. There is no route and
  no screen. `IPlanningModel` is not called. Nothing is written.
- The numbers are calculated again from the scenario. A review token names
  the conversation, its update time, the open draft, the accepted plan, and
  the calculated document. Accept sends that token back. The token is a
  version check, not a secret, and it is not stored.
- The listed changes are the freed-cash choice, the scenario, the actions,
  starting cash, and the living spending and cash floor the calculation
  used. A first plan skips the baseline: roll every freed dollar forward,
  no extra, and avalanche. It lists the actions. When the documents differ
  and those fields do not, the list is one calculated-plan change with no
  amount.
- A review token that does not match is stale. The response has no changes
  and no token. Accept of a stale or unreadable token is rejected.
- A conversation that moved is rejected even when the calculated plan
  already matches the kept plan. A repeated accept returns that plan when
  the conversation has not moved, and it does not write another version.
- An outside edit is a new or replaced draft, a different accepted plan, or
  a calculation that changed because pay, bills, debts, or cash changed.
  The proposal is not saved. The Plan page can still save and accept a
  draft directly. That edit makes an open proposal stale.
- Review and accept do not change the conversation, and they do not link
  the plan. No money is moved.
- A closed conversation is rejected. Another household's conversation is
  not found. A missing household is rejected.

## Changes

- `PlanRevisionRules` lists the changes and checks the versions.
- `IPlanRevisions` reviews a scenario and accepts it through the saved-plan
  draft and accept path when the review is still current.

## Data changes

None. No schema change and no migration. `PlanningModel:ApiKey` and
`PlanningModel:Model` stay blank. No money movement.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~PlanRevision"`: 16 passed, 0 failed. Six cover the choice, the scenario, a dropped action, an unchanged document, the first actions, version order, the token, captured cash, and a document difference the listed fields do not name. Ten cover a review that does not save, a draft that would be replaced, accept and a repeat, a conversation that moved after the plan already matched, an outside draft, an added paycheck, a later message, an accepted plan replaced outside chat, a closed conversation, another household, a missing household, and a missing or damaged review.
- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release`: 673 passed, 0 failed.
- `dotnet ef migrations has-pending-model-changes --project .\api --startup-project .\api`: no model changes since the last migration.
- No live OpenAI call was made.
- No application screen changed, so no browser check was run.

## Manual verification

No screen changed. Nothing to apply to the local database.

## Approval

Approved by Zach on October 16, 2026. This approves the review before a
plan revision is accepted. It does not start item 7, and it does not choose
a model or a price.

## Pending decision

None in this slice. The next roadmap item is Phase 4 item 7, let
calculation services supply every numeric forecast. Which model to use,
and what it costs, stays open.

## Paste-ready prompt

```text
Phase 4 item 6 is approved. Start item 7.

Read AGENTS.md, docs/README.md, docs/reviews/README.md,
docs/reviews/2026-10-16-014-plan-revision-review.md, and docs/roadmap.md
first.

Plan revision review is approved. No model or price is chosen. Items 8–9
have not started. Detail panel keyboard focus stays tracked. Leave
PlanningModel:ApiKey and PlanningModel:Model blank. A live OpenAI call
is not part of the next increment unless the review says it is.

Item 7 lets calculation services supply every numeric forecast. Preserve
assumptions and explain tradeoffs without guarantees, invented terms, or
shame. Ask before generating or applying a migration.
```
