import type {
  DecisionPlanActionDto,
  DecisionPlanActionKind,
  DecisionPlanDto,
  DecisionPlanMilestoneDto,
} from "@/lib/api/types";
import { planReturnHref } from "./planReturn";

/**
 * Assumptions the working plan is using before the person has supplied them.
 * A calculation rule, a left-out paycheck, and an unchosen emergency target stay off this list.
 * Those stay beside the 30-day figure or on Your plan, because agreeing with them does not clear a row.
 */
export const planConfirmationKinds = [
  "NoBills",
  "NoFlexibleSpending",
  "NoCashFloor",
] as const;

export type PlanConfirmationKind = (typeof planConfirmationKinds)[number];

/**
 * One assumption the overview asks the person to settle.
 * `href` returns to Plan. The row clears when that fact is saved, not when the link is opened.
 */
export type PlanConfirmationItem = {
  kind: PlanConfirmationKind;
  term: string;
  detail: string;
  href: string;
  label: string;
};

/**
 * What Overview adds under the 30-day action.
 * `safetyLeads` is true when that action is already the plan's first ranked step.
 * `hiddenIssueKeys` are other-issue rows this column already states.
 */
export type PlanFollowThrough = {
  actions: DecisionPlanActionDto[];
  confirmations: PlanConfirmationItem[];
  milestones: DecisionPlanMilestoneDto[];
  payoffDatesConditional: boolean;
  safetyLeads: boolean;
  hiddenIssueKeys: string[];
};

/**
 * The working decision preview and the 30-day action already on screen.
 * `safetyLabel` distinguishes "Add a debt" from "Fix debt terms".
 * `reserveShort` keeps the protected-savings issue when cash after those protections runs short.
 */
export type PlanFollowThroughInput = {
  plan: DecisionPlanDto | null;
  safetyKey: string;
  safetyLabel: string;
  reserveShort: boolean;
};

const confirmationTargets: Record<
  PlanConfirmationKind,
  { path: string; label: string; term: string; detail: string }
> = {
  NoBills: {
    path: "/bills",
    label: "Review Bills",
    term: "Bills",
    detail:
      "No recurring non-debt bills are included. Add the ones you pay. Debt payments stay on Debts.",
  },
  NoFlexibleSpending: {
    path: "/living",
    label: "Review living costs",
    term: "Flexible spending",
    detail:
      "Monthly flexible spending is not set, so the 30-day figure may be high.",
  },
  NoCashFloor: {
    path: "/savings",
    label: "Review Savings",
    term: "Cash to keep",
    detail:
      "Cash to keep is not set. Available cash can fall to the last dollar.",
  },
};

/**
 * Chooses the ranked actions, milestones, and unset assumptions for Overview.
 * The 30-day action is left out of the list. An assumption already stated by that action, or by a listed action, is left out too.
 * A preview from an older scenario is not passed in.
 */
export function planFollowThrough(
  input: PlanFollowThroughInput,
): PlanFollowThrough {
  const plan = input.plan;
  if (!plan) {
    return emptyFollowThrough();
  }

  const covered = coveredActionKinds(input.safetyKey, input.safetyLabel);
  const actions = plan.actions.filter((action) => !covered.has(action.kind));
  const shown = actions.map((action) => action.kind);
  const confirmations = confirmationItems(plan, covered, shown);

  return {
    actions,
    confirmations,
    milestones: plan.milestones,
    payoffDatesConditional: payoffDatesAreConditional(plan),
    safetyLeads: plan.actions.some(
      (action, index) => index === 0 && covered.has(action.kind),
    ),
    hiddenIssueKeys: issueKeysAlreadyShown(
      shown,
      confirmations.map((item) => item.kind),
      input.reserveShort,
    ),
  };
}

/**
 * An empty follow-through.
 * Used before the working preview has been calculated.
 */
function emptyFollowThrough(): PlanFollowThrough {
  return {
    actions: [],
    confirmations: [],
    milestones: [],
    payoffDatesConditional: false,
    safetyLeads: false,
    hiddenIssueKeys: [],
  };
}

/**
 * The decision-plan action the 30-day card already asks for.
 * "Add a debt" is the only safety label that covers adding a debt. The other debts label covers missing terms.
 */
