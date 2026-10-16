Date: 2026-10-16
Status: Approved 2026-10-16
PR: None

# Phase 3 UX reassessment

## Assessment

Plan still asks a person to get through a scenario editor before learning whether the next month is covered. Choosing Review shortfall then opens a different cash result. The October 15 navigation and cash-first Overview are in place. The October 16 working scenario put the editor above those views.

Tortoise is the signed-in product name in [`docs/design/ui-direction.md`](../design/ui-direction.md). The October 5 UI-direction review says that direction does not name an outside reference app. No other reference application was found.

Recommended flow: **see the next 30 days, then review that same shortfall, then try a change and keep a plan.** Typical pay is a later look, under its own name. The October 15 assessment stays as written.

## Scope

- Read-only signed-in walkthrough of the running local app. Viewports measured at 1440×768, 1440×900, and 390×844. The API health check responded.
- No scenario was submitted, no offer was compared, no plan was saved, and no financial record was edited. Household amounts, names, and screenshots are omitted.
- Already in place, and not repeated as open work: the four primary destinations plus Plan setup and Settings; the debt chart names its plotted subset; Allocation states that it does not move money; payoff choices cover highest rate first, remove a minimum, lower utilization, pay one debt first or last, and keep none, some, or all.

## Page structure

Home, Accounts, Activity, and Plan stay the primary destinations. Plan setup and Settings stay secondary. Cash outlook, debt payoff, and the offer stay supporting views. Offers stay temporary.

### Overview

The question is whether the next 30 days are covered. One status, such as Current facts or Trying changes — not saved, sits with the Plan heading. The safety figure and one next action come next. Review shortfall explains that same steady-pay figure: the amount, the lowest-balance date, and money set aside. Typical pay follows afterward, named as a different view. Debt outlook and the issue count are secondary. On a desktop, safety is the wide column and the action, debt outlook, and issues sit beside it. On a phone, the order is the heading status, safety, action, debt outlook, and issues.

### Try changes

The question is what happens if one thing changes. The selected control and the Update preview result sit together. Before any edit, the page says these are current facts. After an edit, the comparison is Current facts, Trying, and Change. Every choice, including freed cash, stays pending until Update preview. Save records the result on screen. Cash outlook, debt payoff, Compare an offer, and Plan setup are secondary. On a phone, the controls come first, then the result.

### Your plan

The question is what to do, and which version this is. Status, version, and at most three actions come first, each with why, the cash effect, and a repair link. Assumptions and history stay collapsed. A stored version has no live editing controls. A version saved before the shortfall amount exists says that amount was not stored.

### Plan setup

The question is whether the facts Plan uses are still right. The five groups stay in workflow order. Each row shows the saved summary, a real issue when one exists, and a specific action such as Edit living costs. Desktop uses compact full-width rows and may widen the page bound. It does not split the current narrow column into two. Phone uses the same rows and Back to Plan. Bills and debts stay separate editors.

## Repair sequence

| Increment | Scope | Acceptance |
| --- | --- | --- |
| 1 — The next month comes first | Tabs under the heading, one heading status, editor only on Try changes, Review shortfall explains the Overview figure, new versions store the shortfall amount, comparison labels, full-width setup rows | At 1440×768 and 390×844 the safety amount is first. Review shortfall matches it before typical pay. An older version does not gain today's amount |
| 2 — One way to apply a change | Update preview for every choice, pending edits stay visible, save matches the screen, plainer payoff language, offer rate limit beside the field | Two edits apply together only after Update preview. Freed cash does not change the path by itself. The offer stays temporary |
| 3 — Remaining edges | Card-payment labeling, print, and save during a failed refresh, on synthetic data | A card payment is labeled once. Print matches the version on screen. Save waits for the calculated result |

A database migration is provisional. The version is JSON on the existing plan row, so a new optional property may not need one. Stop and ask before generating a migration or rewriting stored documents. Detail panel keyboard focus stays tracked. Do not start Phase 4.

## Findings

### 1. The safety answer sits below the editor

Observed. `PlanWorkingScenario` renders before `PlanTabs` in `PlanPageClient`. At 1440×900 the shortfall amount started below the fold. At 390×844 the safety section started about 1,300px down, after a scenario block about 950px tall. Try changes is three links; the editor never moves there.

Impact: the person sees fields and a comparison before the question they opened Plan to answer.

Change: put Overview, Try changes, and Your plan directly under the heading. One status sits with the heading. The safety figure is the first content. The editor, Update preview, and Reset move to Try changes. On a desktop, safety is the main column and the action, debt outlook, and issues are the supporting column. Remove the `self-start` shrink that leaves Other issues about 289px wide.

Acceptance: at 1440×768 and 390×844, the safety amount is visible before any scenario field. The only scenario text on Overview is the heading status.

### 2. Review shortfall opens a different calculation

