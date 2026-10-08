import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

/**
 * Loads one Plan module after erasing its types.
 * The modules have no runtime imports.
 */
async function loadModule(file) {
  const source = await readFile(
    path.resolve(`features/plan/${file}.ts`),
    "utf8",
  );
  const output = ts.transpileModule(source, {
    compilerOptions: {
      module: ts.ModuleKind.ES2022,
      target: ts.ScriptTarget.ES2022,
    },
  });
  const tempDir = path.resolve(".tmp-tests");
  await mkdir(tempDir, { recursive: true });
  await writeMoneyDigits(tempDir);
  const compiledPath = path.join(tempDir, `${file}.mjs`);
  await writeFile(compiledPath, linkMoneyDigits(output.outputText));
  return import(pathToFileURL(compiledPath).href);
}

/**
 * Writes the shared money reader next to a transpiled test module.
 * The test file cannot resolve the app's `@/` import on its own.
 */
async function writeMoneyDigits(tempDir) {
  const source = await readFile(
    path.resolve("features/accounts/formatCurrency.ts"),
    "utf8",
  );
  const output = ts.transpileModule(source, {
    compilerOptions: {
      module: ts.ModuleKind.ES2022,
      target: ts.ScriptTarget.ES2022,
    },
  });
  await writeFile(path.join(tempDir, "formatCurrency.mjs"), output.outputText);
}

/**
 * Points a transpiled module at the local money reader.
 */
function linkMoneyDigits(source) {
  return source.replaceAll(
    'from "@/features/accounts/formatCurrency"',
    'from "./formatCurrency.mjs"',
  );
}

const series = await loadModule("planChartSeries");
const copy = await loadModule("planCopy");
const readiness = await loadModule("planReadiness");
const extra = await loadModule("planExtra");
const cashRange = await loadModule("planCashRange");
const navigation = await loadModule("planNavigation");
const debtSummary = await loadModule("planDebtSummary");
const money = (amount) => `$${amount.toFixed(2)}`;
const date = (value) => value;
const utc = (year, month) => Date.UTC(year, month - 1, 1);

function debt(debtId, name, overrides = {}) {
  return {
    debtId,
    name,
    stop: "PaidOff",
    balance: 100,
    minimum: 50,
    paidOffOn: null,
    lastMonthInterest: null,
    lastMonthPayment: null,
    ...overrides,
  };
}

function step(debtId, name, endedOn, startsOn, minimum, breathingRoom) {
  return { debtId, name, endedOn, startsOn, minimum, breathingRoom };
}

/**
 * Store pays off in February and Card in April on rollover.
 * Card's first payment is a month later than Store's, so its opening balance carries into January.
 */
function rolloverPath(overrides = {}) {
  return {
    kind: "Rollover",
    steps: [
      step("store", "Store", "2026-02-15", "2026-03-15", 50, 0),
      step("card", "Card", "2026-04-20", "2026-05-20", 25, 75),
    ],
    startingObligation: 75,
    remainingObligation: 0,
    recurringRoom: 75,
    paidOffOn: "2026-04-20",
    totalInterest: 3.4,
    debts: [
      debt("card", "Card", {
        balance: 200,
        minimum: 25,
        paidOffOn: "2026-04-20",
      }),
      debt("store", "Store", { paidOffOn: "2026-02-15" }),
    ],
    balancePoints: [
      { debtId: "store", dueDate: "2026-01-15", balance: 50 },
      { debtId: "card", dueDate: "2026-02-20", balance: 175 },
      { debtId: "store", dueDate: "2026-02-15", balance: 0 },
      { debtId: "card", dueDate: "2026-03-20", balance: 100 },
      { debtId: "card", dueDate: "2026-04-20", balance: 0 },
    ].sort((a, b) => a.dueDate.localeCompare(b.dueDate)),
    ...overrides,
  };
}

function cashWindow(from, through, endingCash, lowestCash, lowestCashOn) {
  return {
    from,
    through,
    endingCash,
    lowestCash,
    lowestCashOn,
    cashShortfall: lowestCash < 0,
  };
}

