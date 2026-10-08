import type {
  DebtBalanceComparisonDto,
  DebtCurrencySummaryDto,
  DebtSummaryGap,
  DebtSummaryItemDto,
  DebtSummaryReportDto,
} from "@/lib/api/types";

type MoneyText = (amount: number, currency: string) => string;

type DateText = (value: string) => string;

/**
 * One figure in the summary strip.
 * `known` is false when the figure is unknown, so the screen can quiet that number.
 * `detail` is the short caveat. A complete figure has none.
 * `hint` names the debts behind that caveat. It is null when the caveat has no debts to name.
 */
export type SummaryMetric = {
  label: string;
  value: string;
  detail: string | null;
  hint: string | null;
  known: boolean;
};

/**
 * One risk or missing-input row under the figures.
 * The label is the fact. The detail is the short context, when there is any.
 * `hint` names the debts behind a missing-term detail.
 */
export type SummarySignal = {
  label: string;
  detail: string | null;
  hint?: string | null;
  warning?: boolean;
};

/**
 * A debt name used to explain a summary gap.
 * `currency` keeps the note on the matching currency group.
 */
export type SummaryDebtName = {
  id: string;
  name: string;
  currency: string;
};

/**
 * The two balances shown when a linked account disagrees with the debt.
 * The recorded amount is the one in use. The account amount is not, until the person chooses it.
 */
export type BalanceComparisonCopy = {
  recordedAmount: string;
  recordedAsOf: string | null;
  recordedStatus: string;
  accountAmount: string;
  accountAsOf: string | null;
  accountStatus: string;
};

/**
 * The line under Summary.
 * The figures are the debts on this page. Interest is one month on each recorded balance, not the total left to pay.
 */
export const summaryInterestNote =
  "Active totals use positive balances only. Zero balances keep their saved terms for review. Interest is one month, not the total left to pay.";

/**
 * The four summary figures for one currency.
 * A null total is Unknown. A known zero stays an amount. The caveat is a short detail, not a paragraph.
 * A hint names the debts in this currency that were left out.
 */
export function summaryMetrics(
  group: DebtCurrencySummaryDto,
  report: DebtSummaryReportDto,
  sharePercent: (ratio: number) => string,
  money: MoneyText,
  debts: SummaryDebtName[] = [],
): SummaryMetric[] {
  const rows = rowsInCurrency(debts, report.debts, group.currency);
  return [
    metric(
      "Recorded",
      group.recordedBalance,
      group.currency,
      group.unknownBalanceCount,
      "unknown",
      "unknown",
      money,
      gapHint(rows, "Balance"),
    ),
    metric(
      "Interest this month",
      group.monthlyInterest,
      group.currency,
      group.unknownInterestCount,
      "missing input",
      "missing inputs",
      money,
      interestHint(rows),
    ),
    metric(
      "Minimums",
      group.minimumPayments,
      group.currency,
      group.unknownMinimumCount,
      "unknown",
      "unknown",
      money,
      gapHint(rows, "MinimumPayment"),
    ),
    {
      label: "Limits in use",
      value:
        group.utilization === null
          ? "Unknown"
          : sharePercent(group.utilization),
      detail: utilizationDetail(group, report, sharePercent),
      hint: gapHint(rows, "CreditLimit"),
      known: group.utilization !== null,
    },
  ];
}

/**
 * The risks and missing inputs the four figures do not already show.
 * A balance that differs from its account is not listed here. That choice stays on the debt.
 * A count of zero is left out. There is no score.
 * A missing-term row names the debts when those names are known.
 */