Observed. The shortfall action in `planSafetyAction` opens the cash outlook. That outlook is titled typical pay, warns that cash first goes below zero on its own date, and says the lowest point is not necessarily the first day below zero. Overview's figure is the steady-pay lowest balance after money set aside, on the lowest-balance date. The two dates and the two balances differed on the signed-in household. Your plan's first action uses the cash-outlook date. Assumptions list Spendable as zero because the stored figure is floored.

Impact: the person follows the action for the number they just read and has to reconcile another result.

Change: Review shortfall explains the Overview figure, including the amount, the lowest-balance date, and money set aside. Typical pay is offered after that explanation, under its own name. Do not invent a date or amount. More labels on the current two screens do not replace this path.

Acceptance: Review shortfall repeats the Overview amount and lowest-balance date before any typical-pay chart.

### 3. A saved version does not contain the shortfall amount

Source-derived. `SpendableEstimate` keeps `LowestAvailable`, which stays negative, and `Spendable`, which is floored at zero. `SavedPlanComposer.Facts` copies `Spendable` and `AlreadyShort`. It does not copy `LowestAvailable` or the date of that low. `ShortfallOn` is the cash-forecast date. `DecisionPlanRules.Milestones` stores that date with a null amount. `Guardrail.Shortfall` is the extra-payment warning and is zero when that warning is off. `DecisionPlanFacts` and `DecisionPlanFactsDto` have no field for the Overview amount. The same composer builds the working Your plan, so the loss happens before a version is saved.

Impact: changing `decisionFactLines` cannot show the shortfall for a version that never stored it. Filling the gap from today's household calculation would rewrite history.

Change: new decision plans store `lowestAvailable` and its date. Your plan shows that captured amount when `AlreadyShort` is true. An older document says the amount was not stored. Leave the old JSON unchanged. If the field needs a new column, stop for migration approval. If it stays an optional JSON property, confirm `DecisionPlanJson` still opens older documents.

Acceptance: a version saved after the change shows the amount it captured. An older version does not show an amount taken from the current calculation.

### 4. Payoff Unknown sits above dated payoffs

Observed. With no edits, the desktop comparison showed the same shortfall in both value cells and Unknown for payoff and total interest, with no Current facts, Trying, or Change headings. Those headings appear only below the small breakpoint. Debt payoff listed dated payoffs, said one debt was not plotted, and said the top edge is the plotted balances. The comparison's payoff is the all-debts-free date in `planScenarioComparison`. One debt that cannot be dated makes that date unknown while individual payoffs remain dated. The last plotted row can say the payment becomes available for other uses while the omitted debt is still outside the chart.

Impact: the person can believe nothing has a payoff date, or that the last plotted payment frees cash while a left-out debt remains.

Change: before any edit, show a baseline sentence instead of two matching columns. After an edit, label Current facts, Trying, and Change, including on a desktop. An unknown all-debts date names the first dated payoff and the debt that blocks a full date.

Acceptance: a household with no edits does not see two identical unlabeled columns. The unknown full payoff points at the blocker and the first dated payoff.

### 5. Phone tabs cover the safety heading

Observed. Plan's view bar is sticky under the phone header. Scrolling the safety title to the top of the page left it under the header and the tabs. The tabs extended to about 128px while the title sat at the top of the scrollport.

Impact: the answer the person scrolled to is hidden by the controls for the views.

Change: keep the view bar from covering the safety heading. The heading status and the safety figure remain readable when the page is scrolled to that answer.

Acceptance: at 390×844, the safety heading and amount stay visible when that section is the scroll target.

### 6. Plan setup is a stack of sparse cards

Observed. At 1440×768 the five groups were single-column panels, about 138–186px tall and 704px wide, inside a main area about 1,169px wide. Living costs, money to protect, and extra-money preferences use Edit. Income and bills use specific labels. The page has no Back to Plan. On a phone the header showed the account control, and the Plan tab was the return. The debt-terms issue shown on Plan is absent from the debts row, which shows a count.

Impact: setup takes a long scroll for a few facts, and the edit that fixes a Plan warning is a generic Edit on a different page.

Change: compact full-width rows, specific actions such as Edit living costs, the existing debt-terms warning on that group, and Back to Plan. Widen the desktop bound if a row needs it to scan. Do not make two columns inside the current narrow width. Bills and debts stay separate editors.

Acceptance: each group shows its saved facts, a specific edit, and an issue when Plan already has one. The return reaches Plan. Rows remain readable at desktop width.

### 7. Keep one apply rule

Source-derived, and a decision for increment 2. Freed cash, payment priority, and the other scenario fields all wait for Update preview today. The October 16 payoff review described switching among the three returned freed-cash paths without another update. Doing that beside choices that still need the button would make two gestures. `canSaveWorkingScenario` already requires the draft to match the applied preview.

Impact: an instant freed-cash switch would let the screen and the saved choice diverge, or teach two different ways to apply a change.

