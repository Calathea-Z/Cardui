import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

/**
 * Loads the spendable copy after erasing its types.
 */
async function loadCopy() {
  const source = await readFile(
    path.resolve("features/plan/spendableCopy.ts"),
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
  const compiledPath = path.join(tempDir, "spendableCopy.mjs");
  await writeFile(compiledPath, output.outputText);
  return import(pathToFileURL(compiledPath).href);
}

const { spendableCopy } = await loadCopy();
const money = (amount) => `$${amount.toFixed(2)}`;
const date = (value) => value;

/**
 * Returns one spendable path. The caller overrides the fields a test cares about.
 */
function estimate(overrides = {}) {
  return {
    from: "2026-10-15",
    through: "2026-11-13",
    spendable: 50,
    lowestAvailable: 50,
    lowestOn: "2026-10-15",
    reliableIncome: 1000,
    obligations: 400,
    ordinarySpending: 0,
    reserved: 0,
    alreadyShort: false,
    guardrail: {
      active: false,
      ordinaryExpenses: false,
      nearTermObligations: false,
      on: null,
      shortfall: 0,
      alreadyShortWithoutExtra: false,
    },
    uncertainties: [],
    ...overrides,
  };
}

/**
 * Returns the cash freshness fields the copy reads.
 */
function cash(overrides = {}) {
  return {
    startingCashOldestAsOf: "2026-10-14",
    startingCashAccountCount: 1,
    startingCashStaleConnectedCount: 0,
    startingCashUnknownDateCount: 0,
    ...overrides,
  };
}

test("names the horizon and refuses to call the lowest day a monthly surplus", () => {
  const copy = spendableCopy(estimate(), cash(), [], true, money, date);

  assert.match(copy.sentence, /2026-10-15/);
  assert.match(copy.sentence, /2026-11-13/);
  assert.match(copy.sentence, /not a monthly surplus/);
  assert.equal(copy.figure, "$50.00");
  assert.equal(copy.guardrail, null);
  assert.equal(copy.leadsStatus, false);
  assert.equal(copy.pieces[0].value, "$1000.00");
  assert.match(copy.freshness, /2026-10-14/);
});

test("says nothing is spendable when the lowest day is already short", () => {
  const copy = spendableCopy(
    estimate({
      spendable: 0,
      lowestAvailable: -300,
      lowestOn: "2026-10-20",
      alreadyShort: true,
    }),
    cash(),
    [],
    true,
    money,
    date,
  );

  assert.match(copy.sentence, /Nothing is left to spend/);
  assert.equal(copy.figure, "$0.00");
  assert.equal(copy.shortfall, "$-300.00 on 2026-10-20");
});

test("lets a new extra-payment shortfall lead the status", () => {
  const copy = spendableCopy(
    estimate({
      guardrail: {
        active: true,
        ordinaryExpenses: true,
        nearTermObligations: false,
        on: "2026-11-01",
        shortfall: 50,
        alreadyShortWithoutExtra: false,
      },
    }),
    cash(),
    [{ freshness: "Stale" }],
    true,
    money,
    date,
  );

  assert.match(copy.guardrail, /ordinary spending/);
  assert.match(copy.guardrail, /2026-11-01/);
  assert.match(copy.guardrail, /\$50\.00 short/);
  assert.equal(copy.leadsStatus, true);
  assert.match(copy.freshness, /1 followed debt balance is not current/);
});

test("keeps an existing shortfall in the status when the extra only deepens it", () => {
  const copy = spendableCopy(
    estimate({
      guardrail: {
        active: true,
        ordinaryExpenses: false,
        nearTermObligations: true,
        on: "2026-11-01",
        shortfall: 80,
        alreadyShortWithoutExtra: true,
      },
    }),
    cash({ startingCashStaleConnectedCount: 2 }),
    [],
    true,
    money,
    date,
  );

  assert.match(copy.guardrail, /near-term bills and debt payments/);
  assert.match(copy.guardrail, /already there without the extra/);
  assert.equal(copy.leadsStatus, false);
  assert.match(copy.freshness, /2 connected cash balances are not current/);
});

test("names variable pay and unset ordinary spending as uncertainties", () => {
  const copy = spendableCopy(
    estimate({
      uncertainties: [
        { kind: "VariableIncome", name: "Side work" },
        { kind: "OrdinarySpendingUnset", name: "" },
      ],
    }),
    cash({ startingCashOldestAsOf: null, startingCashAccountCount: 0 }),
    [],
    false,
    money,
    date,
  );

  assert.deepEqual(copy.uncertainties, [
    "Side work is variable, so it is not counted.",
    "Ordinary spending is not set, so this estimate may be high.",
  ]);
  assert.equal(copy.leadsStatus, false);
  assert.match(copy.freshness, /No active cash account/);
  assert.match(copy.chartNote, /steady pay only/);
});
