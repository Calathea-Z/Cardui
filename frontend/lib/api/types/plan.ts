/**
 * Which way a path treats cash freed by a paid-off debt.
 * Rollover sends it to the next debt. Reclaim keeps a chosen amount. ReclaimAll keeps every freed dollar.
 * This page shows Rollover and ReclaimAll. Reclaim matches Rollover while the chosen amount is zero.
 */
export const payoffRolloverKinds = [
  "Rollover",
  "Reclaim",
  "ReclaimAll",
] as const;

export type PayoffRolloverKind = (typeof payoffRolloverKinds)[number];

/**
 * One payoff that removes a monthly obligation.
 * `startsOn` is the date the minimum is no longer paid. Null means that date falls outside the projection.
 * `breathingRoom` is the recurring freed cash after this step. It does not include shared extra.
 */
export type CashFlowRecoveryStepDto = {
  debtId: string;
  name: string;
  endedOn: string;
  startsOn: string | null;
  minimum: number;
  extra: number;
  amount: number;
  breathingRoomAdded: number;
  breathingRoom: number;
};

/**
 * One path from payoff to breathing room.
 * `startingObligation` is the known minimums before any payoff, and null when every minimum is unknown.
 * `remainingObligation` is the known minimums still due, and null when every remaining minimum is unknown.
 * `recurringRoom` is the monthly amount after the last change, including shared extra once every debt is paid off.
 */
export type CashFlowRecoveryPathDto = {
  kind: PayoffRolloverKind;
  steps: CashFlowRecoveryStepDto[];
  startingObligation: number | null;
  remainingObligation: number | null;
  unknownRemaining: number;
  breathingRoom: number;
  releasedExtra: number;
  recurringRoom: number;
  explanation: string;
};

/**
 * Cash-flow recovery for the signed-in household.
 * `monthlyExtra` and `reclaimAmount` are zero until a saved choice exists.
 * `excludedCurrencies` are codes left out of the planning currency.
 * `hasDebts` is false when no debt is recorded. A debt with no balance still counts, and it is left out of the payoff.
 */
export type CashFlowRecoveryReportDto = {
  planningCurrency: string;
  monthlyExtra: number;
  reclaimAmount: number;
  rollover: CashFlowRecoveryPathDto;
  reclaim: CashFlowRecoveryPathDto;
  reclaimAll: CashFlowRecoveryPathDto;
  excludedCurrencies: string[];
  assumptions: string[];
  hasDebts: boolean;
};
