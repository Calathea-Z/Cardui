import type {
  PayoffRolloverKind,
  PlanCashForecastDto,
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
 * The cash outlook's answer: one sentence about the next 18 months.
 * `warning` is true when cash goes below zero in that time.
 */
export type PlanCashSummaryCopy = {
  warning: boolean;
  sentence: string;
};

/**
 * One horizon card in the cash outlook.
 * `warning` is true when cash goes below zero at any point up to that horizon.
 */
export type PlanCashHorizonCopy = {
  key: string;
  title: string;
  through: string;
  ending: string;
  lowest: string;
  minimums: string;
  warning: boolean;
};

/**
 * One note under the cash outlook. `warning` marks something to finish, such as a debt whose payments are left out.
 */
export type PlanCashNote = {
  key: string;
  text: string;
  warning: boolean;
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
 * The order and saved rules name a tried extra when one is set. Zero extra is minimums only.
 * Only the freed-payment rule changes with the switch. The currency rule names the planning currency.
 */
export function planAssumptions(
  kind: PayoffRolloverKind,
  planningCurrency: string,
  monthlyExtra = 0,
  money: FormatMoney = String,
): PlanAssumption[] {
  return [
    {
      term: "Order",
      detail:
        monthlyExtra > 0
          ? `Highest interest rate first. ${money(monthlyExtra)} extra each month goes to the first debt that can take it.`
          : "Highest interest rate first. No extra payment yet.",
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
    {
      term: "Due dates",
      detail:
        "A due date that has passed moves ahead a month at a time to the first one on or after today.",
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
      term: "Cash outlook",
      detail:
        "Starts from Cash on Accounts, adds income at typical pay, and takes out bills and this path's debt payments.",
    },
    {
      term: "Low pay",
      detail:
        "Uses each income source's low amount where one is recorded, and leaves out expected raises.",
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
        monthlyExtra > 0
          ? "Nothing is saved. This extra amount stays on the page until you leave."
          : "Nothing is saved. The same debts always give the same dates and cents.",
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
 * A tried extra is named in the same line. Zero extra says there is none, because the amount is not stored.
 */
export function planDescription(
  kind: PayoffRolloverKind,
  hasPayoff: boolean,
  monthlyExtra = 0,
  money: FormatMoney = String,
) {
  const extra =
    monthlyExtra > 0
      ? `with ${money(monthlyExtra)} extra each month`
      : "with no extra payment";
  if (!hasPayoff) {
    return `Debts are paid highest interest first, ${extra}. Payoff charts appear once a debt can be paid off.`;
  }

  const lead =
    kind === "Rollover"
      ? "When a debt is paid off, its payment moves to the next debt."
      : "When a debt is paid off, its payment comes back to you.";
  return `${lead} Highest interest first, ${extra}.`;
}

/**
 * The one-sentence answer and the big figure for a path.
 * With every debt paid off and no extra, the big figure is the debt-free date at minimums only.
 * With an extra, that same figure is the earlier date and the sentence names the amount.
 * Breathing room is labeled as what comes back after payoff, not money available today.
 * With some paid off, it counts them and dates the breathing room from the last payoff.
 * With none, it says how many debts need attention, and the figure is the total owed today with the known minimums beside it.
 */
export function planSummary(
  path: PlanRecoveryPathDto,
  attentionCount: number,
  money: FormatMoney,
  date: FormatDate,
  monthlyExtra = 0,
): PlanSummaryCopy {
  if (path.steps.length === 0) {
    return waitingSummary(path, attentionCount, money);
  }

  if (path.paidOffOn) {
    return {
      warning: false,
      sentence:
        monthlyExtra > 0
          ? `You're on track to be debt-free with ${money(monthlyExtra)} extra each month.`
          : "You're on track to be debt-free paying only your minimums. Anything extra brings that day closer.",
      figureLabel:
        monthlyExtra > 0 ? "Debt-free" : "Debt-free at minimums only",
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
  const paidOff = `${path.steps.length} of ${path.debts.length} ${debtWord} paid off by ${date(lastPayoff)}`;
  return {
    warning: true,
    sentence: joinSentences(
      monthlyExtra > 0
        ? `${paidOff}, with ${money(monthlyExtra)} extra each month.`
        : `${paidOff}.`,
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
 * The line under the Cash outlook title: where cash starts and, when the switch is shown, what happens to a freed payment.
 * A tried extra is included in the debt payments, so the line names that amount.
 */
export function cashOutlookDescription(
  kind: PayoffRolloverKind,
  hasPayoff: boolean,
  startingCash: number,
  money: FormatMoney,
  monthlyExtra = 0,
) {
  const start = `Starts from ${money(startingCash)} in Cash on Accounts today.`;
  const path = !hasPayoff
    ? start
    : kind === "Rollover"
      ? `${start} A paid-off debt's payment moves to the next debt.`
      : `${start} A paid-off debt's payment comes back as cash.`;
  return monthlyExtra > 0
    ? `${path} ${money(monthlyExtra)} extra each month is included in the debt payments.`
    : path;
}

/**
 * The line under the extra field.
 * Blank is the minimums-only plan. The amount is kept in the page until the user leaves.
 */
export function planExtraHelp() {
  return "Blank is minimums only. Tried on this page. Leaving clears it.";
}

/**
 * The status under the extra field while a tried amount is loading or has failed.
 * Ready has no status line. The previous plan stays on screen either way.
 */
export function planExtraStatus(status: "updating" | "error") {
  return status === "updating"
    ? "Updating the plan."
    : "This extra amount could not be applied. Leave the field to try it again.";
}

/**
 * The message when the extra field is not a zero or positive amount.
 */
export function planExtraInvalid() {
  return "Enter a zero or positive amount.";
}

/**
 * The cash outlook's one sentence for the longest horizon, usually 18 months.
 * A shortfall names the first short day, then the day cash is back above zero or that it stays short, then the lowest point.
 * Without a shortfall it says cash stays above zero and names the lowest point.
 */
export function cashOutlookSummary(
  forecast: PlanCashForecastDto,
  money: FormatMoney,
  date: FormatDate,
): PlanCashSummaryCopy {
  const window =
    forecast.horizons[forecast.horizons.length - 1]?.window ?? forecast.dayView;
  const lowest = `Lowest point: ${money(window.lowestCash)} on ${date(window.lowestCashOn)}.`;
  if (forecast.shortfallOn === null) {
    return {
      warning: false,
      sentence: `Cash stays above zero through ${date(window.through)}. ${lowest}`,
    };
  }

  const after = forecast.recoveredOn
    ? `and is back above zero on ${date(forecast.recoveredOn)}.`
    : `and stays short through ${date(window.through)}.`;
  return {
    warning: true,
    sentence: `Cash runs short on ${date(forecast.shortfallOn)} ${after} ${lowest}`,
  };
}

/**
 * The spoken label for the 30-day cash chart: the dates it covers, where cash ends, and its lowest day.
 */
export function cashChartLabel(
  forecast: PlanCashForecastDto,
  money: FormatMoney,
  date: FormatDate,
) {
  const view = forecast.dayView;
  return `Cash at the end of each day from ${date(view.from)} to ${date(view.through)}, ending at ${money(view.endingCash)}. Lowest is ${money(view.lowestCash)} on ${date(view.lowestCashOn)}.`;
}

/**
 * One card per horizon: ending cash, the lowest point, and the monthly minimums still due.
 * Minimums read None when nothing is still due, Unknown when every remaining minimum is missing a term,
 * and name how many debts they leave out otherwise.
 */
export function cashHorizonCards(
  forecast: PlanCashForecastDto,
  money: FormatMoney,
  date: FormatDate,
): PlanCashHorizonCopy[] {
  return forecast.horizons.map((horizon) => ({
    key: `${horizon.months}`,
    title: `${horizon.months} months`,
    through: `Through ${date(horizon.window.through)}`,
    ending: money(horizon.window.endingCash),
    lowest: `${money(horizon.window.lowestCash)} on ${date(horizon.window.lowestCashOn)}`,
    minimums: minimumsStillDue(
      horizon.minimumObligation,
      horizon.unknownMinimumCount,
      money,
    ),
    warning: horizon.window.cashShortfall,
  }));
}

/**
 * Notes under the cash outlook, warnings first.
 * Debts that stop before paying off, other than at the 50-year limit, and debts with no balance have payments left out, so cash may be lower.
 * A household with no bills and amounts in another currency each get a quieter note.
 */
export function cashOutlookNotes(
  report: PlanRecoveryDto,
  path: PlanRecoveryPathDto,
): PlanCashNote[] {
  const outlook = report.cashOutlook;
  const leftOut = [
    ...path.debts
      .filter(
        (debt) => debt.stop !== "PaidOff" && debt.stop !== "HorizonReached",
      )
      .map((debt) => debt.name),
    ...report.missingBalance.map((debt) => debt.name),
  ];
  const notes: PlanCashNote[] = [];
  if (leftOut.length > 0) {
    notes.push({
      key: "left-out",
      text: `Payments the plan can't project yet are left out for ${listNames(leftOut)}, so cash may be lower than shown. Finish your plan to include them.`,
      warning: true,
    });
  }

  if (!outlook.hasBills) {
    notes.push({
      key: "no-bills",
      text: "No bills yet, so only debt payments come out of cash. Add them on Bills.",
      warning: false,
    });
  }

  if (outlook.excludedCurrencies.length > 0) {
    notes.push({
      key: "currency",
      text: `Amounts in ${listNames(outlook.excludedCurrencies)} are left out. The outlook uses ${report.planningCurrency}.`,
      warning: false,
    });
  }

  return notes;
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
 * The monthly minimums still due at a horizon.
 * None when nothing remains, Unknown when every remaining minimum is missing a term, and the count left out of a known sum.
 */
function minimumsStillDue(
  obligation: number | null,
  unknownCount: number,
  money: FormatMoney,
) {
  if (obligation === null) {
    return "Unknown";
  }

  if (unknownCount > 0) {
    return `${money(obligation)} a month, ${unknownCount} not included`;
  }

  return obligation === 0 ? "None" : `${money(obligation)} a month`;
}

/**
 * Joins names as "A", "A and B", or "A, B, and C".
 */
function listNames(names: string[]) {
  if (names.length <= 2) {
    return names.join(" and ");
  }

  return `${names.slice(0, -1).join(", ")}, and ${names[names.length - 1]}`;
}

/**
 * Joins the sentences that are present with a space.
 */
function joinSentences(...sentences: (string | null)[]) {
  return sentences.filter(Boolean).join(" ");
}
