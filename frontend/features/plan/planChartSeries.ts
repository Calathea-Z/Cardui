import type {
  PlanCashForecastDto,
  PlanDebtOutcomeDto,
  PlanRecoveryPathDto,
} from "@/lib/api/types";

/**
 * Number of jewel series tokens in `globals.css`.
 */
const SERIES_COLOR_COUNT = 8;

/**
 * One debt drawn as a band on the balance chart.
 * `key` is the row field that holds this debt's balance.
 */
export type PlanBand = {
  debtId: string;
  name: string;
  key: string;
  color: string;
};

/**
 * One month on the balance chart.
 * `timestamp` is the first of the month in UTC. Each band's `key` holds that debt's balance after its payment that month.
 */
export type PlanBalanceRow = {
  timestamp: number;
  total: number;
} & Record<string, number>;

/**
 * One level on the minimums and breathing room chart.
 * A row starts a step that lasts until the next row.
 */
export type PlanObligationRow = {
  timestamp: number;
  minimums: number;
  room: number;
};

/**
 * One debt in the payoff order.
 * `paidOffOn` is the payment that clears it. `minimum` is the monthly payment it frees.
 */
export type PlanPayoffRow = {
  debtId: string;
  name: string;
  paidOffOn: string;
  minimum: number;
  color: string;
};

/**
 * One debt's slice of the total owed today.
 * `percent` is a whole number. The slices add to 100.
 */
export type PlanOwedShare = {
  debtId: string;
  name: string;
  balance: number;
  percent: number;
  color: string;
};

/**
 * One day on the 30-day cash chart.
 * `timestamp` is that day at midnight UTC. `cash` is the ending cash, negative when short.
 * `income`, `bills`, `debtPayments`, and `livingSpending` are that day's totals.
 */
export type PlanCashRow = {
  timestamp: number;
  cash: number;
  income: number;
  bills: number;
  debtPayments: number;
  livingSpending: number;
};

/**
 * Chart color for a debt's position in the rollover order.
 * A ninth debt reuses the first color. Its name still tells it apart.
 */
export function seriesColor(index: number) {
  return `var(--series-${(index % SERIES_COLOR_COUNT) + 1})`;
}

/**
 * Maps each debt to its chart color from the rollover order.
 * Build it from the rollover path so a debt keeps its color when the switch changes.
 */
export function debtColors(
  rolloverDebts: PlanDebtOutcomeDto[],
): Record<string, string> {
  return Object.fromEntries(
    rolloverDebts.map((debt, index) => [debt.debtId, seriesColor(index)]),
  );
}

/**
 * Each debt's share of what is owed today, largest first.
 * Percentages are whole numbers that add to 100: each is rounded down, and the leftover points go to the largest remainders.
 * A debt with no balance is left out. Empty when nothing is owed.
 */
export function owedShares(
  debts: PlanDebtOutcomeDto[],
  colors: Record<string, string>,
): PlanOwedShare[] {
  const owing = debts.filter((debt) => debt.balance > 0);
  const total = owing.reduce((sum, debt) => sum + debt.balance, 0);
  if (total <= 0) {
    return [];
  }

  const shares = owing.map((debt, index) => {
    const exact = (debt.balance / total) * 100;
    return {
      debtId: debt.debtId,
      name: debt.name,
      balance: debt.balance,
      percent: Math.floor(exact),
      remainder: exact - Math.floor(exact),
      color: colors[debt.debtId] ?? seriesColor(index),
    };
  });
  let leftover = 100 - shares.reduce((sum, share) => sum + share.percent, 0);
  for (const share of [...shares].sort((a, b) => b.remainder - a.remainder)) {
    if (leftover <= 0) {
      break;
    }

    share.percent += 1;
    leftover -= 1;
  }

  return shares
    .sort((a, b) => b.balance - a.balance)
    .map((share) => ({
      debtId: share.debtId,
      name: share.name,
      balance: share.balance,
      percent: share.percent,
      color: share.color,
    }));
}

