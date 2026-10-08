# Savings

Status: Current
Updated: 2026-10-08

## Problem

Phase 2 item 7. The household can set monthly living spending, a cash floor, an emergency goal, and named goals for costs that have a date. Living spending and the floor are stored, so Plan's cash outlook no longer leaves those inputs at zero.

Reserving money is not a second expense. Moving money between accounts the household owns is not income. The forecast already treats a savings amount that way: it raises the protected reserve and leaves cash unchanged. What is left to spend is cash minus that reserve.

## Screen

Savings is a setting in the account menu, at `/savings`, after Living and before Targets. It is not a tab. Targets stays the monthly spending page.

The page title is Savings. Cash to keep and Emergency are always there. Named goals are a list under Saving for. Save for something is the one primary action. A goal opens in a panel from 768px up and a full-screen sheet under it.

Monthly living spending is edited on Living, at `/living`. The stored row is still an operating goal. Savings does not show that card.

Monthly living spending is the one flexible-spending total used by Plan. It stores a monthly amount, the day of the month that spending counts (Ready by, default the 1st), and how much is available now. It leaves cash on that day. After the day has passed, only what is left this month leaves cash, on today. Later months count the full monthly amount. It does not finish, and it does not join the protected pile.

Cash to keep is a floor. It stores the amount to always keep available, and how much is available now. The plan protects the full floor. It does not leave cash and it has no date. A household has one of each card. Emergency and each named goal still have a target and a date.

An empty card says what is missing and offers Set. Removing a row asks first and names it. Names under Saving for are unique in the household, ignoring case. The stored kinds are `Operating`, `Floor`, `Emergency`, and `Sinking`.

## What a goal stores

Monthly living spending stores a monthly amount greater than zero, a ready-by day from 1 to 31, and available now. Cash to keep stores a floor greater than zero and available now. It has no date.

Emergency and each named goal store:

- A target amount, required and greater than zero.
- A target date, required.
- How much is already set aside. Blank means zero.
- An optional cash account to follow.

The page shows the monthly amount needed to hit the date. That amount is calculated, not stored. A goal that is already funded adds nothing further. A date that has passed shows the gap as due now. A date too far out to schedule says so. Amounts use the currency stored on the goal. A new goal uses the household planning currency.

Saving a goal does not create a transaction, a bill, or income, and it does not change an account balance.

## Already set aside

The person can type the amount. They can also connect one cash account.

While a goal follows an account, the plan uses that account's balance. Typing a different amount keeps that number until they choose Use account balance. Stopping the follow leaves the amount that was in use on the goal. The link does not move money.

Only a cash account in the planning currency can be followed. A blank currency counts. The account is active and not archived. One account can back one goal. A credit card, a loan, or an investment cannot be followed. A negative balance counts as nothing set aside, and the goal says the account is below zero.

An account that can no longer be followed stays named on the goal. The amount already stored is what the plan uses until the person chooses another account or no account.

## Plan

The cash chart stays the cash in accounts. The line under Cash outlook names how much is set aside today and what is left after it, when any amount is set aside. What is left to spend going below zero is a warning, separate from cash itself going below zero.

The protected amount starts as cash already set aside for emergency and named goals, plus the full cash-to-keep floor, in the planning currency. Each dated goal that is not funded yet adds its calculated amounts on their dates. Those amounts raise the reserve and leave the cash total unchanged. Monthly living spending is not in that pile. It leaves cash on its ready day. A goal in another currency is left out and named.

## Data

One `SavingsGoals` row per goal: household, kind (`Operating`, `Floor`, `Emergency`, or `Sinking`), name, optional target amount and date, optional monthly amount and ready day, optional floor amount, reserved amount, currency, optional account, when the follow started, and when the person last typed their own amount. Deleting the household deletes its goals. Deleting the account clears the link.

`20261008134702_AddSavingsGoals`, `20261008135119_AddSavingsGoalHouseholdIndexes`, and `20261008143017_AddEverydaySpendingAndCashToKeep` are applied. The last one makes the target amount and target date optional, and adds the monthly amount, the ready day, the floor amount, and a unique index so a household has one cash floor. It does not rewrite existing rows. A row saved under the old everyday-spending target has to be set again.