export function summarySignals(
  report: DebtSummaryReportDto,
  debts: SummaryDebtName[] = [],
): SummarySignal[] {
  const signals: SummarySignal[] = [];
  const paymentReview = sum(report, "zeroBalancePaymentReviewCount");
  if (paymentReview > 0) {
    signals.push({
      label:
        paymentReview === 1
          ? "1 zero-balance debt needs payment review"
          : `${paymentReview} zero-balance debts need payment review`,
      detail: "Saved minimum kept · excluded from active totals",
      hint: paymentReviewHint(namedRows(debts, report.debts)),
      warning: true,
    });
  }

  const stale = sum(report, "staleCount");
  if (stale > 0) {
    signals.push({
      label:
        stale === 1
          ? "1 balance is not current"
          : `${stale} balances are not current`,
      detail: "Still counted",
    });
  }

  const due = sum(report, "dueDatePassedCount");
  if (due > 0) {
    signals.push({
      label: due === 1 ? "Due date passed" : `${due} due dates passed`,
      detail: "Payment status unknown",
    });
  }

  const ending = sum(report, "promoEndingCount");
  if (ending > 0) {
    signals.push({
      label: ending === 1 ? "Promo ends soon" : `${ending} promos end soon`,
      detail: `Within ${report.promotionalNoticeDays} days`,
    });
  }

  const ended = sum(report, "promotionEndedCount");
  if (ended > 0) {
    signals.push({
      label: ended === 1 ? "Promotion ended" : `${ended} promotions ended`,
      detail: null,
    });
  }

  const apr = sum(report, "aprNoticeCount");
  if (apr > 0) {
    signals.push({
      label:
        apr === 1
          ? `Rate is ${percent(report.aprNoticePercent)} or higher`
          : `${apr} rates are ${percent(report.aprNoticePercent)} or higher`,
      detail: null,
    });
  }

  const missing = missingDetails(report);
  if (missing.length > 0) {
    signals.push({
      label: "Still missing",
      detail: missing.join(", "),
      hint: stillMissingHint(namedRows(debts, report.debts)),
    });
  }

  return signals;
}

/**
 * The interest line for one debt.
 * Null when the amount cannot be estimated, so the card does not repeat "unknown".
 */
export function debtInterestLine(
  monthlyInterest: number | null,
  rateIsPromotional: boolean,
  currency: string,
  money: MoneyText,
) {
  if (monthlyInterest === null) {
    return null;
  }

  const amount = money(monthlyInterest, currency);
  return rateIsPromotional
    ? `${amount} interest this month at the promo rate`
    : `${amount} interest this month`;
}

/**
 * Joins the known terms and names the ones still blank in one line.
 * Three blanks become one phrase. A known value is not called unknown.
 */
export function debtFactLine(known: string[], missingLabels: string[]) {
  const missing = missingFactPhrase(missingLabels);
  if (known.length === 0) {
    return missing;
  }

  if (!missing) {
    return known.join(" · ");
  }

  return `${known.join(" · ")} · ${missing}`;
}

/**
 * The extra notes for one debt that the terms line does not already show.
 * A passed due date does not claim the payment was missed.
 */
export function debtSummaryLines(
  item: {
    dueDatePassed: boolean;
    promoEndsWithinNotice: boolean;
    promotionEnded: boolean;
    promotionalEndsOn: string | null;
  },
  aprAfter: string | null,
  date: DateText,
) {
  const lines: string[] = [];
  if (item.dueDatePassed) {
    lines.push("Due date passed · payment status unknown");
  }

  if (item.promoEndsWithinNotice && item.promotionalEndsOn) {
    const after =
      aprAfter === null
        ? "APR after that is unknown"
        : `APR after that is ${aprAfter}`;
    lines.push(`Promo ends ${date(item.promotionalEndsOn)} · ${after}`);
  } else if (item.promotionEnded && item.promotionalEndsOn) {
    const after =
      aprAfter === null ? "APR after it is unknown" : `APR is ${aprAfter}`;
    lines.push(`Promo ended ${date(item.promotionalEndsOn)} · ${after}`);
  }

  return lines;
}

/**
 * Which utilization threshold a revolving debt has reached.
 * `notice` is the first callout. `high` is the limit nearly used up. It is not a grade.
 */