Change: every choice stays pending until Update preview. The result says when the selection is not applied. Paths already returned may answer that update without another request, and the screen and the save payload change together. Save stays blocked while they differ. Replace "Avalanche stays the recommended order" and "Utilization cash" in `planPayoffChoice` with who is paid first and what the interest difference means. When a full payoff date is unknown, name the debt that blocks it.

Acceptance: a freed-cash choice does not change the visible path by itself. Two edits apply together only after Update preview. Save matches the figures on screen.

### 8. Card payments, print, and a failed save remain unchecked

The loaded Activity list had no Card payment stamp, including a card-account row whose description read as a payment. The account type and amount sign were not opened, so this does not establish that `isCardPaymentActivity` missed a payment. Print, a failed preview, and save during that failure were not exercised. The offer form was opened and not submitted. It says the comparison is temporary, uses current minimums, and is not saved. The constant-rate sentence is in How this comparison works.

Change: increment 3 checks these on synthetic data. Put the constant-rate limit beside the rate field in increment 2. Do not save the offer.

Acceptance: a known card payment is labeled once and left out of category spending. Print names the version on screen. Save waits until the preview has been calculated.

## Verification and limits

- The signed-in walkthrough covered Plan's Overview, Try changes, Your plan, cash outlook, debt payoff, and offer form, plus Plan setup, Home, Activity, and Allocation. Desktop and phone layouts were measured. No financial record changed.
- A development hydration notice pointed at `PageHeader`. It was not confirmed as a mismatch a signed-in person sees outside the dev overlay.
- The sign-in heading read "Sign in to My Application." That is outside this repair sequence.
- No compilation or application tests ran. This change is the report and the docs status.

## Manual acceptance for increment 1

These checks are pending.

1. At 1440×768 and 390×844, read the safety amount before any scenario field. The only scenario text on Overview is the heading status. The phone heading is not covered by the view bar.
2. Choose Review shortfall. The amount and lowest-balance date match Overview. Typical pay comes afterward and is named as a different view.
3. Open an older saved version. It does not show a shortfall amount taken from today's calculation. A version saved after the contract change shows the amount it captured.
4. With no edits, the comparison does not show two matching unlabeled columns. Plan setup rows are easy to scan at desktop width and name each edit.

## Paste-ready prompt for increment 1

```text
Implement Phase 3 UX increment 1 only: make the next-month answer the first thing on Plan.

Read AGENTS.md, frontend/AGENTS.md, docs/README.md, docs/reviews/README.md,
docs/reviews/2026-10-16-003-phase-3-ux-reassessment.md,
docs/design/plan-page.md, docs/design/ui-direction.md, docs/roadmap.md,
and the applicable .cursor/rules files first.

The person has recorded their money and needs to know whether the next 30 days
are covered. Keep the current visual style, tokens, and primitives. Keep Home,
Accounts, Activity, and Plan. Do not start Phase 4, add a dependency, or change
financial records. Do not run a migration unless a later approval says to.

Put Overview, Try changes, and Your plan directly under the Plan heading.
One status sits with that heading. Overview then leads with next-30-day safety
and one next action. Do not put a scenario summary above or below that figure.
On desktop, use a primary safety column and a smaller column for the action,
debt outlook, and issues. On a phone, use that same order in one column.
Move the full editor to Try changes.

Review shortfall explains the steady-pay shortfall just shown: the amount,
the lowest-balance date, and money set aside. Offer typical pay only after
that, under its own name. Do not open typical pay as the review itself.

The saved decision plan stores floored Spendable and AlreadyShort. It does
not store lowestAvailable. Add that amount and its date for new versions.
An older version says the amount was not stored. Do not recompute it from
current facts, and do not rewrite the old document. A migration is not
authorized. If the change needs a column, stop and ask. If it stays an
optional JSON property, confirm older documents still open.

Before any edits, do not show two identical comparison columns. When a
preview differs, label Current facts, Trying, and Change. An unknown
all-debts date must point to the first dated payoff and the debt that
blocks a full date. Do not invent a date or amount.

On Plan setup, use compact full-width rows. Widen the desktop bound if
scanning needs it. Do not split the narrow column into two. Use specific
edit actions, the existing debt-terms issue on that group, and a return
to Plan. Preserve separate editors.

Do not implement increment 2 or 3. Every scenario choice, including freed
cash, still waits for Update preview. Offers stay temporary. Verify focused
behavior and compilation, give Zach a short manual checklist, write one
review, and update the docs status and index. Stop for approval.
```

## Approval

Approved by Zach on October 16, 2026. Increment 1 is the next implementation. This approval does not start it.

## Pending decision and chat handoff

Approve or revise increment 1. Stop here. A new chat can start from `AGENTS.md`, `docs/README.md`, `docs/reviews/README.md`, this report, and `docs/roadmap.md`. Phase 3 behavior QA remains pending. Detail panel keyboard focus stays tracked. Do not start Phase 4.

The increment 1 prompt above does not skip an earlier Phase 3 item. Items 1–11 and the three approved UX increments are already recorded. Phase 4 stays unscheduled.