/**
 * Three days of cash from Oct 7: payday, then rent, then nothing.
 * The 6-month horizon goes short; 12 and 18 recover.
 */
function cashForecast(overrides = {}) {
  return {
    days: [
      { date: "2026-10-07", cash: 900, income: 0, bills: 0, debtPayments: 100 },
      { date: "2026-10-08", cash: 300, income: 0, bills: 600, debtPayments: 0 },
      { date: "2026-10-09", cash: 300, income: 0, bills: 0, debtPayments: 0 },
    ],
    dayView: cashWindow("2026-10-07", "2026-11-05", 300, 300, "2026-10-08"),
    horizons: [
      {
        months: 6,
        window: cashWindow(
          "2026-10-07",
          "2027-04-06",
          120,
          -45.5,
          "2027-03-01",
        ),
        minimumObligation: 45,
        unknownMinimumCount: 1,
      },
      {
        months: 12,
        window: cashWindow(
          "2026-10-07",
          "2027-10-06",
          800,
          -45.5,
          "2027-03-01",
        ),
        minimumObligation: 0,
        unknownMinimumCount: 0,
      },
      {
        months: 18,
        window: cashWindow(
          "2026-10-07",
          "2028-04-06",
          1500,
          -45.5,
          "2027-03-01",
        ),
        minimumObligation: null,
        unknownMinimumCount: 2,
      },
    ],
    shortfallOn: "2027-02-27",
    recoveredOn: "2027-03-05",
    ...overrides,
  };
}

function affordableCashForecast(overrides = {}) {
  const forecast = cashForecast({
    shortfallOn: null,
    recoveredOn: null,
    reserveShortfallOn: null,
    reserveRestoredOn: null,
  });
  forecast.days = forecast.days.map((day) => ({ ...day, cash: 300 }));
  forecast.dayView = cashWindow(
    "2026-10-07",
    "2026-11-05",
    300,
    300,
    "2026-10-08",
  );
  forecast.horizons = forecast.horizons.map((horizon) => ({
    ...horizon,
    window: cashWindow(
      horizon.window.from,
      horizon.window.through,
      horizon.window.endingCash,
      100,
      horizon.window.lowestCashOn,
    ),
  }));
  return { ...forecast, ...overrides };
}

function cashOutlook(overrides = {}) {
  const path = { typical: cashForecast(), lowPay: null };
  return {
    asOf: "2026-10-07",
    startingCash: 1000,
    startingReserve: 0,
    startingAvailable: 1000,
    startingCashAccountCount: 1,
    startingCashManualAccountCount: 1,
    startingCashConnectedAccountCount: 0,
    startingCashOldestAsOf: "2026-10-07",
    startingCashUnknownDateCount: 0,
    startingCashStaleConnectedCount: 0,
    hasIncome: true,
    hasBills: true,
    excludedCurrencies: [],
    rollover: path,
    reclaimAll: path,
    ...overrides,
  };
}

function report(pathValue, overrides = {}) {
  return {
    planningCurrency: "USD",
    rollover: pathValue,
    reclaimAll: pathValue,
    excludedCurrencies: [],
    missingBalance: [],
    hasDebts: true,
    debtFacts: [
      {
        debtId: "card",
        name: "Card",
        balance: 200,
        balanceAsOf: "2026-10-07",
        balanceSource: "Manual",
        freshness: null,
        minimumPayment: 25,
        needsPaymentReview: false,
      },
    ],
    hasCashFloor: true,
    hasEmergencyGoal: true,
    namedSavingsGoalCount: 0,
    livingSpendingMonthly: 300,
    monthlyExtra: 0,
    cashOutlook: cashOutlook(),
    ...overrides,
  };
}

test("a debt keeps its rollover color, and a ninth debt reuses the first", () => {
  const debts = Array.from({ length: 9 }, (_, index) =>
    debt(`d${index}`, `Debt ${index}`),
  );
  const colors = series.debtColors(debts);
  assert.equal(colors.d0, "var(--series-1)");
  assert.equal(colors.d7, "var(--series-8)");
  assert.equal(colors.d8, "var(--series-1)");
});

