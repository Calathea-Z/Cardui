Date: 2026-10-17
Status: Awaiting review
PR: None

# Conversational planning readiness

## Increment

TOR-5, under TOR-1. This reads the planning services that a guided debt
conversation would use. It says what those services can read, preview, and
save, and where a conversation still cannot happen. No code, calculation,
route, or screen changed. TOR-2, TOR-3, and TOR-4 were not done. Nothing
was written to Linear.

## What is ready

The conversation backend is a set of services. Keep them. A chat screen
should call them rather than add a second planning path.

A person cannot use them yet. None of these services has an HTTP route, and
the frontend has no conversation client. `IPlanningModel.ReplyAsync` asks
for one text reply and sends no tools. Cardui does not keep that exchange.
The scripted planning model named in the core journey is not implemented.
The only scripted model is a test double.

## Read

These reads stay in the signed-in household and do not write.

| Service | What it returns |
| --- | --- |
| `IPlanningTools.ReadSummaryAsync` | One of cash, debts, income, saved bills, savings, living, allocation, or saved plan versions. A bill suggestion is left out. |
| `IPlanningFacts.DescribeAsync` | Labeled inputs: a user statement, an imported fact, an estimate, or an unverified suggestion. |
| `IPlanningQuestions.SelectAsync` | The next one or two questions. They are not written onto the conversation. |
| `IPlanningForecasts.DescribeAsync` | The forecast for one temporary scenario, copied from the plan calculation. |
| `IPlanningShares.GetConsentAsync` | Whether this conversation may include household data. Not asked and declined both mean no. |
| `IPlanningShares.PrepareAsync` | Text for a model request, only after an explicit yes, and only for the named sections. |
| `IConversationService` | The open conversation, or one conversation by id, including a closed one. |

A prepared share can name the conversation summary, cash, debts, income,
bills, savings, living, allocation, saved plans, labeled facts, and the
forecast statement with its assumptions and tradeoffs. Day-by-day forecast
figures stay out. A bank access token is left out. Transaction rows are not
read.

## Preview

A preview checks the same rules as the matching save and returns the result.
It does not insert or update a financial row, and it does not move money.

| Call | Preview |
| --- | --- |
| `ProposeIncomeAsync`, `ProposeBillAsync`, `ProposeDebtAsync`, `ProposeSavingsAsync`, `ProposeLivingContributionAsync`, `ProposeAllocationAsync` | One proposed fact. A suggestion key on a bill is rejected. |
| `RequestCalculationAsync`, `CompareScenariosAsync` | One temporary scenario, or two. |
| `CompareRestructuringAsync` | One consolidation loan, balance transfer, or replacement loan. The offer is not approved. |
| `DraftPlanChangeAsync` | A decision-plan draft. It is not saved or accepted. |
| `IPlanRevisions.ReviewAsync` | What accepting the scenario would change. The review token is a version check, and it is not stored. |

## Save

Chat has no method that saves income, a bill, a debt, a savings row, living
spending, or an allocation. Those saves stay on the existing editors.

| Call | What it writes |
| --- | --- |
| `StartAsync`, `CloseAsync`, `AppendMessageAsync`, `ReplaceSummaryAsync`, `AddQuestionAsync`, `AnswerQuestionAsync` | The conversation record only. |
| `SetPreferenceAsync`, `RemovePreferenceAsync` | A note. Pay, bills, debts, allocation, and saved plans stay as they are. The data-sharing name is reserved. |
| `SetDataSharingAsync`, `AllowAsync`, `DeclineAsync` | An explicit yes or no for this open conversation. A new conversation does not copy it. |
| `LinkPlanVersionAsync`, `UnlinkPlanVersionAsync` | A link to a saved plan version. The version is not accepted or edited. |
| `LeaveUnknownAsync` | A note named `Question:` plus the question key, with the value `unknown`. The financial amount stays unset. |
| `IPlanRevisions.AcceptAsync` | The plan version, when the review token still matches the conversation, the draft, the accepted plan, and the calculation. The conversation is not changed and the version is not linked. |
| `IPlanningCalls.AskAsync` | Nothing. The reply is not stored. A failed call leaves the conversation and the open draft unchanged. |

## Fact confirmation

A saved bill is the only confirmed obligation. Saving it on the bill form,
including a bill saved from a suggestion, is that confirmation.
`DescribeAsync` does not save a bill, hide a pattern, or follow an account.

An unverified suggestion is a recurring pattern noticed in activity, or a
connected account that looks like a debt. A proposed bill cannot carry a
suggestion key, so noticing a payment does not make it a bill. An estimate
is the monthly average of a paycheck. An imported fact is cash, a debt
balance, or a credit limit taken from a connected account or a file.

Leaving a detail unknown skips that question later. It does not turn the
missing amount into zero.

## Gaps

1. There is no route and no Plan chat view, so none of this is reachable.
2. The model cannot call a tool, and a planning call does not store its reply.
3. A proposal has no apply method. Confirming a fact from chat would need
   one save through the existing editor path.
4. Selecting the next question does not record that it was asked.
   `LeaveUnknownAsync` is the only question write.
5. Accepting a plan from a review does not link that version on the
   conversation.
6. The scripted provider for early chat is not built. The API key and model
   stay blank.

## Proposed follow-ups

These are not created. The approved core journey already orders the build:
phone access, then household scope, then the minimal chat view. This report
does not start that view.

1. Minimal chat stores the person's turn and the reply. That is core
   journey item 4, and it is the first place `AskAsync` would keep a reply.
2. That view calls the existing tools. A tool still does not save. That is
   item 6.
3. One confirm action applies a proposal through the existing save. That is
   item 7.
4. Review and accept from that view use the current review token. Say at
   that point whether accept also links the version. That is item 8.
5. A later issue can record the question `SelectAsync` chose. It is not
   needed for the first stored turn.

## Verification

Documentation only. No application code, test run, live call, database
change, or network change.

## Approval

Awaiting review. Approval of this report does not approve a code change,
and it does not start TOR-2, TOR-3, TOR-4, or core journey item 4.

## Pending decision

None. Phone access, the OpenAI model, and the item 9 spending cap stay
where `docs/README.md` already records them.

## Correction

October 17, 2026. The follow-ups above named phone access before household
scope. Zach later directed desktop guided recovery and MX connectivity
ahead of phone access. The current order is in `docs/roadmap.md`.
