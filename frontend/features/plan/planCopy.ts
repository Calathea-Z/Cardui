import type {
  PayoffRolloverKind,
  PlanDebtOutcomeDto,
  PlanRecoveryDto,
  PlanRecoveryPathDto,
} from "@/lib/api/types";

/**
 * Formats a money amount in the planning currency.
 */
type FormatMoney = (amount: number) => string;

/**
 * Formats a `YYYY-MM-DD` date for on-screen text.
 */
type FormatDate = (date: string) => string;

/**
 * The answer at the top of Plan.
 * `figure` is the one big number. `details` are the quieter figures beside it.
 * `warning` is true while the path cannot project a payoff for every debt.
 */
export type PlanSummaryCopy = {
  warning: boolean;
  sentence: string;
  figureLabel: string;
  figure: string;
  details: { label: string; value: string }[];
};

/**
 * One row under Finish your plan: the debt, or a currency, what blocks the projection, and the action that fixes it on Debts.
 * `isDebt` is false for a currency row, which is not counted as a debt needing attention.
 */
export type PlanFinishItem = {
  key: string;
  name: string;
  fix: string;
  action: string;
  isDebt: boolean;
};

/**
 * One rule under How this is calculated: a short term and one plain sentence.
 */
export type PlanAssumption = {
  term: string;
  detail: string;
};

/**
 * The rules behind the projection, one short line each, for the selected path.
 * Only the freed-payment rule changes with the switch. The currency rule names the planning currency.
 */
export function planAssumptions(
  kind: PayoffRolloverKind,
  planningCurrency: string,
): PlanAssumption[] {
  return [
    {
      term: "Order",
      detail: "Highest interest rate first. No extra payment yet.",
    },
    {
      term: "Payments",
      detail: "Each debt pays its minimum on its monthly due date.",
    },
    {
      term: "Interest",
      detail:
        "One month of interest at the debt's rate, or its promo rate until that ends.",
    },
    {
      term: "Payoff month",
      detail:
        "The month a debt is paid off still pays it. Its payment is free the month after.",
    },
    kind === "Rollover"
      ? {
          term: "Rollover",
          detail:
            "A freed payment goes to the next debt. It becomes breathing room once no debt can take it.",
        }
      : {
          term: "Keep freed payments",
          detail: "A freed payment becomes breathing room right away.",
        },
    {
      term: "Left out",
      detail:
        "A debt missing a balance, rate, minimum, or due date, or one whose payment doesn't cover its interest. Finish your plan lists them.",
    },
    {
      term: "Currency",
      detail: `Only ${planningCurrency} debts are counted.`,
    },
    {
      term: "Limit",
      detail: "Projections stop at 50 years.",
    },
    {
      term: "Saved",
      detail:
        "Nothing is saved. The same debts always give the same dates and cents.",
    },
  ];
}

/**
 * The two paths the title-row switch offers, in order.
 */
export const planPathOptions: { value: PayoffRolloverKind; label: string }[] = [
  { value: "Rollover", label: "Rollover" },
  { value: "ReclaimAll", label: "Keep freed payments" },
];

/**
 * One line under the title.
 * With a payoff it says what the selected path does. Without one the switch is hidden, so it says when the payoff charts appear.
 * The order and the missing extra are stated because neither is stored yet.
 */
export function planDescription(kind: PayoffRolloverKind, hasPayoff: boolean) {
  if (!hasPayoff) {
    return "Debts are paid highest interest first, with no extra payment. Payoff charts appear once a debt can be paid off.";
  }

  const lead =
    kind === "Rollover"
      ? "When a debt is paid off, its payment moves to the next debt."
      : "When a debt is paid off, its payment comes back to you.";
  return `${lead} Highest interest first, with no extra payment.`;
}

/**
 * The one-sentence answer and the big figure for a path.
 * With every debt paid off, the big figure is the debt-free date at minimums only, because no extra payment is stored yet,
 * and the breathing room is labeled as what comes back after payoff, not money available today.
 * With some paid off, it counts them and dates the breathing room from the last payoff.
 * With none, it says how many debts need attention, and the figure is the total owed today with the known minimums beside it.
 */
export function planSummary(
  path: PlanRecoveryPathDto,
  attentionCount: number,
  money: FormatMoney,
  date: FormatDate,
): PlanSummaryCopy {
  if (path.steps.length === 0) {
    return waitingSummary(path, attentionCount, money);
  }

  if (path.paidOffOn) {
    return {
      warning: false,
      sentence:
        "You're on track to be debt-free paying only your minimums. Anything extra brings that day closer.",
      figureLabel: "Debt-free at minimums only",
      figure: date(path.paidOffOn),
      details: [
        {
          label: "Back to you after payoff",
          value: `${money(path.recurringRoom)} a month`,
        },
        { label: "Total interest", value: money(path.totalInterest) },
      ],
    };
  }

  const lastPayoff = path.steps[path.steps.length - 1].endedOn;
  const debtWord = path.debts.length === 1 ? "debt" : "debts";
  return {
    warning: true,
    sentence: joinSentences(
      `${path.steps.length} of ${path.debts.length} ${debtWord} paid off by ${date(lastPayoff)}.`,
      minimumsSentence(
        path.startingObligation,
        path.remainingObligation,
        money,
      ),
    ),
    figureLabel: `Back to you each month after ${date(lastPayoff)}`,
    figure: money(path.recurringRoom),
    details: [{ label: "Debt-free", value: "Not projected yet" }],
  };
}