test("bands stack in payoff order and each month carries the last balance", () => {
  const path = rolloverPath();
  const colors = series.debtColors(path.debts);
  const { bands, rows } = series.balanceChart(path, colors);

  assert.deepEqual(
    bands.map((band) => [band.name, band.color]),
    [
      ["Store", "var(--series-2)"],
      ["Card", "var(--series-1)"],
    ],
  );
  assert.deepEqual(
    rows.map((row) => row.timestamp),
    [utc(2025, 12), utc(2026, 1), utc(2026, 2), utc(2026, 3), utc(2026, 4)],
  );
  const [store, card] = bands.map((band) => band.key);
  assert.deepEqual(
    rows.map((row) => [row[store], row[card], row.total]),
    [
      [100, 200, 300],
      [50, 200, 250],
      [0, 175, 175],
      [0, 100, 100],
      [0, 0, 0],
    ],
  );
});

test("a debt that does not pay off is not drawn and does not count in the total", () => {
  const path = rolloverPath({
    steps: [step("store", "Store", "2026-02-15", "2026-03-15", 50, 50)],
    paidOffOn: null,
    debts: [
      debt("card", "Card", {
        stop: "DoesNotPayDown",
        balance: 200,
        minimum: 1,
      }),
      debt("store", "Store", { paidOffOn: "2026-02-15" }),
    ],
    balancePoints: [
      { debtId: "store", dueDate: "2026-01-15", balance: 50 },
      { debtId: "store", dueDate: "2026-02-15", balance: 0 },
      { debtId: "card", dueDate: "2026-02-20", balance: 201 },
    ],
  });
  const { bands, rows } = series.balanceChart(
    path,
    series.debtColors(path.debts),
  );

  assert.deepEqual(
    bands.map((band) => band.debtId),
    ["store"],
  );
  assert.deepEqual(
    rows.map((row) => row.total),
    [100, 50, 0],
  );
});

test("no payoff draws no balance chart and no step chart", () => {
  const path = rolloverPath({
    steps: [],
    paidOffOn: null,
    debts: [debt("card", "Card", { stop: "RateUnknown", minimum: null })],
    balancePoints: [],
  });
  assert.deepEqual(series.balanceChart(path, {}).rows, []);
  assert.deepEqual(series.obligationChart(path), []);
});

test("minimums step down on each removal date and breathing room steps up", () => {
  const rows = series.obligationChart(rolloverPath());
  assert.deepEqual(
    rows.map((row) => [row.timestamp, row.minimums, row.room]),
    [
      [utc(2025, 12), 75, 0],
      [utc(2026, 3), 25, 0],
      [utc(2026, 5), 0, 75],
      [utc(2026, 6), 0, 75],
    ],
  );
});

test("a removal date outside the projection is not stepped", () => {
  const path = rolloverPath();
  path.steps[1] = { ...path.steps[1], startsOn: null };
  const rows = series.obligationChart(path);
  assert.deepEqual(
    rows.map((row) => row.minimums),
    [75, 25, 25],
  );
});

test("month ticks stay at six or fewer and keep both ends", () => {
  const rows = Array.from({ length: 25 }, (_, index) => ({ timestamp: index }));
  const ticks = series.evenTicks(rows);
  assert.equal(ticks.length, 6);
  assert.equal(ticks[0], 0);
  assert.equal(ticks[5], 24);
  assert.deepEqual(series.evenTicks(rows.slice(0, 3)), [0, 1, 2]);
  assert.equal(series.formatMonthTick(utc(2027, 1)), "Jan 27");
});

test("the payoff order follows the steps with the minimum each one frees", () => {
  const path = rolloverPath();
  const order = series.payoffOrder(path, series.debtColors(path.debts));
  assert.deepEqual(
    order.map((row) => [
      row.name,
      row.paidOffOn,
      row.minimum,
      row.breathingRoom,
      row.color,
    ]),
    [
      ["Store", "2026-02-15", 50, 0, "var(--series-2)"],
      ["Card", "2026-04-20", 25, 75, "var(--series-1)"],
    ],
  );
});