/**
 * Debts that pay off on this path, in the order their minimums leave.
 * A debt that does not pay off is not drawn. Finish your plan lists it.
 */
export function payoffOrder(
  path: PlanRecoveryPathDto,
  colors: Record<string, string>,
): PlanPayoffRow[] {
  return path.steps.map((step, index) => ({
    debtId: step.debtId,
    name: step.name,
    paidOffOn: step.endedOn,
    minimum: step.minimum,
    color: colors[step.debtId] ?? seriesColor(index),
  }));
}

/**
 * Builds the stacked balance chart: one band per debt that pays off, one row per month.
 * Bands are in payoff order, first payoff first. The first row is the month before the first payment, at each opening balance.
 * Each later month carries a debt's last balance forward until its next payment, and a paid-off debt stays at zero.
 */
export function balanceChart(
  path: PlanRecoveryPathDto,
  colors: Record<string, string>,
): { bands: PlanBand[]; rows: PlanBalanceRow[] } {
  const bands = payoffOrder(path, colors).map((row, index) => ({
    debtId: row.debtId,
    name: row.name,
    key: `band${index}`,
    color: row.color,
  }));
  const bandIds = new Set(bands.map((band) => band.debtId));
  const points = path.balancePoints.filter((point) =>
    bandIds.has(point.debtId),
  );
  if (bands.length === 0 || points.length === 0) {
    return { bands: [], rows: [] };
  }

  const months = points.map((point) => monthIndex(point.dueDate));
  const first = Math.min(...months) - 1;
  const last = Math.max(...months);
  const current = new Map(
    bands.map((band) => [band.debtId, openingBalance(path, band.debtId)]),
  );
  const rows: PlanBalanceRow[] = [];
  let next = 0;
  for (let month = first; month <= last; month++) {
    while (next < points.length && months[next] <= month) {
      current.set(points[next].debtId, points[next].balance);
      next++;
    }

    const row: PlanBalanceRow = { timestamp: monthTimestamp(month), total: 0 };
    let total = 0;
    for (const band of bands) {
      const balance = current.get(band.debtId) ?? 0;
      row[band.key] = balance;
      total += balance;
    }

    row.total = roundCents(total);
    rows.push(row);
  }

  return { bands, rows };
}

/**
 * Builds the minimums and breathing room step chart.
 * It starts the month before the first payment at today's known minimums and no room. Each removal date lowers the minimums
 * by that debt's minimum and sets the room to the breathing room after that step. A last row a month later keeps the final level visible.
 * Empty when every minimum is unknown or no minimum leaves inside the projection.
 */
export function obligationChart(
  path: PlanRecoveryPathDto,
): PlanObligationRow[] {
  const dated = path.steps.filter((step) => step.startsOn !== null);
  if (
    path.startingObligation === null ||
    dated.length === 0 ||
    path.balancePoints.length === 0
  ) {
    return [];
  }

  const start =
    Math.min(...path.balancePoints.map((point) => monthIndex(point.dueDate))) -
    1;
  const rows: Array<PlanObligationRow & { month: number }> = [
    {
      month: start,
      timestamp: monthTimestamp(start),
      minimums: path.startingObligation,
      room: 0,
    },
  ];
  let minimums = path.startingObligation;
  for (const step of dated) {
    const month = monthIndex(step.startsOn!);
    minimums = roundCents(minimums - step.minimum);
    const row = {
      month,
      timestamp: monthTimestamp(month),
      minimums: Math.max(minimums, 0),
      room: step.breathingRoom,
    };
    if (rows[rows.length - 1].month === month) {
      rows[rows.length - 1] = row;
    } else {
      rows.push(row);
    }
  }

  const final = rows[rows.length - 1];
  rows.push({
    ...final,
    month: final.month + 1,
    timestamp: monthTimestamp(final.month + 1),
  });
  return rows.map(({ timestamp, minimums: amount, room }) => ({
    timestamp,
    minimums: amount,
    room,
  }));
}

