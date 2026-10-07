import type {
  CategoryTargetLineDto,
  CategoryTargetMonthDto,
} from "@/lib/api/types";

const monthNames = [
  "January",
  "February",
  "March",
  "April",
  "May",
  "June",
  "July",
  "August",
  "September",
  "October",
  "November",
  "December",
] as const;

/**
 * The rule under the targets summary.
 * Spent matches posted activity. Rollover is per category and stays off until it is turned on.
 */
export const targetSummaryNote =
  "Spent is posted spending in the planning currency. Income, transfers, and statement adjustments are left out. A transfer, including a card payment, is not spent again. Leftover money and overspend move ahead only when rollover is on.";

/**
 * One summary figure.
 * `known` is false when the month has no targets, so the figure stays quiet.
 */
export type TargetMetric = {
  label: string;
  value: string;
  known: boolean;
  detail?: string;
};

/**
 * A month name and year, such as October 2026.
 */
export function monthLabel(year: number, month: number) {
  return `${monthNames[month - 1]} ${year}`;
}

/**
 * The calendar month before the one given.
 */
export function previousMonth(year: number, month: number) {
  if (month === 1) {
    return { year: year - 1, month: 12 };
  }

  return { year, month: month - 1 };
}

/**
 * A stable value for the month choice list.
 */
export function monthKey(year: number, month: number) {
  return `${year}-${String(month).padStart(2, "0")}`;
}

/**
 * Reads a month choice value.
 * An unrecognized value stays unset.
 */
export function parseMonthKey(value: string) {
  const match = /^(\d{4})-(\d{2})$/.exec(value);
  if (!match) {
    return null;
  }

  const year = Number(match[1]);
  const month = Number(match[2]);
  if (month < 1 || month > 12) {
    return null;
  }

  return { year, month };
}

/**
 * Month choices from eleven months ago through next month.
 * The list is built from the household's today, not the browser clock.
 */
export function monthChoices(todayYear: number, todayMonth: number) {
  const choices = [];
  for (let offset = -11; offset <= 1; offset += 1) {
    const date = new Date(todayYear, todayMonth - 1 + offset, 1);
    const year = date.getFullYear();
    const month = date.getMonth() + 1;
    choices.push({
      value: monthKey(year, month),
      label: monthLabel(year, month),
    });
  }

  return choices;
}

/**
 * Target, spent, and remaining for the summary.
 * A month with no targets says so. Remaining can be an over amount.
 */
export function summaryMetrics(
  month: Pick<CategoryTargetMonthDto, "targetTotal" | "spent" | "remaining">,
  format: (amount: number) => string,
): TargetMetric[] {
  return [
    {
      label: "Target",
      value:
        month.targetTotal === null ? "No targets" : format(month.targetTotal),
      known: month.targetTotal !== null,
    },
    {
      label: "Spent",
      value: format(month.spent),
      known: true,
    },
    {
      label: "Remaining",
      value:
        month.remaining === null
          ? "No targets"
          : remainingText(month.remaining, format),
      known: month.remaining !== null,
    },
  ];
}

/**
 * Short rows under the summary for spending the targets do not cover.
 */
export function summarySignals(
  month: Pick<
    CategoryTargetMonthDto,
    | "otherSpent"
    | "missingTargetCount"
    | "unassignedRolloverCount"
    | "excludedTransactionCount"
    | "excludedCurrencies"
  >,
  format: (amount: number) => string,
) {
  const signals: string[] = [];
  if (month.otherSpent > 0) {
    signals.push(
      `${format(month.otherSpent)} of spending has no target, so it is not part of remaining.`,
    );
  }

  if (month.missingTargetCount === 1) {
    signals.push("1 category has no target.");
  } else if (month.missingTargetCount > 1) {
    signals.push(`${month.missingTargetCount} categories have no target.`);
  }

  if (month.unassignedRolloverCount === 1) {
    signals.push("1 category has an amount from last month and no target yet.");
  } else if (month.unassignedRolloverCount > 1) {
    signals.push(
      `${month.unassignedRolloverCount} categories have an amount from last month and no target yet.`,
    );
  }

  if (month.excludedTransactionCount > 0) {
    const currencies = month.excludedCurrencies.join(", ");
    signals.push(
      month.excludedTransactionCount === 1
        ? `1 transaction in ${currencies} is not included.`
        : `${month.excludedTransactionCount} transactions in ${currencies} are not included.`,
    );
  }

  return signals;
}

/**
 * Says whether spent is the month so far, the full month, or still zero.
 */
export function spentPeriodNote(
  month: Pick<
    CategoryTargetMonthDto,
    "throughToday" | "year" | "month" | "todayYear" | "todayMonth"
  >,
) {
  const future =
    month.year > month.todayYear ||
    (month.year === month.todayYear && month.month > month.todayMonth);
  if (future) {
    return "This month has not started. Spent stays at zero until then.";
  }

  if (month.throughToday) {
    return "Spent runs through today. Later dates in this month are not included yet.";
  }

  return "Spent is the full month.";
}

/**
 * The preview line for a month that has not been saved.
 * A copy that skips a month does not bring leftover money across the gap.
 */
export function copyForwardNote(
  month: Pick<
    CategoryTargetMonthDto,
    "saved" | "copiedFromYear" | "copiedFromMonth" | "year" | "month"
  >,
) {
  if (
    month.saved ||
    month.copiedFromYear === null ||
    month.copiedFromMonth === null
  ) {
    return null;
  }

  const from = monthLabel(month.copiedFromYear, month.copiedFromMonth);
  const current = monthLabel(month.year, month.month);
  const previous = previousMonth(month.year, month.month);
  const skipped =
    previous.year !== month.copiedFromYear ||
    previous.month !== month.copiedFromMonth;
  const base = `${current} has no saved targets. These amounts are from ${from}.`;
  if (!skipped) {
    return base;
  }

  return `${base} Money left over does not skip ${monthLabel(previous.year, previous.month)}.`;
}

/**
 * Remaining text for one category or the month.
 * A missing target stays "No target". An over amount is named, not only colored.
 */
export function remainingText(
  remaining: number | null,
  format: (amount: number) => string,
) {
  if (remaining === null) {
    return "No target";
  }

  if (remaining < 0) {
    return `${format(Math.abs(remaining))} over`;
  }

  return `${format(remaining)} left`;
}

/**
 * The rollover sentence under a category.
 * Incoming money and the choice to roll this month ahead are separate.
 */
export function rolloverDetail(
  rolloverIn: number,
  rollover: boolean,
  previousLabel: string,
  format: (amount: number) => string,
) {
  const parts: string[] = [];
  if (rolloverIn > 0) {
    parts.push(`Includes ${format(rolloverIn)} from ${previousLabel}`);
  } else if (rolloverIn < 0) {
    parts.push(
      `Starts ${format(Math.abs(rolloverIn))} over from ${previousLabel}`,
    );
  }

  if (rollover) {
    parts.push("Rolls into next month");
  }

  return parts.length === 0 ? null : `${parts.join(". ")}.`;
}

/**
 * Groups category rows by subgroup, keeping the order the API returned.
 */
export function groupTargetLines(lines: CategoryTargetLineDto[]) {
  const groups: { name: string; lines: CategoryTargetLineDto[] }[] = [];
  for (const line of lines) {
    const current = groups[groups.length - 1];
    if (!current || current.name !== line.subGroupName) {
      groups.push({ name: line.subGroupName, lines: [line] });
      continue;
    }

    current.lines.push(line);
  }

  return groups;
}
