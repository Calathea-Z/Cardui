import type { SpendableStatus } from "./spendableCopy";
import { planReturnHref } from "./planReturn";

/**
 * The one next step under the 30-day safety answer.
 * `view` opens a Plan detail or Try changes. `href` opens an existing editor and comes back to Plan.
 */
export type PlanSafetyAction = {
  key: string;
  title: string;
  detail: string;
  label: string;
  view?: "shortfall" | "changes";
  href?: string;
};

type PlanSafetyInput = {
  status: SpendableStatus;
  hasDebts: boolean;
  debtAttentionCount: number;
  noCash: boolean;
  noIncome: boolean;
  spendingUnset: boolean;
};

/**
 * Chooses the next action from the steady-pay safety result.
 * A shortfall opens the steady-pay explanation for the figure just shown. Missing facts open the editor that supplies them.
 * A covered window offers a change to try. It does not send a shortfall to the budget editor.
 */
export function planSafetyAction(input: PlanSafetyInput): PlanSafetyAction {
  if (input.status === "short") {
    return {
      key: "shortfall",
      title: "Review the shortfall",
      detail:
        "The shortfall is the lowest balance after money set aside in this 30-day window.",
      label: "Review shortfall",
      view: "shortfall",
    };
  }

  if (input.noCash) {
    return {
      key: "cash",
      title: "Add starting cash",
      detail:
        "Plan starts from active cash accounts in the household currency.",
      label: "Review Accounts",
      href: planReturnHref("/accounts"),
    };
  }

  if (input.noIncome) {
    return {
      key: "income",
      title: "Add steady pay",
      detail:
        "This estimate counts steady pay only. Without it, the figure is incomplete.",
      label: "Review Income",
      href: planReturnHref("/income"),
    };
  }

  if (input.debtAttentionCount > 0) {
    return {
      key: "debts",
      title: "Fix debt terms",
      detail: `${input.debtAttentionCount} ${input.debtAttentionCount === 1 ? "debt needs" : "debts need"} a term before the payoff estimate is complete.`,
      label: "Fix debt terms",
      href: planReturnHref("/debts"),
    };
  }

  if (input.spendingUnset) {
    return {
      key: "budget",
      title: "Set living costs",
      detail: "Ordinary spending is not set, so this estimate may be high.",
      label: "Review living costs",
      href: planReturnHref("/living"),
    };
  }

  if (!input.hasDebts) {
    return {
      key: "debts",
      title: "Add a debt",
      detail: "Add a card or loan when this plan should include payoff.",
      label: "Review Debts",
      href: planReturnHref("/debts"),
    };
  }

  return {
    key: "changes",
    title: "Try a change",
    detail:
      "See what an extra payment, income change, or cash change does to this preview.",
    label: "Try a change",
    view: "changes",
  };
}