/**
 * Builds the 30-day cash chart: one row per day, and the row for the lowest day in that window.
 * The lowest day is the first day that reaches the window's lowest cash. It is null when there are no days.
 */
export function cashChart(forecast: PlanCashForecastDto): {
  rows: PlanCashRow[];
  lowest: PlanCashRow | null;
} {
  const rows = forecast.days.map((day) => ({
    timestamp: dayTimestamp(day.date),
    cash: day.cash,
    income: day.income,
    bills: day.bills,
    debtPayments: day.debtPayments,
    livingSpending: day.livingSpending ?? 0,
  }));
  if (rows.length === 0) {
    return { rows, lowest: null };
  }

  const lowestOn = dayTimestamp(forecast.dayView.lowestCashOn);
  const lowest =
    rows.find((row) => row.timestamp === lowestOn) ??
    rows.reduce((low, row) => (row.cash < low.cash ? row : low));
  return { rows, lowest };
}

/**
 * Picks up to `maxTicks` evenly spaced ticks from the rows, always including the first and the last.
 */
export function evenTicks(rows: { timestamp: number }[], maxTicks = 6) {
  if (rows.length <= maxTicks) {
    return rows.map((row) => row.timestamp);
  }

  const count = Math.max(maxTicks, 2);
  const ticks = new Set<number>();
  for (let index = 0; index < count; index++) {
    const at = Math.round((index * (rows.length - 1)) / (count - 1));
    ticks.add(rows[at].timestamp);
  }

  return [...ticks];
}

/**
 * Short month label for a chart axis, such as "Jan 27".
 * The timestamp is read in UTC, the same way the rows are built.
 */
export function formatMonthTick(timestamp: number) {
  return new Intl.DateTimeFormat("en-US", {
    month: "short",
    year: "2-digit",
    timeZone: "UTC",
  }).format(new Date(timestamp));
}

/**
 * Month and year for a chart tooltip, such as "Jan 2027".
 */
export function formatMonthLabel(timestamp: number) {
  return new Intl.DateTimeFormat("en-US", {
    month: "short",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(timestamp));
}

/**
 * Short day label for a chart axis, such as "Oct 7".
 * The timestamp is read in UTC, the same way the rows are built.
 */
export function formatDayTick(timestamp: number) {
  return new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "numeric",
    timeZone: "UTC",
  }).format(new Date(timestamp));
}

/**
 * Weekday and date for a chart tooltip, such as "Wed, Oct 7".
 */
export function formatDayLabel(timestamp: number) {
  return new Intl.DateTimeFormat("en-US", {
    weekday: "short",
    month: "short",
    day: "numeric",
    timeZone: "UTC",
  }).format(new Date(timestamp));
}

/**
 * Midnight UTC for a `YYYY-MM-DD` date, so a calendar day does not shift with the browser's time zone.
 */
function dayTimestamp(date: string) {
  return Date.UTC(
    Number(date.slice(0, 4)),
    Number(date.slice(5, 7)) - 1,
    Number(date.slice(8, 10)),
  );
}

/**
 * Months since year zero for a `YYYY-MM-DD` date, so months compare as whole numbers.
 */
function monthIndex(date: string) {
  const year = Number(date.slice(0, 4));
  const month = Number(date.slice(5, 7));
  return year * 12 + (month - 1);
}

/**
 * First of the month in UTC milliseconds for a month index.
 */
function monthTimestamp(month: number) {
  return Date.UTC(Math.floor(month / 12), month % 12, 1);
}

/**
 * A debt's balance before any payment. Zero when the debt is not on this path.
 */
function openingBalance(path: PlanRecoveryPathDto, debtId: string) {
  return path.debts.find((debt) => debt.debtId === debtId)?.balance ?? 0;
}

/**
 * Rounds to cents so a sum of balances does not show floating-point noise.
 */
function roundCents(amount: number) {
  return Math.round(amount * 100) / 100;
}
