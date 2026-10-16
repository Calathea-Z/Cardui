import { moneyDigits } from "@/features/accounts/formatCurrency";
import type {
  PayoffConstraintKind,
  PayoffPriorityChoice,
  PayoffRolloverKind,
  PlanScenarioQuery,
} from "@/lib/api/types";
import { parsePlanExtra } from "./planExtra";

/**
 * A parsed optional money field.
 * Empty keeps the saved amount. Invalid is not sent. An amount includes zero.
 */
export type ParsedScenarioAmount =
  | { state: "empty" }
  | { state: "amount"; amount: number }
  | { state: "invalid" };

/**
 * True when the preview matches the plan loaded with the page.
 * Zero extra, no income loss, no windfall, and null spending and floor keep the saved figures.
 * Avalanche, no constraint, and no reclaim amount keep the baseline payoff.
 * A missing priority or reclaim amount is that baseline, so an older preview still matches.
 */
export function isSavedPlanScenario(scenario: PlanScenarioQuery) {
  return (
    scenario.monthlyExtra <= 0 &&
    scenario.incomeLossMonths === 0 &&
    scenario.windfallAmount <= 0 &&
    scenario.windfallOn === null &&
    scenario.livingSpendingMonthly === null &&
    scenario.cashFloor === null &&
    (scenario.priority ?? "Avalanche") === "Avalanche" &&
    (scenario.constraintDebtId ?? null) === null &&
    (scenario.constraintKind ?? null) === null &&
    (scenario.reclaimAmount ?? 0) <= 0
  );
}

/**
 * Reads the months-without-pay field.
 * Blank is zero, so every paycheck stays. A whole number from 0 to 18 is that many months. Anything else is not sent.
 */
export function parseIncomeLossMonths(value: string) {
  const trimmed = value.trim();
  if (!trimmed) {
    return 0;
  }

  if (!/^\d{1,2}$/.test(trimmed)) {
    return null;
  }

  const months = Number(trimmed);
  if (!Number.isInteger(months) || months > 18) {
    return null;
  }

  return months;
}

/**
 * Reads an optional money field.
 * Blank keeps the saved amount. Zero is an explicit amount. A negative amount or other text is invalid.
 */
export function parseScenarioAmount(value: string): ParsedScenarioAmount {
  const trimmed = value.trim();
  if (!trimmed) {
    return { state: "empty" };
  }

  const digits = moneyDigits(trimmed);
  if (digits === null) {
    return { state: "invalid" };
  }

  const parsed = Number(digits);
  if (!Number.isFinite(parsed) || parsed < 0) {
    return { state: "invalid" };
  }

  return { state: "amount", amount: Math.round(parsed * 100) / 100 };
}

/**
 * The last day of the 18-month forecast that starts on asOf.
 * That day is the day before the same calendar day 18 months later. A short month keeps its last day.
 * An unusable date returns null.
 */
export function planForecastEnd(asOf: string) {
  const start = parseIsoDate(asOf);
  if (!start) {
    return null;
  }

  const end = addMonths(start, 18);
  end.setUTCDate(end.getUTCDate() - 1);
  return formatIsoDate(end);
}

/**
 * Reads the windfall amount and date together.
 * Both blank clears the windfall. A positive amount needs a date from asOf through the forecast end.
 * An unusable amount, a missing date, or a date outside that window is not sent.
 */
export function parseWindfall(
  amountText: string,
  date: string,
  asOf: string,
  forecastEnd: string,
) {
  const amount = parseScenarioAmount(amountText);
  const trimmedDate = date.trim();
  if (amount.state === "invalid") {
    return null;
  }

  if (amount.state === "empty" || amount.amount === 0) {
    return trimmedDate ? null : { amount: 0, on: null };
  }

  if (!trimmedDate || trimmedDate < asOf || trimmedDate > forecastEnd) {
    return null;
  }

  return { amount: amount.amount, on: trimmedDate };
}

/**
 * The message when months without pay is not a whole number from 0 to 18.
 */
export function incomeLossInvalid() {
  return "Enter a whole number of months from 0 to 18.";
}

/**
 * The message when a preview amount is not blank, zero, or positive.
 */