export const utilizationNoticeLevels = ["notice", "high"] as const;

export type UtilizationNoticeLevel = (typeof utilizationNoticeLevels)[number];

/**
 * The callout for one utilization threshold.
 * `text` names the threshold and that this share of the limit is in use.
 */
export type UtilizationNotice = {
  level: UtilizationNoticeLevel;
  text: string;
};

/**
 * The callout when a revolving debt has reached a named utilization threshold.
 * Notice is the first share. High is the share near the whole limit. Null when the share is under the first threshold.
 * It is not a grade.
 */
export function utilizationNotice(
  reachesNotice: boolean,
  reachesLimit: boolean,
  report: DebtSummaryReportDto,
): UtilizationNotice | null {
  if (reachesLimit) {
    return {
      level: "high",
      text: `${share(report.utilizationLimitNotice)} or more of the limit is in use`,
    };
  }

  if (reachesNotice) {
    return {
      level: "notice",
      text: `${share(report.utilizationNotice)} or more of the limit is in use`,
    };
  }

  return null;
}

/**
 * The title on a debt that has two balances.
 * It names the situation so the summary totals are not asked to explain it.
 */
export const twoBalancesTitle = "Two balances";

/**
 * Says which balance the totals use, and what choosing the account balance does.
 * When the account can be followed, choosing it starts that follow. Otherwise it copies the balance once.
 * When the account balance cannot be stored, the line does not offer that choice.
 */
export function twoBalancesNote(canChoose: boolean, startsFollow = false) {
  if (canChoose && startsFollow) {
    return "The totals above use the recorded balance. Choosing the account balance follows that account from now on.";
  }

  if (canChoose) {
    return "The totals above use the recorded balance. Choose the account balance if you want them to use that instead.";
  }

  return "The totals above use the recorded balance.";
}

/**
 * Labels both balances when they differ.
 * The recorded amount is marked in use. The account amount is marked not in use, or with the reason it cannot be chosen.
 */
export function balanceComparisonCopy(
  debtBalance: number | null,
  debtBalanceAsOf: string | null,
  debtCurrency: string,
  comparison: DebtBalanceComparisonDto,
  money: MoneyText,
  date: DateText,
): BalanceComparisonCopy {
  return {
    recordedAmount:
      debtBalance === null ? "Unknown" : money(debtBalance, debtCurrency),
    recordedAsOf: debtBalanceAsOf ? date(debtBalanceAsOf) : null,
    recordedStatus: "In use",
    accountAmount: money(
      comparison.accountBalance,
      comparison.accountCurrency ?? debtCurrency,
    ),
    accountAsOf: comparison.accountBalanceAsOf
      ? date(comparison.accountBalanceAsOf)
      : null,
    accountStatus: blockCopy(comparison.block) ?? "Not in use",
  };
}

/**
 * Builds one money figure.
 * A missing total is Unknown. A leftover count becomes the detail.
 */
function metric(
  label: string,
  amount: number | null,
  currency: string,
  unknownCount: number,
  singular: string,
  plural: string,
  money: MoneyText,
  hint: string | null,
): SummaryMetric {
  return {
    label,
    value: amount === null ? "Unknown" : money(amount, currency),
    detail: unknownDetail(unknownCount, singular, plural),
    hint,
    known: amount !== null,
  };
}

/**
 * The short caveat for a count of debts left out of a total.
 * Zero is omitted.
 */
function unknownDetail(countValue: number, singular: string, plural: string) {
  if (countValue === 0) {
    return null;
  }

  return `${countValue} ${countValue === 1 ? singular : plural}`;
}

/**
 * The utilization caveat.
 * A threshold is named when the ratio reaches it. Debts left out of the ratio are counted.
 */
