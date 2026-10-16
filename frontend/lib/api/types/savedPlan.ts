import type {
  PayoffConstraintKind,
  PayoffPriorityChoice,
  PayoffRolloverKind,
} from "./plan";

export const planVersionStatuses = ["Draft", "Accepted", "Superseded"] as const;

export type PlanVersionStatus = (typeof planVersionStatuses)[number];

export const decisionPlanActionKinds = [
  "CoverCashShortfall",
  "LowerExtra",
  "AddStartingCash",
  "AddDebt",
  "AddIncome",
  "FinishDebtTerms",
  "RefreshBalances",
  "HoldIncomeLoss",
  "HoldWindfall",
  "PayFirstDebt",
  "UseFreedPayment",
  "ConfirmBills",
  "SetFlexibleSpending",
  "ChooseCashToKeep",
] as const;

export type DecisionPlanActionKind = (typeof decisionPlanActionKinds)[number];

export const decisionPlanDetailKinds = [
  "Order",
  "Payments",
  "Interest",
  "PayoffMonth",
  "DueDates",
  "FreedPayment",
  "CashOutlook",
  "Spendable",
  "LowPay",
  "LeftOut",
  "Currency",
  "Limit",
  "IncomeLoss",
  "Windfall",
  "SpendingChange",
  "CashFloorChange",
  "ScenarioOrder",
  "CashShortfall",
  "ReserveShortfall",
  "ExtraGuardrail",
  "PayoffConditional",
  "DoesNotPayDown",
  "HorizonReached",
  "ExcludedCurrency",
  "StaleBalance",
  "IncomeLossRisk",
  "WindfallNotApplied",
  "WindfallLeftOut",
  "NoReliableIncome",
  "MissingBalance",
  "UnknownRate",
  "UnknownMinimum",
  "UnknownDueDate",
  "NoIncome",
  "NoBills",
  "NoFlexibleSpending",
  "NoCashFloor",
  "PaymentReview",
  "VariableIncomeLeftOut",
  "UncertainIncomeLeftOut",
  "IncomeLeftOut",
  "DebtLeftOut",
  "FactsChange",
  "ShortfallDate",
  "IncomeLossEnds",
  "RefreshBalances",
  "ZeroBalanceMinimum",
  "WindfallArrives",
  "EmergencyChoice",
] as const;

export type DecisionPlanDetailKind = (typeof decisionPlanDetailKinds)[number];

export const decisionPlanMilestoneKinds = [
  "CashShortfall",
  "CashRecovered",
  "ReserveShortfall",
  "FirstPayoff",
  "DebtFree",
  "BreathingRoom",
] as const;

export type DecisionPlanMilestoneKind =
  (typeof decisionPlanMilestoneKinds)[number];

/**
 * Whether the extra payment leaves ordinary spending or a near-term bill or debt payment short.
 * Shortfall is how far below zero that day falls, and it is zero when the guardrail is off.
 */
export type DecisionPlanGuardrailDto = {
  active: boolean;
  ordinaryExpenses: boolean;
  nearTermObligations: boolean;
  on: string | null;
  shortfall: number;
  alreadyShortWithoutExtra: boolean;
};

/**
 * Cash, budget, and payoff figures captured for one decision plan.
 * Living spending and the cash floor are the amounts that plan used. Zero means it had none.
 * A null date means that event does not happen in the calculation.
 */
export type DecisionPlanFactsDto = {
  startingCash: number;
  startingReserve: number;
  startingAvailable: number;
  startingCashAccountCount: number;
  startingCashOldestAsOf: string | null;
  staleCashCount: number;
  hasIncome: boolean;
  hasBills: boolean;
  livingSpendingMonthly: number;
  cashFloor: number;
  hasEmergencyGoal: boolean;
  namedSavingsGoalCount: number;
  staleDebtCount: number;
  paymentReviewCount: number;
  spendable: number;
  alreadyShort: boolean;
  guardrail: DecisionPlanGuardrailDto;
  shortfallOn: string | null;
  recoveredOn: string | null;
  reserveShortfallOn: string | null;
  startingObligation: number | null;
  recurringRoom: number;
  paidOffOn: string | null;
  totalInterest: number;
};

