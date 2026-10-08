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
 * `warning` is true while cash is unaffordable or the path cannot project every payoff.
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
          term: "Roll payments forward",
          detail:
            "A freed payment goes to the next debt. It becomes breathing room once no debt can take it.",
        }
      : {
          term: "Free up cash",
          detail: "A freed payment becomes breathing room right away.",
        },
    {
      term: "Cash outlook",
      detail:
        "Starts with Cash on Accounts, adds shared pay, and takes out bills, living spending, and debt payments. Set-aside money stays in cash.",
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
  { value: "Rollover", label: "Roll payments forward" },
  { value: "ReclaimAll", label: "Free up cash" },
];

/**
 * Explains where a paid-off debt's payment goes in the selected temporary scenario.
 */
export function planPathDescription(kind: PayoffRolloverKind) {
  return kind === "Rollover"
    ? "A paid-off debt's payment moves to the next debt. It becomes available cash only when no modeled debt can take it."
    : "A paid-off debt's payment becomes available cash after that debt ends instead of moving to the next debt.";
}

/**
 * The one-sentence answer and the big figure for a path.
 * A dated cash or protected-reserve shortfall overrides payoff reassurance and makes any payoff date conditional.
 * With every debt paid off and no extra, the big figure is the debt-free date at minimums only.
 * With an extra, that same figure is the earlier date and the sentence names the amount.
 * Breathing room is labeled as what comes back after payoff, not money available today.
 * With some paid off, it counts them and dates the breathing room from the last payoff.
 * With none, it says how many debts need attention, and the figure is the total owed today with the known minimums beside it.
 */
export function planSummary(
  path: PlanRecoveryPathDto,
  cash: PlanCashForecastDto,
  attentionCount: number,
  money: FormatMoney,
  date: FormatDate,
  monthlyExtra = 0,
): PlanSummaryCopy {
  const cashShortfallOn = forecastShortfallOn(cash);
  const affordabilityShortfallOn = cashShortfallOn ?? cash.reserveShortfallOn;
  if (affordabilityShortfallOn) {
    const longest =
      cash.horizons[cash.horizons.length - 1]?.window ?? cash.dayView;
    return {
      warning: true,
      sentence: cashShortfallOn
        ? `This payoff path is not affordable yet. Cash runs short on ${date(cashShortfallOn)}.`
        : `This payoff path is not affordable yet. Money left after protected savings runs short on ${date(affordabilityShortfallOn)}.`,
      figureLabel: cashShortfallOn
        ? "First cash shortfall"
        : "Protected cash shortfall",
      figure: date(affordabilityShortfallOn),
      details: [
        {
          label: "Conditional debt-free estimate",
          value: path.paidOffOn ? date(path.paidOffOn) : "Not projected yet",
        },
        {
          label: "Lowest forecast cash",
          value: money(longest.lowestCash),
        },
      ],
    };
  }

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
 * Finds the first negative-cash date even when an older response omitted the summary field.
 */
function forecastShortfallOn(forecast: PlanCashForecastDto): string | null {
  if (forecast.shortfallOn) {
    return forecast.shortfallOn;
  }

  const day = forecast.days.find((item) => item.cash < 0);
  if (day) {
    return day.date;
  }

  return (
    forecast.horizons.find((horizon) => horizon.window.lowestCash < 0)?.window
      .lowestCashOn ?? null
  );
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
 * A reserve above zero names how much is set aside and what is left after it.
 */
export function cashOutlookDescription(
  kind: PayoffRolloverKind,
  hasPayoff: boolean,
  startingCash: number,
  money: FormatMoney,
  monthlyExtra = 0,
  startingReserve = 0,
  startingAvailable = 0,
) {
  const start = `Starts from ${money(startingCash)} in Cash on Accounts today.`;
  const path = !hasPayoff
    ? start
    : kind === "Rollover"
      ? `${start} A paid-off debt's payment moves to the next debt.`
      : `${start} A paid-off debt's payment comes back as cash.`;
  const sentence =
    monthlyExtra > 0
      ? `${path} ${money(monthlyExtra)} extra each month is included in the debt payments.`
      : path;
  return startingReserve > 0
    ? `${sentence} ${money(startingReserve)} is protected. ${money(startingAvailable)} is available after protected savings.`
    : sentence;
}

/**
 * The line under the extra field.
 * Blank is the minimums-only plan. The amount is kept in the page until the user leaves.
 */
export function planExtraHelp() {
  return "Enter 0 for minimums only. The preview updates when you leave the field or press Enter.";
}

/**
 * The status shown while a tried amount is loading or has failed.
 * It names the amount still represented by the visible results so an old forecast cannot look newly applied.
 */
export function planExtraStatus(
  status: "updating" | "error",
  requestedAmount: number,
  appliedAmount: number,
  money: FormatMoney,
) {
  return status === "updating"
    ? `Updating the preview for ${money(requestedAmount)} extra. Results still show ${money(appliedAmount)} extra until it finishes.`
    : `The ${money(requestedAmount)} extra preview could not be applied. Results still show ${money(appliedAmount)} extra. Leave the field or press Enter to try again.`;
}

/**
 * The message when the extra field is not a zero or positive amount.
 */
export function planExtraInvalid() {
  return "Enter a zero or positive amount.";
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
      text: "No bills yet. Add them on Bills. Monthly living spending and debt payments still come out of cash.",
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