test("a full payoff leads with the debt-free date at minimums and dates the breathing room after payoff", () => {
  const summary = copy.planSummary(
    rolloverPath(),
    affordableCashForecast(),
    0,
    money,
    date,
  );
  assert.equal(
    summary.sentence,
    "You're on track to be debt-free paying only your minimums. Anything extra brings that day closer.",
  );
  assert.equal(summary.warning, false);
  assert.equal(summary.figureLabel, "Debt-free at minimums only");
  assert.equal(summary.figure, "2026-04-20");
  assert.deepEqual(summary.details, [
    { label: "Back to you after payoff", value: "$75.00 a month" },
    { label: "Total interest", value: "$3.40" },
  ]);
});

test("a cash shortfall replaces on-track language and makes the payoff date conditional", () => {
  const summary = copy.planSummary(
    rolloverPath(),
    cashForecast(),
    0,
    money,
    date,
  );

  assert.equal(summary.warning, true);
  assert.equal(
    summary.sentence,
    "This payoff path is not affordable yet. Cash runs short on 2027-02-27.",
  );
  assert.equal(summary.figureLabel, "First cash shortfall");
  assert.equal(summary.figure, "2027-02-27");
  assert.deepEqual(summary.details[0], {
    label: "Conditional debt-free estimate",
    value: "2026-04-20",
  });
  assert.equal(summary.sentence.includes("on track"), false);
});

test("a partial payoff counts the debts paid off and names what stays due", () => {
  const summary = copy.planSummary(
    rolloverPath({
      steps: [step("store", "Store", "2026-02-15", "2026-03-15", 50, 50)],
      paidOffOn: null,
      remainingObligation: 25,
      recurringRoom: 50,
    }),
    affordableCashForecast(),
    1,
    money,
    date,
  );
  assert.equal(
    summary.sentence,
    "1 of 2 debts paid off by 2026-02-15. Minimums drop from $75.00 to $25.00.",
  );
  assert.equal(summary.warning, true);
  assert.equal(summary.figureLabel, "Back to you each month after 2026-02-15");
  assert.equal(summary.figure, "$50.00");
  assert.deepEqual(summary.details, [
    { label: "Debt-free", value: "Not projected yet" },
  ]);
});

test("no payoff counts the debts needing attention and leads with the total owed", () => {
  const waiting = rolloverPath({
    steps: [],
    paidOffOn: null,
    startingObligation: 45,
    debts: [
      debt("rei", "REI Mastercard", {
        stop: "DoesNotPayDown",
        balance: 2400,
        minimum: 45,
      }),
      debt("paypal", "PayPal Credit", {
        stop: "DueDateUnknown",
        balance: 600,
        minimum: null,
      }),
    ],
  });
  const summary = copy.planSummary(
    waiting,
    affordableCashForecast(),
    2,
    money,
    date,
  );
  assert.equal(
    summary.sentence,
    "2 debts need attention before your plan can project a payoff.",
  );
  assert.equal(summary.warning, true);
  assert.equal(summary.figureLabel, "Owed today");
  assert.equal(summary.figure, "$3000.00");
  assert.deepEqual(summary.details, [
    { label: "Monthly minimums", value: "$45.00 a month, 1 not included" },
  ]);

  assert.equal(
    copy.planSummary(waiting, affordableCashForecast(), 1, money, date)
      .sentence,
    "1 debt needs attention before your plan can project a payoff.",
  );
  const unknown = copy.planSummary(
    { ...waiting, startingObligation: null },
    affordableCashForecast(),
    0,
    money,
    date,
  );
  assert.equal(unknown.sentence, "Your plan can't project a payoff yet.");
  assert.deepEqual(unknown.details, [
    { label: "Monthly minimums", value: "Unknown" },
  ]);
});