export function scenarioAmountInvalid() {
  return "Enter a zero or positive amount, or leave it blank.";
}

/**
 * The message when a windfall is missing its amount or a date inside the forecast.
 */
export function windfallInvalid() {
  return "Enter an amount and a date inside the 18-month forecast, or clear both.";
}

/**
 * Reads a YYYY-MM-DD date as UTC.
 * A day that does not exist returns null.
 */
function parseIsoDate(value: string) {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!match) {
    return null;
  }

  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  const date = new Date(Date.UTC(year, month - 1, day));
  if (
    date.getUTCFullYear() !== year ||
    date.getUTCMonth() !== month - 1 ||
    date.getUTCDate() !== day
  ) {
    return null;
  }

  return date;
}

/**
 * Adds calendar months the way the forecast does.
 * A 31st in a shorter month lands on that month's last day.
 */
function addMonths(date: Date, months: number) {
  const target = new Date(
    Date.UTC(date.getUTCFullYear(), date.getUTCMonth() + months, 1),
  );
  const lastDay = new Date(
    Date.UTC(target.getUTCFullYear(), target.getUTCMonth() + 1, 0),
  ).getUTCDate();
  const day = Math.min(date.getUTCDate(), lastDay);
  return new Date(Date.UTC(target.getUTCFullYear(), target.getUTCMonth(), day));
}

/**
 * Whether living costs or the cash floor use the saved amount or an entered amount.
 * Saved is explicit. An entered amount includes zero, so blank is not a hidden zero.
 */
export type ScenarioAmountChoice = "saved" | "amount";

/**
 * The working scenario before Update preview sends it.
 * Text stays as entered so a failed update can keep it.
 */
export type ScenarioDraft = {
  monthlyExtra: string;
  incomeLossMonths: string;
  windfallAmount: string;
  windfallOn: string;
  livingChoice: ScenarioAmountChoice;
  livingAmount: string;
  floorChoice: ScenarioAmountChoice;
  floorAmount: string;
  priority: PayoffPriorityChoice;
  constraintDebtId: string;
  constraintKind: PayoffConstraintKind | "";
  reclaimAmount: string;
  kind: PayoffRolloverKind;
};

/**
 * Which fields cannot be sent yet.
 * Null means that field is valid. Update preview sends nothing until every field is null.
 */
export type ScenarioDraftErrors = {
  monthlyExtra: "extra" | null;
  incomeLossMonths: "months" | null;
  windfall: "windfall" | null;
  living: "living-empty" | "living-invalid" | "living-unset" | null;
  floor: "floor-empty" | "floor-invalid" | null;
  constraint: "constraint" | null;
  reclaim: "reclaim" | null;
};

/**
 * No field errors. A successful read clears the previous ones.
 */
export const emptyScenarioDraftErrors: ScenarioDraftErrors = {
  monthlyExtra: null,
  incomeLossMonths: null,
  windfall: null,
  living: null,
  floor: null,
  constraint: null,
  reclaim: null,
};

/**
 * The preview that keeps saved spending, the saved floor, and minimum payments.
 * Rollover is the current-facts payoff behavior.
 */
export function currentFactsScenario(): PlanScenarioQuery {
  return {
    monthlyExtra: 0,
    incomeLossMonths: 0,
    windfallAmount: 0,
    windfallOn: null,
    livingSpendingMonthly: null,
    cashFloor: null,
    priority: "Avalanche",
    constraintDebtId: null,
    constraintKind: null,
    reclaimAmount: 0,
  };
}

/**
 * True when the applied preview is still current facts.
 * A different payoff behavior is a change even when the amounts match the saved plan.
 */
export function isCurrentFacts(
  scenario: PlanScenarioQuery,
  kind: PayoffRolloverKind,
) {
  return isSavedPlanScenario(scenario) && kind === "Rollover";
}

/**
 * Builds the draft shown for an applied preview.
 * Saved spending and floor stay on "Use saved amount". Zero extra and zero months are shown as digits.
 */
