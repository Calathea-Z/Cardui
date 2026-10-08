import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

/**
 * Loads a targets module after erasing its types.
 * The modules under test do not import the app at runtime.
 */
async function loadModule(fileName) {
  const source = await readFile(
    path.resolve("features/targets", fileName),
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
  const compiledPath = path.join(tempDir, fileName.replace(/\.ts$/, ".mjs"));
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

const copy = await loadModule("categoryTargetCopy.ts");
const form = await loadModule("categoryTargetForm.ts");
const money = (amount) => `$${amount.toFixed(2)}`;

test("a missing target stays open and an over amount is named", () => {
  assert.equal(copy.remainingText(null, money), "No target");
  assert.equal(copy.remainingText(0, money), "$0.00 left");
  assert.equal(copy.remainingText(12.5, money), "$12.50 left");
  assert.equal(copy.remainingText(-4, money), "$4.00 over");
});

test("summary totals stay quiet when no category has a target", () => {
  const metrics = copy.summaryMetrics(
    { targetTotal: null, spent: 15, remaining: null },
    money,
  );
  assert.equal(metrics[0].value, "No targets");
  assert.equal(metrics[0].known, false);
  assert.equal(metrics[1].value, "$15.00");
  assert.equal(metrics[2].value, "No targets");
});

test("spending with no target is named and is not called remaining", () => {
  const signals = copy.summarySignals(
    {
      otherSpent: 15,
      missingTargetCount: 2,
      unassignedRolloverCount: 1,
      excludedTransactionCount: 1,
      excludedCurrencies: ["EUR"],
    },
    money,
  );
  assert.equal(
    signals[0],
    "$15.00 of spending has no target, so it is not part of remaining.",
  );
  assert.equal(signals[1], "2 categories have no target.");
  assert.match(signals[2], /amount from last month/);
  assert.match(signals[3], /EUR/);
});

test("a preview names its source and does not skip a month", () => {
  assert.equal(
    copy.copyForwardNote({
      saved: false,
      copiedFromYear: 2026,
      copiedFromMonth: 9,
      year: 2026,
      month: 10,
    }),
    "October 2026 has no saved targets. These amounts are from September 2026.",
  );
  assert.match(
    copy.copyForwardNote({
      saved: false,
      copiedFromYear: 2026,
      copiedFromMonth: 7,
      year: 2026,
      month: 10,
    }),
    /does not skip September 2026/,
  );
  assert.equal(
    copy.copyForwardNote({
      saved: true,
      copiedFromYear: 2026,
      copiedFromMonth: 9,
      year: 2026,
      month: 10,
    }),
    null,
  );
});

test("the current month stops today and a future month has not started", () => {
  assert.match(
    copy.spentPeriodNote({
      throughToday: true,
      year: 2026,
      month: 10,
      todayYear: 2026,
      todayMonth: 10,
    }),
    /through today/,
  );
  assert.equal(
    copy.spentPeriodNote({
      throughToday: false,
      year: 2026,
      month: 9,
      todayYear: 2026,
      todayMonth: 10,
    }),
    "Spent is the full month.",
  );
  assert.match(
    copy.spentPeriodNote({
      throughToday: false,
      year: 2026,
      month: 11,
      todayYear: 2026,
      todayMonth: 10,
    }),
    /has not started/,
  );
});

test("rollover money and the choice to roll ahead are separate", () => {
  assert.equal(
    copy.rolloverDetail(40, true, "September 2026", money),
    "Includes $40.00 from September 2026. Rolls into next month.",
  );
  assert.equal(
    copy.rolloverDetail(-10, false, "September 2026", money),
    "Starts $10.00 over from September 2026.",
  );
  assert.equal(copy.rolloverDetail(0, false, "September 2026", money), null);
});

test("month choices run from eleven months ago through next month", () => {
  const choices = copy.monthChoices(2026, 10);
  assert.equal(choices[0].label, "November 2025");
  assert.equal(choices.at(-1).label, "November 2026");
  assert.equal(copy.parseMonthKey(choices[11].value).month, 10);
});

test("a blank target is not zero and zero is kept", () => {
  assert.equal(form.readTargetAmount("").ok, false);
  assert.equal(form.readTargetAmount("0").ok, true);
  assert.equal(form.readTargetAmount("0").amount, 0);
  assert.equal(form.readTargetAmount("12.50").amount, 12.5);
  assert.equal(form.readTargetAmount("12.501").ok, false);
  assert.equal(form.readTargetAmount("-1").ok, false);
  assert.equal(form.readTargetAmount("30,000").amount, 30000);
  assert.equal(form.readTargetAmount("30,00").ok, false);
  assert.match(form.readTargetAmount("30,44").error, /30\.44/);
  assert.equal(form.targetToForm({ target: null, rollover: true }).amount, "");
  assert.equal(form.targetToForm({ target: 0, rollover: false }).amount, "0");
});