function utilizationDetail(
  group: DebtCurrencySummaryDto,
  report: DebtSummaryReportDto,
  sharePercent: (ratio: number) => string,
) {
  const parts: string[] = [];
  if (
    group.utilization !== null &&
    group.utilization >= report.utilizationLimitNotice
  ) {
    parts.push(`${sharePercent(report.utilizationLimitNotice)} or more`);
  } else if (
    group.utilization !== null &&
    group.utilization >= report.utilizationNotice
  ) {
    parts.push(`${sharePercent(report.utilizationNotice)} or more`);
  }

  if (group.unknownUtilizationCount > 0) {
    parts.push(
      unknownDetail(
        group.unknownUtilizationCount,
        "missing a limit",
        "missing limits",
      ) ?? "",
    );
  }

  return parts.length > 0 ? parts.join(" · ") : null;
}

/**
 * The missing terms that are not already a detail on the four figures.
 */
function missingDetails(report: DebtSummaryReportDto) {
  const details: string[] = [];
  pushMissing(
    details,
    sum(report, "missingDueDateCount"),
    "due date",
    "due dates",
  );
  pushMissing(
    details,
    sum(report, "missingRemainingTermCount"),
    "term",
    "terms",
  );
  pushMissing(
    details,
    sum(report, "missingPromotionalEndCount"),
    "promo end",
    "promo ends",
  );
  pushMissing(
    details,
    sum(report, "missingPromotionalRateCount"),
    "promo rate",
    "promo rates",
  );
  pushMissing(
    details,
    sum(report, "missingRateAfterPromotionCount"),
    "rate after a promo",
    "rates after a promo",
  );
  return details;
}

/**
 * Adds one missing-term phrase when the count is above zero.
 */
const gapPhrase: Record<DebtSummaryGap, string> = {
  Balance: "a balance",
  Apr: "an APR",
  MinimumPayment: "a minimum",
  DueDate: "a due date",
  CreditLimit: "a credit limit",
  RemainingTerm: "the months left",
  PromotionalEnd: "a promo end date",
  PromotionalRate: "a promo rate",
  RateAfterPromotion: "an APR after the promo",
};

const stillMissingGaps = [
  "DueDate",
  "RemainingTerm",
  "PromotionalEnd",
  "PromotionalRate",
  "RateAfterPromotion",
] as const satisfies readonly DebtSummaryGap[];

type GapRow = {
  name: string;
  gaps: DebtSummaryGap[];
  monthlyInterest: number | null;
  needsPaymentReview: boolean;
};

/**
 * Names the debts in one currency that are missing one fact.
 * An empty match returns null so the caveat stays a count.
 */
function gapHint(rows: GapRow[], gap: DebtSummaryGap) {
  const names = rows
    .filter((row) => row.gaps.includes(gap))
    .map((row) => row.name);
  return names.length === 0 ? null : namesNeed(names, gapPhrase[gap]);
}

/**
 * Names why interest could not be estimated.
 * Debts missing the same facts share one sentence.
 */
function interestHint(rows: GapRow[]) {
  const groups = new Map<string, string[]>();
  for (const row of rows) {
    if (row.monthlyInterest !== null) {
      continue;
    }

    const parts = (["Balance", "Apr"] as const)
      .filter((gap) => row.gaps.includes(gap))
      .map((gap) => gapPhrase[gap]);
    const phrase = parts.length > 0 ? joinList(parts) : "an interest input";
    const names = groups.get(phrase) ?? [];
    names.push(row.name);
    groups.set(phrase, names);
  }

  if (groups.size === 0) {
    return null;
  }

  return [...groups.entries()]
    .map(([phrase, names]) => namesNeed(names, phrase))
    .join(" ");
}

/**
 * Names the debts behind the terms that are not already on the four figures.
 */
function stillMissingHint(rows: GapRow[]) {
  const sentences = stillMissingGaps
    .map((gap) => gapHint(rows, gap))
    .filter((sentence) => sentence !== null);
  return sentences.length > 0 ? sentences.join(" ") : null;
}

