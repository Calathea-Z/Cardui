import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

/**
 * Loads the summary copy module after erasing its types.
 * The module has no runtime imports.
 */
async function loadCopy() {
  const source = await readFile(
    path.resolve("features/debts/debtSummaryCopy.ts"),
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
  const compiledPath = path.join(tempDir, "debtSummaryCopy.mjs");
  await writeFile(compiledPath, output.outputText);
  return import(pathToFileURL(compiledPath).href);
}

const copy = await loadCopy();
const money = (amount, currency) => `${currency} ${amount.toFixed(2)}`;
const share = (ratio) => `${Math.round(ratio * 100)}%`;
const date = (value) => value;

const thresholds = {
  aprNoticePercent: 20,
  utilizationNotice: 0.3,
  utilizationLimitNotice: 0.9,
  promotionalNoticeDays: 60,
};

function group(overrides = {}) {
  return {
    currency: "USD",
    debtCount: 1,
    recordedBalance: 100,
    unknownBalanceCount: 0,
    monthlyInterest: 1.5,
    unknownInterestCount: 0,
    minimumPayments: 25,
    unknownMinimumCount: 0,
    utilization: 0.25,
    utilizationDebtCount: 1,
    unknownUtilizationCount: 0,
    dueDatePassedCount: 0,
    promoEndingCount: 0,
    promotionEndedCount: 0,
    aprNoticeCount: 0,
    utilizationNoticeCount: 0,
    utilizationLimitNoticeCount: 0,
    balanceDifferenceCount: 0,
    missingDueDateCount: 0,
    missingRemainingTermCount: 0,
    missingPromotionalEndCount: 0,
    missingPromotionalRateCount: 0,
    missingRateAfterPromotionCount: 0,
    ...overrides,
  };
}

function report(currencies) {
  return { ...thresholds, currencies, debts: [] };
}

test("unknown totals stay unknown and a known zero stays an amount", () => {
  const metrics = copy.summaryMetrics(
    group({
      recordedBalance: null,
      unknownBalanceCount: 1,
      monthlyInterest: null,
      unknownInterestCount: 1,
      minimumPayments: 0,
      unknownMinimumCount: 1,
      utilization: null,
      utilizationDebtCount: 0,
      unknownUtilizationCount: 1,
    }),
    report([]),
    share,
    money,
  );

  assert.equal(metrics[0].value, "Unknown");
  assert.equal(metrics[0].known, false);
  assert.equal(metrics[1].value, "Unknown");
  assert.equal(metrics[2].value, "USD 0.00");
  assert.equal(metrics[2].detail, "1 unknown");
  assert.equal(metrics[2].known, true);
  assert.equal(metrics[3].value, "Unknown");
  assert.equal(metrics[3].detail, "1 missing a limit");
});

test("the interest rule is one line and a leftover count stays a detail", () => {
  assert.match(copy.summaryInterestNote, /debts below/);
  assert.match(copy.summaryInterestNote, /divided by 12/);
  assert.match(copy.summaryInterestNote, /not the total left to pay/);

  const metrics = copy.summaryMetrics(
    group({
      unknownInterestCount: 2,
      unknownBalanceCount: 1,
      utilization: 0.4,
    }),
    report([]),
    share,
    money,
  );

  assert.equal(metrics[0].detail, "1 unknown");
  assert.equal(metrics[1].value, "USD 1.50");
  assert.equal(metrics[1].detail, "2 missing inputs");
  assert.equal(metrics[3].detail, "30% or more");
});

test("signals stay short and do not repeat the utilization figure", () => {
  const signals = copy.summarySignals(
    report([
      group({
        dueDatePassedCount: 1,
        aprNoticeCount: 1,
        utilizationNoticeCount: 1,
        balanceDifferenceCount: 1,
        missingDueDateCount: 2,
        missingRemainingTermCount: 1,
      }),
    ]),
  );
  const text = signals
    .map((signal) => `${signal.label} ${signal.detail ?? ""}`)
    .join(" ");

  assert.match(text, /Due date passed/);
  assert.match(text, /Payment status unknown/);
  assert.match(text, /20% or higher/);
  assert.match(text, /Still missing/);
  assert.equal(/differ|two balances/i.test(text), false);
  assert.match(text, /2 due dates, 1 term/);
  assert.equal(text.toLowerCase().includes("score"), false);
  assert.equal(text.includes("30%"), false);
});

test("a different account balance is labeled beside the one in use", () => {
  const comparison = copy.balanceComparisonCopy(
    842.5,
    "2026-10-01",
    "USD",
    {
      accountBalance: 900,
      accountBalanceAsOf: "2026-10-05",
      accountCurrency: "USD",
      canUseAccountBalance: true,
      block: "None",
    },
    money,
    date,
  );

  assert.equal(comparison.recordedAmount, "USD 842.50");
  assert.equal(comparison.recordedAsOf, "2026-10-01");
  assert.equal(comparison.recordedStatus, "In use");
  assert.equal(comparison.accountAmount, "USD 900.00");
  assert.equal(comparison.accountAsOf, "2026-10-05");
  assert.equal(comparison.accountStatus, "Not in use");
  assert.match(
    copy.twoBalancesNote(true),
    /totals above use the recorded balance/,
  );
  assert.match(copy.twoBalancesNote(true), /Choose the account balance/);
  assert.equal(
    copy.twoBalancesNote(false).includes("Choose the account balance"),
    false,
  );
});

test("a card names a passed due date once and skips an unknown interest line", () => {
  assert.equal(copy.debtInterestLine(null, false, "USD", money), null);
  assert.equal(
    copy.debtInterestLine(14.03, false, "USD", money),
    "USD 14.03 interest this month",
  );
  assert.equal(
    copy.debtFactLine([], ["APR", "minimum", "due date"]),
    "APR, minimum, and due date unknown",
  );
  assert.equal(copy.utilizationMark(false, true, report([])), "90% or more");

  const lines = copy.debtSummaryLines(
    {
      dueDatePassed: true,
      promoEndsWithinNotice: true,
      promotionEnded: false,
      promotionalEndsOn: "2026-11-01",
    },
    null,
    date,
  );

  assert.match(lines.join(" "), /Due date passed/);
  assert.match(lines.join(" "), /payment status unknown/);
  assert.match(lines.join(" "), /Promo ends 2026-11-01/);
  assert.match(lines.join(" "), /APR after that is unknown/);
});
