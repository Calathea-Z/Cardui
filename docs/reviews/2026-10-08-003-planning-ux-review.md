Date: 2026-10-08
Status: Approved 2026-10-08
PR: None

# Planning UX review

## Scope and perspective

Zach requested a signed-in click-through of `/plan`, `/debts`, and the unfinished `/savings`. Persona: someone trying to get out of debt who needs help with unfamiliar financial decisions and confidence in a realistic plan.

This is a UX assessment, not approval of the savings increment or verification of the financial calculations. Recommendations below are proposals, not implementation decisions. No application code or financial records were changed. Personal account names, balances, and screenshots are intentionally omitted from this repository report.

## Overall assessment

The screens are calm and readable, and Plan has useful scenario controls. However, they currently work better as an inventory and calculator than as a guided debt-recovery experience. The user must supply difficult answers, interpret warnings, reconcile differing totals, and translate charts into an action plan.

The priority is to connect three questions: what should I do next, can I afford it, and what information still needs checking?

## Findings, in priority order

### 1. Plan gives reassurance without making its limits prominent

Observed: the summary says "You're on track" and presents an exact debt-free date. The cash outlook says cash stays above zero. The expanded calculation notes describe typical pay, bills, and debt payments; they do not clearly explain where everyday variable spending is represented. Savings was empty, with no visible prompt on Plan to review that omission.

Impact: a beginner can read a successful projection as confirmation that their whole budget is affordable. This review does not establish that the math is wrong; the visible explanation is insufficient to judge completeness.

Proposal: label the result "Estimated debt-free date" and put key assumptions and missing budget inputs beside it. Show an understandable monthly breakdown of income, bills, everyday spending, debt payments, savings, and money remaining. Distinguish unknown amounts from confirmed zero amounts. Explain whether new card spending is assumed to stop.

### 2. There is no clear near-term action plan

Observed: Plan leads with path controls, a summary, and charts. Payoff-order rows highlight a chart band. There is no visible consolidated instruction for the next payment cycle: which debt gets extra, how much goes to each debt, and what remains available for living expenses.

Impact: knowing a distant finish date does not answer "What do I do with my next paycheck?"

Proposal: put a short "Your next step" section above the charts, with a payment breakdown and an explanation of the selected priority. Link directly to any inputs that need confirmation. Clearly state that planned payments are not scheduled bank payments.

### 3. Savings asks the user to solve the hard questions alone

Observed: everyday spending and emergency setup both ask for a target, target date, amount already set aside, and optional account. No visible guidance helps determine a realistic amount or date. The primary Savings action is "Save for something," while essential spending and emergency setup have generic "Set" buttons.

Impact: someone who does not know how much to reserve cannot confidently begin. The interface gives optional goals more prominence than establishing a workable foundation.

Proposal: offer an estimate-building path with editable assumptions. For emergencies, help the user choose a starter milestone and see how it affects debt repayment; do not invent a universal target. Explain the tradeoff before asking for a deadline.

### 4. Everyday spending needs a distinct meaning and behavior

Observed: the heading says Everyday spending, but the form describes an amount to accumulate by a date and ends with "Save goal."

Impact: a recurring monthly spending allowance and a minimum cash cushion are different concepts. A user cannot tell which this represents or how it relates to Bills and Targets.

Proposal: decide between a monthly allowance and an ongoing cash cushion before changing the form. A monthly allowance should ask for the monthly amount and only ask about recurring timing if calculations use it. A cushion should explain the desired minimum available balance. Explain which expenses belong here so users do not count bills twice.

### 5. Debt and Plan totals need reconciliation

Observed: a debt with a zero current balance still displays a nonzero minimum. Debts' total minimums differs from Plan's payment amount. The count of high-rate debts includes zero-balance entries. "Synced" appears beside the balance, while APR, minimum, and due date remain separately editable in the form.

Impact: a beginner may not know whether a payment is still required or which total to trust. A synced label can be mistaken for verification of every field.

Proposal: explicitly distinguish synced balance from manually maintained payment terms, explain excluded debts and differences between totals, and ask for review of potentially stale minimums. Do not automatically erase a stated minimum solely because the current balance is zero. Separate active debt warnings from zero-balance accounts.

### 6. Scenario controls show results but not the comparison

Observed: switching between Rollover and Keep freed payments materially changes the payoff estimate and interest. A temporary extra amount also changes those results. No compact summary states how much sooner or how much interest is avoided. The extra field updates after leaving the field, not while it remains focused.

Impact: the user must remember the previous numbers, perform the comparison, and decide affordability unaided. Typing without leaving the field can initially appear to do nothing.

Proposal: show a baseline comparison, such as time saved and estimated interest avoided, beside the revised result. Explain the effect on available cash. Use plain-language path names such as "Keep paying the same total" and "Lower payments as debts finish." Give a clear recalculation cue or explicit preview action.

### 7. Rollover language can misrepresent spendable money

Observed: even with rollover selected, payoff rows say money is "freed a month," and a chart is called "Minimums and breathing room." The summary calls the rollover outcome "minimums only."

Impact: a user may think each paid-off debt releases money to spend, though its payment is redirected. "Minimums only" may suggest declining total payments rather than maintaining the initial payment budget.

Proposal: distinguish the required minimum, planned total payment, amount redirected to another debt, and money actually available to spend. Use scenario-specific row copy such as "Redirected to your next debt" when rollover is on.

### 8. Warnings identify problems without a recovery step