/**
 * The cash preview selected for a decision plan.
 * A null spending or floor amount means the plan used the household's saved figure.
 * `priority` is who extra and a rolled payment follow. An older saved plan is avalanche.
 * `reclaimAmount` is how much freed cash the plan keeps each month. Zero unless it keeps some.
 */
export type DecisionPlanScenarioDto = {
  monthlyExtra: number;
  incomeLossMonths: number;
  windfallAmount: number;
  windfallOn: string | null;
  livingSpendingMonthly: number | null;
  cashFloor: number | null;
  priority: PayoffPriorityChoice;
  constraintDebtId: string | null;
  constraintKind: PayoffConstraintKind | null;
  reclaimAmount: number;
};

/**
 * One debt in the payoff order captured for a decision plan.
 * A null minimum, rate, or payoff date was unknown.
 */
export type DecisionPlanDebtDto = {
  debtId: string;
  name: string;
  balance: number;
  minimum: number | null;
  apr: number | null;
  stop: string;
  paidOffOn: string | null;
};

/**
 * One modeled payoff captured for a decision plan.
 * startsOn is null when the freed-payment date falls outside the projection.
 */
export type DecisionPlanStepDto = {
  debtId: string;
  name: string;
  endedOn: string;
  startsOn: string | null;
  minimum: number;
  breathingRoom: number;
};

/**
 * One ranked action. Order starts at 1.
 * For LowerExtra, count is 1 for ordinary spending, 2 for a near-term bill or debt payment, and 3 for both.
 * alreadyShortWithoutExtra is true only for that extra-payment action.
 */
export type DecisionPlanActionDto = {
  order: number;
  kind: DecisionPlanActionKind;
  name: string | null;
  nextName: string | null;
  amount: number | null;
  extra: number | null;
  on: string | null;
  count: number | null;
  alreadyShortWithoutExtra: boolean;
};

/**
 * One assumption, risk, gap, or review condition.
 * Name, amount, date, and count are null when that kind does not use them.
 */
export type DecisionPlanDetailDto = {
  kind: DecisionPlanDetailKind;
  name: string | null;
  amount: number | null;
  on: string | null;
  count: number | null;
};

/**
 * One milestone the calculation produced.
 * A null date or amount was not calculated.
 */
export type DecisionPlanMilestoneDto = {
  kind: DecisionPlanMilestoneKind;
  name: string | null;
  amount: number | null;
  on: string | null;
};

/**
 * A decision plan and the facts it was written from.
 * Later edits to a debt or a bill do not change a saved copy of this value.
 */
export type DecisionPlanDto = {
  calculationVersion: string;
  asOf: string;
  planningCurrency: string;
  kind: PayoffRolloverKind;
  facts: DecisionPlanFactsDto;
  scenario: DecisionPlanScenarioDto;
  debts: DecisionPlanDebtDto[];
  steps: DecisionPlanStepDto[];
  missingBalanceNames: string[];
  excludedCurrencies: string[];
  actions: DecisionPlanActionDto[];
  milestones: DecisionPlanMilestoneDto[];
  assumptions: DecisionPlanDetailDto[];
  risks: DecisionPlanDetailDto[];
  missing: DecisionPlanDetailDto[];
  reviewConditions: DecisionPlanDetailDto[];
};

/**
 * One saved plan version.
 * acceptedAt and supersededAt are null until that change happens.
 */
export type PlanVersionDto = {
  id: string;
  number: number;
  status: PlanVersionStatus;
  createdAt: string;
  acceptedAt: string | null;
  supersededAt: string | null;
  plan: DecisionPlanDto;
};

/**
 * The scenario to store as a draft.
 * replacesDraftId is null only when the household has no open draft.
 */
export type SavePlanVersionDto = {
  kind: PayoffRolloverKind;
  monthlyExtra: number;
  incomeLossMonths: number;
  windfallAmount: number;
  windfallOn: string | null;
  livingSpendingMonthly: number | null;
  cashFloor: number | null;
  priority: PayoffPriorityChoice;
  constraintDebtId: string | null;
  constraintKind: PayoffConstraintKind | null;
  reclaimAmount: number;
  replacesDraftId: string | null;
};