test("each debt's share of what is owed is a whole percent and they add to 100", () => {
  const debts = [
    debt("a", "A", { balance: 1 }),
    debt("b", "B", { balance: 1 }),
    debt("c", "C", { balance: 1 }),
    debt("z", "Zero", { balance: 0 }),
  ];
  const shares = series.owedShares(debts, series.debtColors(debts));
  assert.deepEqual(
    shares.map((share) => share.percent),
    [34, 33, 33],
  );
  assert.equal(shares.length, 3);

  const uneven = [
    debt("small", "Small", { balance: 600 }),
    debt("big", "Big", { balance: 2400 }),
  ];
  assert.deepEqual(
    series
      .owedShares(uneven, series.debtColors(uneven))
      .map((share) => [share.name, share.percent, share.color]),
    [
      ["Big", 80, "var(--series-2)"],
      ["Small", 20, "var(--series-1)"],
    ],
  );
  assert.deepEqual(
    series.owedShares([debt("z", "Zero", { balance: 0 })], {}),
    [],
  );
});

test("finish your plan names what blocks each debt, then no balance, then currencies", () => {
  const path = rolloverPath({
    debts: [
      debt("rei", "REI Mastercard", {
        stop: "DoesNotPayDown",
        minimum: 45,
        lastMonthInterest: 52.13,
        lastMonthPayment: 45,
      }),
      debt("even", "Even card", {
        stop: "DoesNotPayDown",
        minimum: 20,
        lastMonthInterest: 20,
        lastMonthPayment: 20,
      }),
      debt("old", "Unmodeled", { stop: "DoesNotPayDown", minimum: 15 }),
      debt("paypal", "PayPal Credit", {
        stop: "DueDateUnknown",
        minimum: null,
      }),
      debt("promo", "Promo card", { stop: "RateUnknown", minimum: 20 }),
      debt("store", "Store card", { stop: "RateUnknown", minimum: null }),
      debt("min", "Loan", { stop: "MinimumUnknown", minimum: null }),
      debt("long", "Mortgage", { stop: "HorizonReached", minimum: 10 }),
      debt("done", "Done", { stop: "PaidOff" }),
    ],
  });
  const items = copy.finishPlanItems(
    report(path, {
      missingBalance: [{ debtId: "blank", name: "Blank card" }],
      excludedCurrencies: ["CAD"],
    }),
    path,
    money,
  );

  assert.deepEqual(
    items.map((item) => `${item.name}: ${item.fix} [${item.action}]`),
    [
      "REI Mastercard: Interest is about $52.13 a month, more than your $45.00 payment. Paying $53.00 or more starts paying it down. [Update payment]",
      "Even card: Interest is about $20.00 a month, the same as your $20.00 payment. Paying $21.00 or more starts paying it down. [Update payment]",
      "Unmodeled: $15.00 a month doesn't pay this down. [Update payment]",
      "PayPal Credit: Missing a due date. [Add due date]",
      "Promo card: Missing the interest rate after the promotion ends. [Add rate]",
      "Store card: Missing an interest rate. [Add rate]",
      "Loan: Missing a minimum payment. [Add minimum]",
      "Mortgage: At this payment, the payoff falls past the 50-year limit. [Update payment]",
      "Blank card: Missing a balance, so this debt is left out. [Add balance]",
      "CAD debts: Left out. This plan uses USD. [Review]",
    ],
  );
  assert.equal(items.filter((item) => item.isDebt).length, 9);
  assert.equal(
    copy.finishPlanItems(report(rolloverPath()), rolloverPath(), money).length,
    0,
  );
});

test("a shortfall with the interest fields missing from the response falls back to the minimum", () => {
  const stale = debt("rei", "REI Mastercard", {
    stop: "DoesNotPayDown",
    minimum: 45,
  });
  delete stale.lastMonthInterest;
  delete stale.lastMonthPayment;
  const path = rolloverPath({ debts: [stale] });

  const [item] = copy.finishPlanItems(report(path), path, money);

  assert.equal(item.fix, "$45.00 a month doesn't pay this down.");
});