/**
 * Everything that keeps this path from projecting a payoff, one row each.
 * Debts on the path come first in the rollover order, then debts with no balance, then each currency left out.
 */
export function finishPlanItems(
  report: PlanRecoveryDto,
  path: PlanRecoveryPathDto,
  money: FormatMoney,
): PlanFinishItem[] {
  const blocked = path.debts
    .filter((debt) => debt.stop !== "PaidOff")
    .map((debt) => ({
      key: debt.debtId,
      name: debt.name,
      ...blockedFix(debt, money),
      isDebt: true,
    }));
  const missing = report.missingBalance.map((debt) => ({
    key: debt.debtId,
    name: debt.name,
    fix: "Missing a balance, so this debt is left out.",
    action: "Add balance",
    isDebt: true,
  }));
  const currencies = report.excludedCurrencies.map((code) => ({
    key: `currency-${code}`,
    name: `${code} debts`,
    fix: `Left out. This plan uses ${report.planningCurrency}.`,
    action: "Review",
    isDebt: false,
  }));
  return [...blocked, ...missing, ...currencies];
}

/**
 * The summary while no debt pays off: how many debts need attention, the total owed today, and the known minimums.
 * A debt whose minimum cannot be used yet is counted as not included, rather than as zero.
 */
function waitingSummary(
  path: PlanRecoveryPathDto,
  attentionCount: number,
  money: FormatMoney,
): PlanSummaryCopy {
  const owed = path.debts.reduce((sum, debt) => sum + debt.balance, 0);
  const notCounted = path.debts.filter((debt) => debt.minimum === null).length;
  const minimums =
    path.startingObligation === null
      ? "Unknown"
      : `${money(path.startingObligation)} a month`;
  const sentence =
    attentionCount === 0
      ? "Your plan can't project a payoff yet."
      : `${attentionCount} ${attentionCount === 1 ? "debt needs" : "debts need"} attention before your plan can project a payoff.`;
  return {
    warning: true,
    sentence,
    figureLabel: "Owed today",
    figure: money(owed),
    details: [
      {
        label: "Monthly minimums",
        value:
          notCounted > 0 && path.startingObligation !== null
            ? `${minimums}, ${notCounted} not included`
            : minimums,
      },
    ],
  };
}

/**
 * The fix and the action for one debt that did not pay off on this path.
 * A payment that does not cover interest names that interest and the whole-dollar payment that starts paying the debt down.
 * A rate that runs out after the first payment is a promotion with no rate after it.
 */
function blockedFix(
  debt: PlanDebtOutcomeDto,
  money: FormatMoney,
): { fix: string; action: string } {
  switch (debt.stop) {
    case "DoesNotPayDown":
      return { fix: shortfallFix(debt, money), action: "Update payment" };
    case "RateUnknown":
      return {
        fix:
          debt.minimum === null
            ? "Missing an interest rate."
            : "Missing the interest rate after the promotion ends.",
        action: "Add rate",
      };
    case "MinimumUnknown":
      return { fix: "Missing a minimum payment.", action: "Add minimum" };
    case "DueDateUnknown":
      return { fix: "Missing a due date.", action: "Add due date" };
    case "HorizonReached":
      return {
        fix: "At this payment, the payoff falls past the 50-year limit.",
        action: "Update payment",
      };
    case "PaidOff":
      return { fix: "", action: "" };
  }
}

/**
 * Says how far a payment falls short of the interest, and the smallest whole-dollar payment above that interest.
 * Without a modeled month, it falls back to the stored minimum.
 */
function shortfallFix(debt: PlanDebtOutcomeDto, money: FormatMoney) {
  const interest = debt.lastMonthInterest;
  const payment = debt.lastMonthPayment ?? debt.minimum;
  if (typeof interest !== "number" || typeof payment !== "number") {
    return debt.minimum === null
      ? "The payment doesn't pay this down."
      : `${money(debt.minimum)} a month doesn't pay this down.`;
  }

  const startsPayingDown = Math.floor(interest) + 1;
  const comparison = interest > payment ? "more than" : "the same as";
  return `Interest is about ${money(interest)} a month, ${comparison} your ${money(payment)} payment. Paying ${money(startsPayingDown)} or more starts paying it down.`;
}

/**
 * How far the monthly minimums drop. Null when today's minimums are unknown, so nothing is said.
 * Unknown remaining minimums are named as unknown.
 */
function minimumsSentence(
  starting: number | null,
  remaining: number | null,
  money: FormatMoney,
) {
  if (starting === null) {
    return null;
  }

  const to = remaining === null ? "an unknown amount" : money(remaining);
  return `Minimums drop from ${money(starting)} to ${to}.`;
}

/**
 * Joins the sentences that are present with a space.
 */
function joinSentences(...sentences: (string | null)[]) {
  return sentences.filter(Boolean).join(" ");
}
