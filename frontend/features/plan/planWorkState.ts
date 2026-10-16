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

    return { scenario: parsed.scenario, kind: parsed.kind };
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
 * True when the stored kind is one of the two payoff paths the page can select.
 */
function isKind(value: unknown): value is PayoffRolloverKind {
  return value === "Rollover" || value === "ReclaimAll";
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
    (scenario.cashFloor === null || typeof scenario.cashFloor === "number")
  );
}
