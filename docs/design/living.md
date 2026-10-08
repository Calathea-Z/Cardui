# Plan budget

Status: Current
Updated: 2026-10-08

## Problem

Phase 2 item 8. The household can say how much of each person's pay the
shared plan may use and set one realistic monthly amount for flexible living
spending. Those choices stay in the plan. They do not require every past
transaction to be categorized.

Plan budget is not a second category budget. It supplies two planning inputs:
shared pay and one monthly flexible-spending total.

## Screen

Plan budget is a setting in the account menu, at `/living`, after Debts and before
Savings. It is not a tab.

Pay available to the plan lists each person already on Household. The entered
amount is today's monthly benchmark. Blank uses all recorded pay; zero shares
none. The calculator derives that person's current share of scheduled pay and
keeps the same share when pay is low or a raise takes effect. Paycheck dates
stay unchanged. Pay with no person stays fully shared and is named.

Flexible monthly spending is one household amount for groceries, gas, hobbies,
and other flexible spending that is not already on Bills. It stores a monthly
amount, the day it counts, and what is left this month. It can optionally
follow one cash account. The current-month amount and account controls are
secondary details because the monthly amount is the main decision.

Monthly affordability compares shared pay with scheduled bills, known debt
minimums on positive balances, and flexible monthly spending. A saved minimum
on a zero-balance debt is retained for review and excluded. It is a yearly
average, not cash on a date. Irregular bills, unknown minimums, and another
currency are named and left out. Plan remains the home for the dated cash
outlook and payoff scenarios.

## What belongs where

- A scheduled obligation with a due date belongs on Bills.
- A debt payment belongs on Debts, not Bills. Entering it in both places can
  count the same payment twice.
- Flexible monthly spending belongs in the one amount on Plan budget.
- Cash that must remain protected and a goal with a target date belong on
  Savings.
- Spending targets compare categorized posted activity with category intentions. They
  do not add another spending amount to Plan.

## Paychecks

The entered monthly contribution establishes a share of today's scheduled pay.
That share is applied to each real paycheck, including low pay and a later
raise. A biweekly paycheck is not rewritten as a deposit on the 1st.
The screen keeps the entered benchmark exact and separately explains any
few-cent difference in the monthly average produced by dated paychecks.

An amount with no scheduled paycheck is stored and does not invent a deposit.
A paycheck without a schedule still arrives on its own date. Pay in another
currency stays out of the monthly average.

Saving a contribution does not change the stored paycheck and does not move
money.

## Plan

The cash chart stays cash in accounts. Flexible monthly spending leaves cash on
its ready day. After that day has passed, only what is left this month leaves
cash, on today. Later months count the full monthly amount. It does not finish.
Cash outlook names the monthly amount and links to Plan budget.
Savings contributions raise the protected reserve instead; Spending targets do not feed
the forecast.

The previous Kept for life list and its payoff comparison were removed. They
duplicated flexible monthly spending and modeled sending all ordinary living
spending to debt, which is not a realistic affordability alternative. Spending
change scenarios belong on Plan.

## Data

`HouseholdContributors.MonthlyContribution` stores the current monthly
benchmark. Null means all recorded pay is shared.

Monthly living spending remains the one `SavingsGoals` row whose kind is
`Operating`. Existing data is preserved. The applied
`20261008152308_AddLivingContributionsAndAllowances` migration added
`MonthlyContribution` and an empty `LifeAllowances` table. The applied
`20261008154357_SimplifyLivingModel` migration removes that table. It does not
change contributions, savings goals, transactions, or balances.
