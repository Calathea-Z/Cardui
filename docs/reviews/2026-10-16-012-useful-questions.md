Date: 2026-10-16
Status: Approved 2026-10-16
PR: None

# Useful questions

## Increment

Phase 4 item 4. The assistant chooses one or two questions from the
household's cash, pay, bills, debts, and living spending. Dated cash comes
first. A person can leave a detail unknown, and that note does not become a
saved amount. Each question says why the detail matters. No model or price
is chosen. There is no chat screen and no live OpenAI call. Items 5–9 have
not started.

## Decision

- `IPlanningQuestions.SelectAsync` returns the next questions. There is no
  route and no screen. `IPlanningModel` is not called. The questions are
  not written onto the conversation. A later item can say them.
- At most two are returned. One gap returns one question. None returns an
  empty list. While a dated cash gap is open, both questions come from that
  list.
- The dated order is cash on hand when no cash account is recorded, a
  connected cash balance older than two days, the next paycheck when none
  is recorded, the next essential bill when none is recorded, whether pay
  that is not steady should be left out, then a missing debt minimum or due
  date. A passed due date with a missing minimum comes before a later debt.
  A third debt waits.
- Unreliable pay is asked only when every paycheck is variable or uncertain
  and a bill or a debt payment is already known. Any steady paycheck skips
  that question. The question names the soonest unreliable payment. A
  flexible bill does not answer the essential-bill question.
- A known zero minimum is recorded. It is not asked again. A blank minimum
  stays blank.
- Ordinary monthly spending and a missing interest rate wait until the
  dated cash questions are answered or left unknown. The rate for one debt
  waits until that debt's minimum and due date are known or left unknown.
  Credit limit, remaining term, promotional terms, allocation, savings
  goals, and cash to keep are not questions in this slice. Another currency
  is not a question here.
- Leaving a detail unknown stores a conversation note. The name is
  `Question:` plus the question key, so a note the person wrote under
  another name is left alone. The value is `unknown`. Pay, bills, debts,
  cash, and living spending stay as they are. The next selection skips that
  key. A note with any other value does not. A closed conversation is
  rejected. A debt from another household is not found.
- The reason is returned with the question. It says what the detail changes
  about near-term cash, and that an unknown amount stays unknown.

## Changes

- `PlanningQuestionRules` chooses at most two questions from a snapshot of
  cash, pay, bills, debts, living spending, and notes already left unknown.
- `IPlanningQuestions` reads that snapshot through the services the screens
  use, and stores an unknown note through `IConversationService`.

## Data changes

None. No schema change and no migration. `PlanningModel:ApiKey` and
`PlanningModel:Model` stay blank. No money movement.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~PlanningQuestion"`: 35 passed, 0 failed. Twenty-one cover the order, the two-question cap, a stale balance, unreliable pay, a passed minimum, a known zero payment, a rate that waits, ordinary spending, and a rejected key. Fourteen cover an empty household that is not written, two of three debt payments, a flexible bill, variable pay, a zero minimum, an unknown note that skips the detail and saves no fact, an unknown debt payment that leaves the minimum blank, an ordinary note, a question note whose value is not unknown, another household's debt, a closed conversation, a bad key, and a missing household.
- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release`: 632 passed, 0 failed.
- `dotnet ef migrations has-pending-model-changes --project .\api --startup-project .\api`: no model changes since the last migration.
- No live OpenAI call was made.
- No application screen changed, so no browser check was run.

## Manual verification

No screen changed. Nothing to apply to the local database.

## Approval

Approved by Zach on October 16, 2026. This approves the questioning rule.
It does not start item 5, and it does not choose a model or a price.

## Pending decision

None in this slice. The next roadmap item is Phase 4 item 5, distinguish
user statements, imported facts, estimates, and unverified suggestions.
Which model to use, and what it costs, stays open.

## Paste-ready prompt

```text
Phase 4 item 4 is approved. Start item 5.

Read AGENTS.md, docs/README.md, docs/reviews/README.md,
docs/reviews/2026-10-16-012-useful-questions.md, and docs/roadmap.md
first.

Useful questions are approved. No model or price is chosen. Items 6–9 have
not started. Detail panel keyboard focus stays tracked. Leave
PlanningModel:ApiKey and PlanningModel:Model blank. A live OpenAI call
is not part of the next increment unless the review says it is.

Item 5 distinguishes user statements, imported facts, estimates, and
unverified suggestions. Do not silently convert an inferred recurring
expense into a confirmed obligation. Ask before generating or applying a
migration.
```