Observed: Debts shows utilization thresholds and a count of high rates. The most prominent per-debt actions are Stop following, Edit, and Remove. The list begins with a zero-balance debt rather than emphasizing a near-term payment or active payoff priority.

Impact: warnings can add anxiety without helping the user act. Maintenance controls dominate the choices a person sees while trying to reduce debt.

Proposal: pair warnings with a short explanation and a relevant next step. Give active debts useful ordering by due date or planning priority, identify zero-balance accounts separately, and put maintenance actions in a secondary menu. Avoid presenting a generic utilization threshold as the user's debt-payoff strategy.

### 9. Form copy explains storage rules more than user decisions

Observed: labels include Recorded, Revolving, APR, Linked account, and Stop following. Help text includes "A blank is not stored as zero," "This is not a second expense," and "The same debts always give the same dates and cents." Debt setup does allow unknown terms to remain blank, which is useful.

Impact: the user learns implementation details without necessarily learning where to find the requested information or how uncertainty changes the plan.

Proposal: use "Total owed," "Credit card or credit line," "Interest rate (APR)," and "Update balance from an account" where appropriate. Add guidance for locating rate and minimum on a statement. Preserve the ability to say "I don't know" and show the resulting plan limitation. Keep concise reassurance that saving a goal does not move money.

### 10. An account balance is not necessarily savings allocated to one goal

Observed: savings help text says choosing an account uses its balance until the user types their own amount. It does not visibly explain whether the entire account should be dedicated to this goal or how to handle cash shared between bills and savings.

Impact: selecting a checking account may appear to set aside money that is also needed for everyday expenses.

Proposal: clearly ask whether to use the entire balance or a specified portion, explain shared-account allocation, and show the resulting amount still available for other uses. This is a clarity finding; saving and allocation behavior were not exercised.

### 11. Core planning inputs are hard to discover from Plan

Observed: in the narrow layout, Debts and Savings are under the account menu in Settings. The populated Plan had no obvious direct edit links to them. The initial wider Savings view also showed Plan below the visible portion of a short independently scrolling primary-nav area.

Impact: financial planning inputs can be mistaken for account preferences, and returning to fix assumptions requires knowing the navigation structure.

Proposal: expose contextual links to debts, living costs, income, and savings from Plan. Recheck primary navigation overflow at desktop sizes before treating the initial observation as a reproducible layout defect.

### 12. Forecast detail can overwhelm the main decision

Observed: on the narrow screen, debt composition and balance charts precede the payoff list and cash outlook. The latter then includes 6-, 12-, and 18-month blocks, with another series under the low-pay disclosure. The payoff chart begins in the prior month without an obvious explanation. The cash starting amount and first end-of-day low differ without a nearby event breakdown.

Impact: the important affordability answer sits well below the first screen, and unexplained timing differences can reduce confidence even when mathematically valid.

Proposal: put an actionable summary and near-term cash safety first, with longer projections as supporting detail. Explain starting cash versus end-of-day cash and show the contributing events when inspected. Use estimated months in headline forecasts rather than implying certainty through distant exact dates.

## What is already working

- Calm styling, readable cards, and consistent form surfaces.
- A prominent payoff estimate gives the user a meaningful outcome.
- Rollover, extra-payment, and lower-income scenarios provide useful foundations for decision support.
- Debt setup explicitly allows unknown terms instead of forcing false zero values.
- Savings says that saving a goal does not move money.
- Progressive disclosure keeps promotional terms and calculation details out of the initial view.

## Agent verification

- Used the running signed-in browser UI; login assistance was not needed.
- Read all three routes, opened everyday-spending, emergency, and named-goal forms, and inspected the date picker.
- Opened Add debt, promotional terms, and Edit debt for a followed account; did not submit changes.
- Switched both Plan paths, expanded calculation and lower-pay details, and selected a payoff row to verify chart highlighting.
- Entered a temporary synthetic extra-payment amount, observed recalculation after blur, cleared it, and verified restoration of the baseline summary. Returned to Savings through the account menu.
- Inspected rendered screenshots at the browser's supplied widths. Most of the review was in the narrow layout; no comprehensive responsive or keyboard audit was performed.
- No application records were created, edited, or deleted. No migrations, builds, automated test suites, or calculation audits were run. Existing uncommitted work was preserved.
- Documentation-only changes: this report, the review index, and the current-work status block.

## Manual checks awaiting Zach

These checks need no saved data changes; an extra-payment preview is temporary.

1. Read Plan as a newcomer and explain the next payment action without consulting the charts. Expected design outcome: debt, amount, timing, and remaining spending money are clear.
2. Compare Debts' minimum total with Plan's planned payment. Expected design outcome: any difference is explained and zero-balance debts do not create uncertainty about what is due.
3. Try explaining Everyday spending versus Bills, Targets, and Emergency. Expected design outcome: each has a distinct purpose and avoids double counting.
4. Inspect both desktop and phone navigation. Expected design outcome: Plan is easy to find and its financial inputs can be reached without searching account settings.

## Approval and pending decision

Approved by Zach on October 8, 2026, with the other reviews that were
waiting. This approves the assessment. It does not schedule the proposals
that later increments did not already build. Everyday spending, Plan
affordability, and the bounded closure were handled in the simplified
Living review, the Phase 2 UX closure, and the Plan page refinement.
Anything still only proposed here stays unscheduled. Phase 3 item 6 is
next.

This chat can continue for discussion of the findings. Once an implementation increment is chosen and approved, a new chat can start from AGENTS.md, docs/README.md, docs/reviews/README.md, and this report; the normal roadmap-order handoff check still applies.
