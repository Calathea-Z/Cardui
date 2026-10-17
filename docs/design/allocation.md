# Allocation

Status: Current
Updated: 2026-10-17

## Problem

The rest of Phase 3 item 6. The household can save a rule for extra variable pay and a rule for a windfall, then review what those rules would do. The rules are preferences. Tortoise does not move money, pay a debt, or change an account balance.

Temporary Plan cash scenarios stay temporary. A windfall tried on Plan still stays in cash.

## Screen

Allocation is a setting in the account menu, at `/allocation`, after Savings and before Spending targets. It is not a tab.

The page title is Allocation. The primary action is Save allocation. Variable income and Windfall are the two rules. Each asks for an extra-debt percent and a savings percent. The share that stays available is shown and is not a third field.

The variable-income review lists each source marked Variable. A strong payment above typical, in the planning currency, shows how one surplus payment would split. A source in another currency is named and left out. A variable source with no strong amount above typical is named and not split. Steady and uncertain pay stay whole and are not listed.

The windfall review states the percents. It does not invent a dollar amount. It says the Plan preview still keeps a windfall in cash.

An unsaved page shows zeros and says no rule is saved yet. Saving zeros is an explicit choice to leave the whole amount available. A failed load keeps that empty page and says the load failed.

## What a rule stores

Both rules store two whole percents from 0 to 100. Debt and savings together cannot pass 100. The rest stays available. Blank, a decimal, and a negative percent are rejected. The default before the first save is zero and zero.

Saving does not create a transaction, a bill, a debt payment, or a savings deposit, and it does not change an account balance. It does not change the Plan forecast.

## Data

One `AllocationPreferences` row per household: the four percents and when they were saved. Deleting the household deletes the row. No row means the preference is not saved.

`20261015193812_AddAllocationPreferences` adds that table and no other columns. It is applied.