test("how this is calculated is one short line per rule and follows the switch", () => {
  const rollover = copy.planAssumptions("Rollover", "USD");
  const kept = copy.planAssumptions("ReclaimAll", "USD");
  const terms = (rules) => rules.map((rule) => rule.term);

  assert.ok(terms(rollover).includes("Roll payments forward"));
  assert.ok(!terms(rollover).includes("Free up cash"));
  assert.ok(terms(kept).includes("Free up cash"));
  assert.ok(!terms(kept).includes("Roll payments forward"));
  assert.equal(
    rollover.find((rule) => rule.term === "Currency").detail,
    "Only USD debts are counted.",
  );
  for (const rule of [...rollover, ...kept]) {
    assert.ok(rule.detail.length <= 140, `${rule.term} stays short`);
  }
});

test("the 30-day cash chart has one row per day at UTC midnight and marks the lowest day", () => {
  const { rows, lowest } = series.cashChart(cashForecast());
  assert.deepEqual(
    rows.map((row) => [row.timestamp, row.cash, row.bills, row.debtPayments]),
    [
      [Date.UTC(2026, 9, 7), 900, 0, 100],
      [Date.UTC(2026, 9, 8), 300, 600, 0],
      [Date.UTC(2026, 9, 9), 300, 0, 0],
    ],
  );
  assert.equal(lowest.timestamp, Date.UTC(2026, 9, 8));
  assert.equal(series.formatDayTick(Date.UTC(2026, 9, 7)), "Oct 7");
  assert.equal(series.formatDayLabel(Date.UTC(2026, 9, 7)), "Wed, Oct 7");

  const empty = series.cashChart(cashForecast({ days: [] }));
  assert.deepEqual(empty, { rows: [], lowest: null });
});

test("cash ranges use daily points only for 30 days and honest horizon summaries after that", () => {
  const forecast = cashForecast();
  const chart = series.cashChart(forecast);
  const thirty = cashRange.cashRangeView(
    forecast,
    rolloverPath(),
    "30-days",
    chart,
    money,
    date,
  );
  const six = cashRange.cashRangeView(
    forecast,
    rolloverPath(),
    "6-months",
    chart,
    money,
    date,
  );
  const twelve = cashRange.cashRangeView(
    forecast,
    rolloverPath(),
    "12-months",
    chart,
    money,
    date,
  );
  const eighteen = cashRange.cashRangeView(
    forecast,
    rolloverPath(),
    "18-months",
    chart,
    money,
    date,
  );

  assert.equal(thirty.label, "30 days");
  assert.equal(thirty.warning, false);
  assert.equal(thirty.chartRows.length, 3);
  assert.match(thirty.chartLabel, /ending at \$300\.00/);
  assert.equal(six.warningText, "6 months cash runs short on 2027-02-27.");
  assert.equal(six.ending, "$120.00");
  assert.equal(six.lowest, "$-45.50");
  assert.equal(six.minimums, "$45.00 a month, 1 not included");
  assert.equal(six.chartRows.length, 0);
  assert.equal(twelve.minimums, "None");
  assert.equal(eighteen.minimums, "Unknown");
});

test("a selected range names protected-cash pressure only when it falls inside that period", () => {
  const forecast = cashForecast({
    reserveShortfallOn: "2027-02-01",
    reserveRestoredOn: "2027-02-15",
  });
  const chart = series.cashChart(forecast);
  const thirty = cashRange.cashRangeView(
    forecast,
    rolloverPath(),
    "30-days",
    chart,
    money,
    date,
  );
  const six = cashRange.cashRangeView(
    forecast,
    rolloverPath(),
    "6-months",
    chart,
    money,
    date,
  );

  assert.equal(thirty.reserveWarningText, null);
  assert.equal(
    six.reserveWarningText,
    "6 months available cash after protected savings runs short on 2027-02-01.",
  );
});

