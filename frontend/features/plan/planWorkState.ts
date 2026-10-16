import type { PayoffRolloverKind, PlanScenarioQuery } from "@/lib/api/types";

const storageKey = "cardui.plan.work";

/**
 * A store with the sessionStorage shape.
 * Tests pass a memory store. The page passes the browser session store.
 */
export type PlanWorkStore = {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
};

/**
 * The working preview kept while the person edits setup and comes back.
 * It is the last applied scenario, not an unsaved keystroke and not a saved plan.
 */
export type PlanWorkState = {
  scenario: PlanScenarioQuery;
  kind: PayoffRolloverKind;
};

/**
 * Reads the working preview from the browser session.
 * A missing, unreadable, or incomplete value is ignored so the page uses current facts.
 */
export function readPlanWork(
  store: PlanWorkStore | null,
): PlanWorkState | null {
  if (!store) {
    return null;
  }

  try {
    const raw = store.getItem(storageKey);
    if (!raw) {
      return null;
    }

    const parsed = JSON.parse(raw) as Partial<PlanWorkState>;
    if (!isScenario(parsed.scenario) || !isKind(parsed.kind)) {
      return null;
    }

    return {
      scenario: normalizeScenario(parsed.scenario),
      kind: parsed.kind,
    };
  } catch {
    return null;
  }
}

/**
 * Stores the working preview for this browser session.
 * Leaving Plan for an editor can restore it. It is not written to the household.
 */
export function writePlanWork(
  store: PlanWorkStore | null,
  state: PlanWorkState,
) {
  store?.setItem(storageKey, JSON.stringify(state));
}

/**
 * Drops the stored preview once the page is back on current facts.
 */
export function clearPlanWork(store: PlanWorkStore | null) {
  store?.removeItem(storageKey);
}

/**
 * True when the stored kind is keep none, keep some, or keep all.
 */
function isKind(value: unknown): value is PayoffRolloverKind {
  return value === "Rollover" || value === "Reclaim" || value === "ReclaimAll";
}

/**
 * True when the stored scenario has the fields the recovery request sends.
 * A partial object is ignored rather than sent with missing amounts.
 */
function isScenario(value: unknown): value is PlanScenarioQuery {
  if (!value || typeof value !== "object") {
    return false;
  }

  const scenario = value as Partial<PlanScenarioQuery>;
  return (
    typeof scenario.monthlyExtra === "number" &&
    typeof scenario.incomeLossMonths === "number" &&
    typeof scenario.windfallAmount === "number" &&
    (scenario.windfallOn === null || typeof scenario.windfallOn === "string") &&
    (scenario.livingSpendingMonthly === null ||
      typeof scenario.livingSpendingMonthly === "number") &&
    (scenario.cashFloor === null || typeof scenario.cashFloor === "number") &&
    (scenario.priority === undefined || isPriority(scenario.priority)) &&
    (scenario.constraintKind === undefined ||
      scenario.constraintKind === null ||
      isConstraint(scenario.constraintKind)) &&
    (scenario.reclaimAmount === undefined ||
      typeof scenario.reclaimAmount === "number")
  );
}

/**
 * Fills payoff choices an older session did not store.
 * A missing priority is avalanche, and a missing reclaim amount keeps none.
 */
function normalizeScenario(scenario: PlanScenarioQuery): PlanScenarioQuery {
  const partial = scenario as Partial<PlanScenarioQuery>;
  return {
    monthlyExtra: scenario.monthlyExtra,
    incomeLossMonths: scenario.incomeLossMonths,
    windfallAmount: scenario.windfallAmount,
    windfallOn: scenario.windfallOn,
    livingSpendingMonthly: scenario.livingSpendingMonthly,
    cashFloor: scenario.cashFloor,
    priority: isPriority(partial.priority) ? partial.priority : "Avalanche",
    constraintDebtId:
      typeof partial.constraintDebtId === "string"
        ? partial.constraintDebtId
        : null,
    constraintKind: isConstraint(partial.constraintKind)
      ? partial.constraintKind
      : null,
    reclaimAmount:
      typeof partial.reclaimAmount === "number" ? partial.reclaimAmount : 0,
  };
}

/**
 * True when the stored priority is one of the four alternatives.
 */
function isPriority(value: unknown): value is PlanScenarioQuery["priority"] {
  return (
    value === "Avalanche" ||
    value === "MinimumRelease" ||
    value === "Utilization" ||
    value === "Constraint"
  );
}

/**
 * True when the stored constraint is pay first or pay last.
 */
function isConstraint(
  value: unknown,
): value is NonNullable<PlanScenarioQuery["constraintKind"]> {
  return value === "PayFirst" || value === "PayLast";
}