export function draftFromApplied(
  scenario: PlanScenarioQuery,
  kind: PayoffRolloverKind,
): ScenarioDraft {
  return {
    monthlyExtra: String(scenario.monthlyExtra),
    incomeLossMonths: String(scenario.incomeLossMonths),
    windfallAmount:
      scenario.windfallAmount > 0 ? String(scenario.windfallAmount) : "0",
    windfallOn: scenario.windfallOn ?? "",
    livingChoice: scenario.livingSpendingMonthly === null ? "saved" : "amount",
    livingAmount:
      scenario.livingSpendingMonthly === null
        ? ""
        : String(scenario.livingSpendingMonthly),
    floorChoice: scenario.cashFloor === null ? "saved" : "amount",
    floorAmount: scenario.cashFloor === null ? "" : String(scenario.cashFloor),
    priority: scenario.priority,
    constraintDebtId: scenario.constraintDebtId ?? "",
    constraintKind: scenario.constraintKind ?? "",
    reclaimAmount:
      scenario.reclaimAmount > 0 ? String(scenario.reclaimAmount) : "0",
    kind,
  };
}

/**
 * The draft for current facts.
 */
export function currentFactsDraft(): ScenarioDraft {
  return draftFromApplied(currentFactsScenario(), "Rollover");
}

/**
 * Reads every working-scenario field together.
 * One invalid field rejects the whole update. `savedLiving` blocks an amount when no flexible spending is saved.
 */
export function readScenarioDraft(
  draft: ScenarioDraft,
  asOf: string,
  savedLiving: number,
):
  | { ok: true; scenario: PlanScenarioQuery; kind: PayoffRolloverKind }
  | { ok: false; errors: ScenarioDraftErrors } {
  const errors = { ...emptyScenarioDraftErrors };
  const monthlyExtra = parsePlanExtra(draft.monthlyExtra);
  if (monthlyExtra === null) {
    errors.monthlyExtra = "extra";
  }

  const incomeLossMonths = parseIncomeLossMonths(draft.incomeLossMonths);
  if (incomeLossMonths === null) {
    errors.incomeLossMonths = "months";
  }

  const forecastEnd = planForecastEnd(asOf);
  const windfall = forecastEnd
    ? parseWindfall(draft.windfallAmount, draft.windfallOn, asOf, forecastEnd)
    : null;
  if (!windfall) {
    errors.windfall = "windfall";
  }

  let livingSpendingMonthly: number | null = null;
  if (draft.livingChoice === "amount") {
    if (savedLiving <= 0) {
      errors.living = "living-unset";
    } else {
      const living = parseScenarioAmount(draft.livingAmount);
      if (living.state === "empty") {
        errors.living = "living-empty";
      } else if (living.state === "invalid") {
        errors.living = "living-invalid";
      } else {
        livingSpendingMonthly = living.amount;
      }
    }
  }

  let cashFloor: number | null = null;
  if (draft.floorChoice === "amount") {
    const floor = parseScenarioAmount(draft.floorAmount);
    if (floor.state === "empty") {
      errors.floor = "floor-empty";
    } else if (floor.state === "invalid") {
      errors.floor = "floor-invalid";
    } else {
      cashFloor = floor.amount;
    }
  }

  const constraintDebtId = draft.constraintDebtId.trim();
  const constraintKind = draft.constraintKind;
  const hasConstraintDebt = constraintDebtId.length > 0;
  const hasConstraintKind = constraintKind !== "";
  if (
    draft.priority === "Constraint" &&
    (!hasConstraintDebt || !hasConstraintKind)
  ) {
    errors.constraint = "constraint";
  }

  let reclaimAmount = 0;
  const reclaim = parseScenarioAmount(draft.reclaimAmount);
  if (draft.kind === "Reclaim") {
    if (reclaim.state !== "amount" || reclaim.amount <= 0) {
      errors.reclaim = "reclaim";
    } else {
      reclaimAmount = reclaim.amount;
    }
  } else if (reclaim.state === "amount") {
    reclaimAmount = reclaim.amount;
  }

  if (
    errors.monthlyExtra ||
    errors.incomeLossMonths ||
    errors.windfall ||
    errors.living ||
    errors.floor ||
    errors.constraint ||
    errors.reclaim ||
    monthlyExtra === null ||
    incomeLossMonths === null ||
    !windfall
  ) {
    return { ok: false, errors };
  }

  return {
    ok: true,
    kind: draft.kind,
    scenario: {
      monthlyExtra,
      incomeLossMonths,
      windfallAmount: windfall.amount,
      windfallOn: windfall.on,
      livingSpendingMonthly,
      cashFloor,
      priority: draft.priority,
      constraintDebtId:
        hasConstraintDebt && hasConstraintKind ? constraintDebtId : null,
      constraintKind:
        hasConstraintDebt && hasConstraintKind ? constraintKind : null,
      reclaimAmount,
    },
  };
}

