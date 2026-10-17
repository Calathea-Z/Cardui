# Planning conversation

Status: Current
Updated: 2026-10-17

Phase 4 items 1–9 exist as backend and domain components. They are not a
guided-planning experience. The phase exit is not met. There is no chat
screen, no conversation route, and no live OpenAI call in ordinary use.
The API key and model stay blank until a later review authorizes a live
call. No model and no price are chosen.

This note was checked against `api/Domain/Planning`,
`api/Services/Planning/OpenAiPlanningModel.cs`,
`api/Services/Implementations/PlanningShares.cs`, and
`api/Configuration/PlanningModelConfiguration.cs` on October 17, 2026.
No planning call was made. The scripted planning provider described in the
roadmap is not in the application code. Test doubles named Scripted are
not that provider.

## What is not built

No controller calls `IPlanningTools`, `IPlanningFacts`, `IPlanningQuestions`,
`IPlanningForecasts`, `IPlanningShares`, `IPlanningCalls`, `IPlanRevisions`,
or `IConversationService`. That absence was a source search on October 17,
2026, not a runtime probe. The model request sends text and instructions.
It does not send tools. A planning call can return a reply without
persisting it or assembling consent-filtered context. A fact proposal is
validated and not saved. Plan review and accept through the conversation
have no route. Accepting a plan from the Plan page does not link the
conversation.

`OpenAiPlanningModel` runs only when `PlanningModel:ApiKey` and
`PlanningModel:Model` are both set. A partial set stops startup. With both
blank, guided conversation stays off and the API still starts.

## Call limits

Checked in `PlanningCallRules` and `PlanningModelConfiguration`.

- One attempt stops after 30 seconds.
- A transient failure may be tried again. The first try counts, and two
  retries remain (`MaxAttempts` is 3). The pause before a retry is 100
  milliseconds. Transient statuses are 408, 429, 500, 502, 503, and 504.
- A message or developer text longer than 24,000 characters, after trimming,
  is not sent.
- The request asks for at most 1,024 output tokens. That caps the reply
  length. It is not a price.
- The request sets `store` to false. The API key is sent only in the
  Authorization header.
- Logs record the attempt, the status, a short error type and code, the
  exception type, and token counts. They do not include the reply text.
  The API key is not one of those fields. This cleanup did not audit every
  other log in the process.

A failure leaves the open draft and the conversation unchanged. The person
can continue on Plan.

## Consent and what may be shared

`PlanningShareRules` stores the choice on the open conversation under the
reserved note name `DataSharing`. The stored words are `allowed` and
`declined`. A missing note means the question has not been asked. Not asked
and declined both refuse a share. A person cannot save a note under that
name. A new conversation does not copy the choice. Connecting a bank,
importing a file, or starting a conversation leaves the choice unasked.

After an explicit yes, the prepared text includes only the person's message
and the named sections. The sections are summary, cash, debts, income,
bills, savings, living, allocation, plan, facts, and forecast. One
transaction description or one uploaded excerpt can be attached, up to 500
characters. The share does not read transaction rows or import files.
Developer instructions stay Cardui's own words. Quoted household text is
data, not an instruction.

Cash is the total, the account counts, and the balance dates. It does not
include account names, masks, or connection tokens. A debt line is the
name, the plan balance or unknown, and the rate, minimum, due date, and
promotion end when those are present. A null minimum is omitted. Account
ids and masks are left out. Income is take-home, cadence, the next date,
the person, and low pay when it was recorded. Strong pay, gross pay, and
later dates stay off that line. A bill is the saved amount, cadence, due
date, and essential or flexible. The source account id is left out. Savings
lines leave out the account mask and account id. Bill, living, allocation,
plan, and forecast wording beyond those omissions was not re-read field by
field for this note. The section list and the refusal rules were.

A bank access token is refused before the request is posted. The markers
are the data-protection prefix `dp:v1:` and the Plaid prefixes
`access-sandbox-`, `access-development-`, and `access-production-`. A line
that contains one is left out. The token is not logged.

## Facts, questions, and forecasts

A planning input is labeled `UserStatement`, `ImportedFact`, `Estimate`, or
`UnverifiedSuggestion`. The label is not stored, and describing a fact does
not save it. A recurring suggestion does not become a bill. Only a bill the
person already saved is a confirmed obligation. A monthly average is an
estimate and loses any date it was given.

`PlanningQuestionRules.Choose` returns at most two questions. Dated cash
comes first. A detail the person left unknown is skipped and is not stored
as zero.

`PlanningForecastRules` copies amounts and dates from the plan calculation.
It rejects wording that guarantees a result, invents a term the calculation
does not use, or treats ordinary spending as a personal failing. Calculation
version text is `"1"`. A forecast from this type does not add a number.

`PlanningToolRules` allows a tool to read a summary, propose a fact,
calculate, compare, or draft. It does not save. A suggestion key on a
proposed bill is rejected.

A proposed plan revision is shown before it is accepted. An outside edit or
a stale conversation rejects that accept. Those services have no route.
