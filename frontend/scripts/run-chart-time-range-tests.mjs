import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

const sourcePath = path.resolve("features/accounts/chartTimeRange.ts");
const source = await readFile(sourcePath, "utf8");
const output = ts.transpileModule(source, {
  compilerOptions: {
    module: ts.ModuleKind.ES2022,
    target: ts.ScriptTarget.ES2022,
    importsNotUsedAsValues: ts.ImportsNotUsedAsValues.Remove,
  },
});

const tempDir = path.resolve(".tmp-tests");
await mkdir(tempDir, { recursive: true });
const compiledPath = path.join(tempDir, "chartTimeRange.mjs");
await writeFile(compiledPath, output.outputText);

const chartTimeRange = await import(pathToFileURL(compiledPath).href);

test("computePeriodChange returns absolute and percentage deltas", () => {
  const change = chartTimeRange.computePeriodChange([
    {
      date: "2026-07-01",
      netWorth: 100,
      cash: 100,
      investments: 0,
      creditCards: 0,
      loans: 0,
    },
    {
      date: "2026-07-10",
      netWorth: 125,
      cash: 125,
      investments: 0,
      creditCards: 0,
      loans: 0,
    },
  ]);

  assert.deepEqual(change, {
    delta: 25,
    deltaPercent: 25,
    startValue: 100,
    endValue: 125,
  });
});

test("formatPeriodDelta preserves direction and percent text", () => {
  assert.deepEqual(
    chartTimeRange.formatPeriodDelta({
      delta: -50,
      deltaPercent: -12.345,
      startValue: 400,
      endValue: 350,
    }),
    {
      amount: "-$50",
      percent: "-12.3%",
    },
  );
});

test("formatChartAxisCurrency compacts large balances", () => {
  assert.equal(chartTimeRange.formatChartAxisCurrency(663000), "$663K");
  assert.equal(chartTimeRange.formatChartAxisCurrency(-5300), "-$5.3K");
  assert.equal(chartTimeRange.formatChartAxisCurrency(1250000), "$1.3M");
});

test("getRangeTicks returns evenly spaced numeric ticks", () => {
  assert.deepEqual(
    chartTimeRange.getRangeTicks({ startMs: 1000, endMs: 5000 }, 3),
    [1000, 3000, 5000],
  );
});

test("getInsufficientHistoryMessage explains missing snapshots", () => {
  assert.equal(
    chartTimeRange.getInsufficientHistoryMessage(0, "1W"),
    "No balance snapshots were recorded in the past week. Balance history is captured during account sync.",
  );
  assert.equal(
    chartTimeRange.getInsufficientHistoryMessage(1, "1M"),
    "Only one day of balance history was recorded in the past month. At least two days are needed to show a trend.",
  );
});