test("a missing horizon is unavailable instead of reusing the 30-day values", () => {
  const forecast = cashForecast({ horizons: [] });
  const view = cashRange.cashRangeView(
    forecast,
    rolloverPath(),
    "12-months",
    series.cashChart(forecast),
    money,
    date,
  );

  assert.equal(view.available, false);
  assert.equal(view.ending, "Unavailable");
  assert.equal(view.chartRows.length, 0);
});

test("the cash outlook line says where cash starts and follows the switch", () => {
  assert.equal(
    copy.cashOutlookDescription("Rollover", false, 1000, money),
    "Starts from $1000.00 in Cash on Accounts today.",
  );
  assert.match(
    copy.cashOutlookDescription("Rollover", true, 1000, money),
    /moves to the next debt\.$/,
  );
  assert.match(
    copy.cashOutlookDescription("ReclaimAll", true, 1000, money),
    /comes back as cash\.$/,
  );
  assert.equal(
    copy.cashOutlookDescription("Rollover", false, 1000, money, 0, 200, 800),
    "Starts from $1000.00 in Cash on Accounts today. $200.00 is protected. $800.00 is available after protected savings.",
  );
});

test("cash outlook notes name debts whose payments are left out, then missing bills, then currencies", () => {
  const path = rolloverPath({
    debts: [
      debt("rei", "REI Mastercard", { stop: "DoesNotPayDown" }),
      debt("paypal", "PayPal Credit", { stop: "DueDateUnknown" }),
      debt("long", "Mortgage", { stop: "HorizonReached" }),
      debt("done", "Done"),
    ],
  });
  const notes = copy.cashOutlookNotes(
    report(path, {
      missingBalance: [{ debtId: "blank", name: "Blank card" }],
      cashOutlook: cashOutlook({
        hasBills: false,
        excludedCurrencies: ["CAD"],
      }),
    }),
    path,
  );

  assert.deepEqual(
    notes.map((note) => [note.key, note.warning]),
    [
      ["left-out", true],
      ["no-bills", false],
      ["currency", false],
    ],
  );
  assert.equal(
    notes[0].text,
    "Payments the plan can't project yet are left out for REI Mastercard, PayPal Credit, and Blank card, so cash may be lower than shown. Finish your plan to include them.",
  );
  assert.equal(
    notes[2].text,
    "Amounts in CAD are left out. The outlook uses USD.",
  );
  assert.deepEqual(
    copy.cashOutlookNotes(report(rolloverPath()), rolloverPath()),
    [],
  );
});

test("how this is calculated names the due date, cash outlook, and low pay rules", () => {
  const terms = copy
    .planAssumptions("Rollover", "USD")
    .map((rule) => rule.term);
  for (const term of ["Due dates", "Cash outlook", "Low pay"]) {
    assert.ok(terms.includes(term), term);
  }
});

test("the switch offers plain-language payment paths with a line for each", () => {
  assert.deepEqual(
    copy.planPathOptions.map((option) => option.label),
    ["Roll payments forward", "Free up cash"],
  );
  assert.match(copy.planPathDescription("Rollover"), /moves to the next debt/);
  assert.match(copy.planPathDescription("ReclaimAll"), /available cash/);
});

test("readiness preserves stale and inconsistent facts after affordability is clear", () => {
  const value = report(rolloverPath(), {
    debtFacts: [
      {
        debtId: "paid",
        name: "Paid card",
        balance: 0,
        balanceAsOf: "2026-10-01",
        balanceSource: "Synced",
        freshness: "Stale",
        minimumPayment: 125,
        needsPaymentReview: true,
      },
      {
        debtId: "card",
        name: "Card",
        balance: 200,
        balanceAsOf: "2026-10-07",
        balanceSource: "Manual",
        freshness: null,
        minimumPayment: 25,
        needsPaymentReview: false,
      },
    ],
  });
  const result = readiness.planReadiness(
    value,
    affordableCashForecast(),
    0,
    date,
  );

  assert.equal(result.next.title, "Refresh debt balances");
  assert.equal(result.next.href, "/debts");
  assert.match(
    result.rows.find((row) => row.key === "debts").detail,
    /zero-balance payment review/,
  );
  assert.match(
    result.rows.find((row) => row.key === "cash").detail,
    /oldest balance date/i,
  );
});