/**
 * True when the draft would send the preview already on screen.
 * An invalid draft does not match, so it cannot be saved as that preview.
 */
export function draftMatchesApplied(
  draft: ScenarioDraft,
  applied: PlanScenarioQuery,
  kind: PayoffRolloverKind,
  asOf: string,
  savedLiving: number,
) {
  const read = readScenarioDraft(draft, asOf, savedLiving);
  if (!read.ok || read.kind !== kind) {
    return false;
  }

  return sameScenarioQuery(read.scenario, applied);
}

/**
 * How Update preview applies one draft.
 * `returned` uses the paths already on the report. `request` sends one recovery for every changed field, and the path travels with it.
 */
export type PreviewUpdate =
  | { action: "invalid"; errors: ScenarioDraftErrors }
  | {
      action: "returned" | "request";
      scenario: PlanScenarioQuery;
      kind: PayoffRolloverKind;
    };

/**
 * Decides whether Update preview can use the report already on screen.
 * A matching scenario switches the freed-cash path locally. Any other edit, including a second change beside that path, is one request. An invalid field sends nothing.
 */
export function previewUpdate(
  draft: ScenarioDraft,
  applied: PlanScenarioQuery,
  asOf: string,
  savedLiving: number,
): PreviewUpdate {
  const read = readScenarioDraft(draft, asOf, savedLiving);
  if (!read.ok) {
    return { action: "invalid", errors: read.errors };
  }

  return {
    action: sameScenarioQuery(read.scenario, applied) ? "returned" : "request",
    scenario: read.scenario,
    kind: read.kind,
  };
}

/**
 * True when Your plan may save the working scenario.
 * Updating, a failed update, and edits that are not in the preview cannot be saved.
 */
export function canSaveWorkingScenario(input: {
  status: "ready" | "updating" | "error";
  draft: ScenarioDraft;
  applied: PlanScenarioQuery;
  kind: PayoffRolloverKind;
  asOf: string;
  savedLiving: number;
}) {
  return (
    input.status === "ready" &&
    draftMatchesApplied(
      input.draft,
      input.applied,
      input.kind,
      input.asOf,
      input.savedLiving,
    )
  );
}

/**
 * True when two previews would request the same recovery.
 */
function sameScenarioQuery(left: PlanScenarioQuery, right: PlanScenarioQuery) {
  return (
    left.monthlyExtra === right.monthlyExtra &&
    left.incomeLossMonths === right.incomeLossMonths &&
    left.windfallAmount === right.windfallAmount &&
    left.windfallOn === right.windfallOn &&
    left.livingSpendingMonthly === right.livingSpendingMonthly &&
    left.cashFloor === right.cashFloor &&
    left.priority === right.priority &&
    left.constraintDebtId === right.constraintDebtId &&
    left.constraintKind === right.constraintKind &&
    left.reclaimAmount === right.reclaimAmount
  );
}

/**
 * True when a stored priority is one of the four alternatives.
 */
export function isPayoffPriority(
  value: unknown,
): value is PayoffPriorityChoice {
  return (
    value === "Avalanche" ||
    value === "MinimumRelease" ||
    value === "Utilization" ||
    value === "Constraint"
  );
}

/**
 * True when a stored constraint direction is pay first or pay last.
 */
export function isPayoffConstraint(
  value: unknown,
): value is PayoffConstraintKind {
  return value === "PayFirst" || value === "PayLast";
}

/**
 * Formats a UTC date as YYYY-MM-DD.
 */
function formatIsoDate(date: Date) {
  const month = String(date.getUTCMonth() + 1).padStart(2, "0");
  const day = String(date.getUTCDate()).padStart(2, "0");
  return `${date.getUTCFullYear()}-${month}-${day}`;
}