/**
 * Names zero-balance debts whose saved positive minimum needs review.
 */
function paymentReviewHint(rows: GapRow[]) {
  const names = rows
    .filter((row) => row.needsPaymentReview)
    .map((row) => row.name);
  if (names.length === 0) {
    return null;
  }

  return `${joinList(names)} ${names.length === 1 ? "has" : "have"} a saved minimum but no active balance.`;
}

/**
 * Joins debt names with the fact they still need.
 */
function namesNeed(names: string[], phrase: string) {
  const verb = names.length === 1 ? "needs" : "need";
  return `${joinList(names)} ${verb} ${phrase}.`;
}

/**
 * Joins a list with commas and a final "and".
 * One item stays as it is.
 */
function joinList(parts: string[]) {
  if (parts.length <= 1) {
    return parts[0] ?? "";
  }

  if (parts.length === 2) {
    return `${parts[0]} and ${parts[1]}`;
  }

  return `${parts.slice(0, -1).join(", ")}, and ${parts[parts.length - 1]}`;
}

/**
 * Pairs summary rows with debt names in one currency.
 * A debt from another currency stays out of this group's note.
 */
function rowsInCurrency(
  debts: SummaryDebtName[],
  items: DebtSummaryItemDto[],
  currency: string,
) {
  return namedRows(debts, items).filter((row) => row.currency === currency);
}

/**
 * Pairs every summary row with its debt name.
 * A summary row with no matching debt is left out.
 */
function namedRows(debts: SummaryDebtName[], items: DebtSummaryItemDto[]) {
  return items.flatMap((item) => {
    const debt = debts.find((entry) => entry.id === item.debtId);
    if (!debt) {
      return [];
    }

    return [
      {
        name: debt.name,
        currency: debt.currency,
        gaps: item.gaps,
        monthlyInterest: item.monthlyInterest,
        needsPaymentReview: item.needsPaymentReview,
      },
    ];
  });
}

function pushMissing(
  details: string[],
  countValue: number,
  singular: string,
  plural: string,
) {
  if (countValue === 0) {
    return;
  }

  details.push(`${countValue} ${countValue === 1 ? singular : plural}`);
}

/**
 * Adds one count across every currency group.
 * Money totals stay inside each currency. These counts do not.
 */
function sum(report: DebtSummaryReportDto, key: keyof DebtCurrencySummaryDto) {
  return report.currencies.reduce((total, group) => {
    const value = group[key];
    return total + (typeof value === "number" ? value : 0);
  }, 0);
}

/**
 * Turns blank term names into one phrase.
 * One name stays singular. The last name is joined with "and".
 */
function missingFactPhrase(labels: string[]) {
  if (labels.length === 0) {
    return null;
  }

  if (labels.length === 1) {
    return `${labels[0]} unknown`;
  }

  if (labels.length === 2) {
    return `${labels[0]} and ${labels[1]} unknown`;
  }

  return `${labels.slice(0, -1).join(", ")}, and ${labels[labels.length - 1]} unknown`;
}

/**
 * Formats a percent that is already in percent points. 20 becomes 20%.
 */
function percent(value: number) {
  return `${value}%`;
}

/**
 * Formats a utilization ratio as a percent. 0.30 becomes 30%.
 */
function share(ratio: number) {
  return `${Math.round(ratio * 1000) / 10}%`.replace(/\.0%$/, "%");
}

/**
 * The short reason an account balance is shown and cannot be chosen.
 * None returns null so the status can say the recorded balance is still in use.
 */
function blockCopy(block: DebtBalanceComparisonDto["block"]) {
  switch (block) {
    case "DateUnknown":
      return "No date on this balance";
    case "NegativeBalance":
      return "Below zero";
    case "CurrencyDiffers":
      return "Currency differs";
    case "AmountTooLarge":
      return "Too large to store";
    default:
      return null;
  }
}