test("readiness leads to Plan budget when complete inputs still run short", () => {
  const result = readiness.planReadiness(
    report(rolloverPath()),
    cashForecast(),
    0,
    date,
  );

  assert.equal(result.next.title, "Fix the dated cash shortfall");
  assert.equal(result.next.href, "/living");
  assert.equal(result.next.label, "Review shortfall");
});

test("readiness keeps protected-cash affordability ahead of setup improvements", () => {
  const result = readiness.planReadiness(
    report(rolloverPath(), { hasEmergencyGoal: false }),
    affordableCashForecast({ reserveShortfallOn: "2026-10-20" }),
    0,
    date,
  );

  assert.equal(result.next.title, "Review protected savings");
  assert.equal(result.next.href, "/savings");
});

test("a tried extra updates plan copy and names the applied forecast while loading", () => {
  assert.equal(extra.parsePlanExtra(""), 0);
  assert.equal(extra.parsePlanExtra("  200.005 "), 200.01);
  assert.equal(extra.parsePlanExtra("-1"), null);
  assert.equal(extra.parsePlanExtra("abc"), null);
  assert.equal(extra.parsePlanExtra("1,200"), 1200);
  assert.equal(extra.parsePlanExtra("$1,200.50"), 1200.5);

  const summary = copy.planSummary(
    rolloverPath(),
    affordableCashForecast(),
    0,
    money,
    date,
    200,
  );
  assert.equal(
    summary.sentence,
    "You're on track to be debt-free with $200.00 extra each month.",
  );
  assert.equal(summary.figureLabel, "Debt-free");

  const rules = copy.planAssumptions("Rollover", "USD", 200, money);
  assert.equal(
    rules.find((rule) => rule.term === "Order").detail,
    "Highest interest rate first. $200.00 extra each month goes to the first debt that can take it.",
  );
  assert.match(
    rules.find((rule) => rule.term === "Saved").detail,
    /stays on the page until you leave/,
  );
  for (const rule of rules) {
    assert.ok(rule.detail.length <= 140, `${rule.term} stays short`);
  }

  assert.match(
    copy.cashOutlookDescription("Rollover", true, 1000, money, 200),
    /\$200\.00 extra each month is included in the debt payments\.$/,
  );
  assert.equal(
    copy.planExtraHelp(),
    "Enter 0 for minimums only. The preview updates when you leave the field or press Enter.",
  );
  assert.equal(
    copy.planExtraStatus("updating", 200, 0, money),
    "Updating the preview for $200.00 extra. Results still show $0.00 extra until it finishes.",
  );
  assert.equal(
    copy.planExtraStatus("error", 200, 0, money),
    "The $200.00 extra preview could not be applied. Results still show $0.00 extra. Leave the field or press Enter to try again.",
  );
  assert.equal(copy.planExtraInvalid(), "Enter a zero or positive amount.");
});

test("Plan tab keys wrap and support Home and End", () => {
  assert.equal(navigation.movePlanTab("overview", "ArrowLeft"), "debt");
  assert.equal(navigation.movePlanTab("debt", "ArrowRight"), "overview");
  assert.equal(navigation.movePlanTab("cash", "Home"), "overview");
  assert.equal(navigation.movePlanTab("cash", "End"), "debt");
});

test("debt milestones distinguish rolled payments from available cash", () => {
  const rollover = debtSummary.debtMilestones(
    rolloverPath(),
    "Rollover",
    money,
    date,
  );
  const reclaimed = debtSummary.debtMilestones(
    { ...rolloverPath(), kind: "ReclaimAll" },
    "ReclaimAll",
    money,
    date,
  );

  assert.equal(rollover.currentMinimums, "$75.00 a month");
  assert.equal(rollover.firstPayoff, "Store · 2026-02-15");
  assert.match(rollover.breathingRoomNote, /no modeled debt can take/);
  assert.match(reclaimed.breathingRoomNote, /other uses/);
});
