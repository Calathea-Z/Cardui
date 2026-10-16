import type { AllocationRuleDto } from "@/lib/api/types";
import { planReturnHref } from "@/features/plan/planReturn";

/**
 * Formats a money amount in the planning currency.
 */
type FormatMoney = (amount: number) => string;

/**
 * One person's saved contribution share.
 * `monthlyAmount` null means full recorded pay stays shared.
 */
export type PlanSetupContribution = {
  monthlyAmount: number | null;
};

/**
 * The saved facts Plan setup summarizes.
 * A false availability flag means that editor's load failed, so the count is not shown as zero.
 */
export type PlanSetupInput = {
  currency: string;
  incomeAvailable: boolean;
  sourceCount: number;
  sharesAvailable: boolean;
  contributions: PlanSetupContribution[];
  billsAvailable: boolean;
  billCount: number;
  debtsAvailable: boolean;
  debtCount: number;
  livingAvailable: boolean;
  livingMonthly: number | null;
  savingsAvailable: boolean;
  floorAmount: number | null;
  emergencyName: string | null;
  emergencyTarget: number | null;
  namedGoalCount: number;
  allocationAvailable: boolean;
  allocationSaved: boolean;
  variableIncome: AllocationRuleDto | null;
  windfall: AllocationRuleDto | null;
  debtTermsIssue: string | null;
};

/**
 * One editor link inside a setup group.
 */
export type PlanSetupAction = {
  label: string;
  href: string;
};

/**
 * One setup group: a saved summary and the existing editors that change it.
 * `issue` is a warning Plan already has for this group. Null means none was found.
 */
export type PlanSetupGroup = {
  key: string;
  title: string;
  lines: string[];
  issue: string | null;
  actions: PlanSetupAction[];
};

/**
 * Builds the five setup groups from saved facts.
 * Each group keeps its own editor. Nothing here merges income, bills, debts, or savings into one record.
 */
export function planSetupGroups(
  input: PlanSetupInput,
  money: FormatMoney,
): PlanSetupGroup[] {
  return [
    incomeGroup(input),
    billsGroup(input),
    livingGroup(input, money),
    protectGroup(input, money),
    allocationGroup(input),
  ];
}

/**
 * Pay sources and contribution shares, with a separate edit for each screen.
 */
function incomeGroup(input: PlanSetupInput): PlanSetupGroup {
  return {
    key: "income",
    title: "Income",
    lines: [
      input.incomeAvailable ? sourceLine(input.sourceCount) : unread,
      input.sharesAvailable ? shareLine(input.contributions) : unread,
    ],
    issue: null,
    actions: [
      { label: "Edit pay", href: planReturnHref("/income", "setup") },
      { label: "Edit shares", href: planReturnHref("/living", "setup") },
    ],
  };
}

/**
 * Bills and debts as two lines, each with its own editor.
 */
function billsGroup(input: PlanSetupInput): PlanSetupGroup {
  return {
    key: "bills",
    title: "Bills and debt payments",
    lines: [
      input.billsAvailable
        ? countLine(input.billCount, "bill", "bills")
        : unread,
      input.debtsAvailable
        ? countLine(input.debtCount, "debt", "debts")
        : unread,
    ],
    issue: input.debtTermsIssue,
    actions: [
      { label: "Edit bills", href: planReturnHref("/bills", "setup") },
      { label: "Edit debts", href: planReturnHref("/debts", "setup") },
    ],
  };
}

/**
 * The one flexible monthly spending amount.
 */
function livingGroup(
  input: PlanSetupInput,
  money: FormatMoney,
): PlanSetupGroup {
  const line = !input.livingAvailable
    ? unread
    : input.livingMonthly === null
      ? "Flexible monthly spending is not set."
      : `Flexible monthly spending is ${money(input.livingMonthly)} a month.`;
  return {
    key: "living",
    title: "Living costs",
    lines: [line],
    issue: null,
    actions: [
      { label: "Edit living costs", href: planReturnHref("/living", "setup") },
    ],
  };
}

/**
 * Cash to keep, the emergency goal, and named goals.
 */
function protectGroup(
  input: PlanSetupInput,
  money: FormatMoney,
): PlanSetupGroup {
  const lines = input.savingsAvailable
    ? [
        input.floorAmount === null
          ? "Cash to keep is not set."
          : `Cash to keep is ${money(input.floorAmount)}.`,
        input.emergencyName === null
          ? "No emergency goal is saved."
          : input.emergencyTarget === null
            ? `${input.emergencyName} is saved without a target amount.`
            : `${input.emergencyName} target is ${money(input.emergencyTarget)}.`,
        countLine(input.namedGoalCount, "named goal", "named goals") + ".",
      ]
    : [unread];
  return {
    key: "protect",
    title: "Money to protect",
    lines,
    issue: null,
    actions: [
      {
        label: "Edit money to protect",
        href: planReturnHref("/savings", "setup"),
      },
    ],
  };
}

/**
 * The saved variable-pay and windfall splits.
 * An unsaved page stays unsaved instead of looking like a chosen split.
 */
function allocationGroup(input: PlanSetupInput): PlanSetupGroup {
  const lines = !input.allocationAvailable
    ? [unread]
    : !input.allocationSaved || !input.variableIncome || !input.windfall
      ? ["Not saved."]
      : [
          splitLine("Variable pay", input.variableIncome),
          splitLine("Windfall", input.windfall),
        ];
  return {
    key: "allocation",
    title: "Extra money preferences",
    lines,
    issue: null,
    actions: [
      {
        label: "Edit extra-money preferences",
        href: planReturnHref("/allocation", "setup"),
      },
    ],
  };
}

const unread = "Could not be read. Open the editor to see what is saved.";

/**
 * The debt-terms warning Plan already shows, written for the debts row.
 * `count` is the number of debts in that list. Null means the plan could not be read, so no warning is invented.
 * Zero stays quiet.
 */
export function planSetupDebtTermsIssue(count: number | null): string | null {
  if (count == null || count === 0) {
    return null;
  }

  const noun = count === 1 ? "debt needs" : "debts need";
  return `${count} ${noun} a term before the payoff estimate is complete.`;
}

/**
 * Names how many pay sources are saved.
 */
function sourceLine(count: number) {
  return count === 1 ? "1 pay source." : `${count} pay sources.`;
}

/**
 * Names whether contribution shares limit pay or keep full recorded pay.
 */
function shareLine(contributions: PlanSetupContribution[]) {
  if (contributions.length === 0) {
    return "No contribution shares are recorded.";
  }

  const limited = contributions.filter(
    (contribution) => contribution.monthlyAmount !== null,
  ).length;
  const people = contributions.length === 1 ? "person" : "people";
  if (limited === 0) {
    return `${contributions.length} ${people} share full recorded pay.`;
  }

  return `${limited} of ${contributions.length} ${people} limit how much pay is shared.`;
}

/**
 * A count line without a trailing period, so callers can add one.
 */
function countLine(count: number, singular: string, plural: string) {
  return `${count} ${count === 1 ? singular : plural}`;
}

/**
 * One saved three-way split. The percents are the stored values.
 */
function splitLine(label: string, rule: AllocationRuleDto) {
  return `${label}: ${rule.debtPercent}% debt, ${rule.savingsPercent}% savings, ${rule.cashPercent}% stays available.`;
}