function coveredActionKinds(
  safetyKey: string,
  safetyLabel: string,
): ReadonlySet<DecisionPlanActionKind> {
  switch (safetyKey) {
    case "shortfall":
      return new Set(["CoverCashShortfall"]);
    case "cash":
      return new Set(["AddStartingCash"]);
    case "income":
      return new Set(["AddIncome"]);
    case "budget":
      return new Set(["SetFlexibleSpending"]);
    case "debts":
      return new Set(
        safetyLabel === "Add a debt" ? ["AddDebt"] : ["FinishDebtTerms"],
      );
    default:
      return new Set();
  }
}

/**
 * Unset bills, flexible spending, and cash to keep, in the order the plan stored them.
 * A matching ranked action already asks for the same fact, so the assumption is not repeated.
 */
function confirmationItems(
  plan: DecisionPlanDto,
  covered: ReadonlySet<DecisionPlanActionKind>,
  shown: readonly DecisionPlanActionKind[],
): PlanConfirmationItem[] {
  const chosen: PlanConfirmationItem[] = [];
  for (const detail of [...plan.missing, ...plan.reviewConditions]) {
    if (!isConfirmationKind(detail.kind)) {
      continue;
    }

    if (
      chosen.some((item) => item.kind === detail.kind) ||
      alreadyAsked(detail.kind, covered, shown)
    ) {
      continue;
    }

    const target = confirmationTargets[detail.kind];
    chosen.push({
      kind: detail.kind,
      term: target.term,
      detail: target.detail,
      href: planReturnHref(target.path),
      label: target.label,
    });
  }

  return chosen;
}

/**
 * True when this stored kind is one of the three overview assumptions.
 */
function isConfirmationKind(
  kind: DecisionPlanDto["missing"][number]["kind"],
): kind is PlanConfirmationKind {
  return planConfirmationKinds.some((item) => item === kind);
}

/**
 * True when the 30-day action or a listed next action already asks for this fact.
 */
function alreadyAsked(
  kind: PlanConfirmationKind,
  covered: ReadonlySet<DecisionPlanActionKind>,
  shown: readonly DecisionPlanActionKind[],
): boolean {
  const action = confirmationAction(kind);
  return action !== null && (covered.has(action) || shown.includes(action));
}

/**
 * The ranked action that settles one assumption.
 * A kind with no ranked action can only be cleared by saving the fact.
 */
function confirmationAction(
  kind: PlanConfirmationKind,
): DecisionPlanActionKind | null {
  switch (kind) {
    case "NoBills":
      return "ConfirmBills";
    case "NoFlexibleSpending":
      return "SetFlexibleSpending";
    case "NoCashFloor":
      return "ChooseCashToKeep";
    default:
      return null;
  }
}

/**
 * True when a payoff milestone is on the list and the plan marks that date conditional.
 * Cash milestones alone do not add the note.
 */
function payoffDatesAreConditional(plan: DecisionPlanDto) {
  const dated = plan.milestones.some(
    (milestone) =>
      milestone.kind === "FirstPayoff" || milestone.kind === "DebtFree",
  );
  return dated && plan.risks.some((risk) => risk.kind === "PayoffConditional");
}

/**
 * Other-issue rows that would repeat an action or an assumption already in this column.
 * A ranked debt action hides the summary row. The named debt can still be listed.
 * A protected-savings shortfall stays, even when cash to keep is also unset.
 */
function issueKeysAlreadyShown(
  shown: readonly DecisionPlanActionKind[],
  confirmations: readonly PlanConfirmationKind[],
  reserveShort: boolean,
): string[] {
  const keys: string[] = [];
  if (shown.includes("AddStartingCash")) {
    keys.push("cash");
  }

  if (shown.includes("AddIncome")) {
    keys.push("income");
  }

  if (shown.includes("FinishDebtTerms") || shown.includes("AddDebt")) {
    keys.push("debts");
  }

  if (shown.includes("ConfirmBills") || confirmations.includes("NoBills")) {
    keys.push("bills");
  }

  if (
    shown.includes("SetFlexibleSpending") ||
    confirmations.includes("NoFlexibleSpending")
  ) {
    keys.push("budget");
  }

  if (
    !reserveShort &&
    (shown.includes("ChooseCashToKeep") ||
      confirmations.includes("NoCashFloor"))
  ) {
    keys.push("savings");
  }

  return keys;
}
