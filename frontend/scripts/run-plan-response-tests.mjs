import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

/**
 * Loads the Plan response validator after erasing its types.
 */
async function loadValidator() {
  const source = await readFile(
    path.resolve("lib/api/validatePlanRecovery.ts"),
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
  const compiledPath = path.join(tempDir, "validatePlanRecovery.mjs");
  await writeFile(compiledPath, output.outputText);
  return import(pathToFileURL(compiledPath).href);
}

const { isCurrentPlanRecovery } = await loadValidator();

/**
 * Returns the closure fields the current Plan page requires at runtime.
 */
function currentResponse() {
  return {
    debtFacts: [],
    livingSpendingMonthly: 0,
    incomeLossMonths: 0,
    windfallAmount: 0,
    windfallOn: null,
    scenarioLivingSpendingMonthly: null,
    cashFloor: 0,
    scenarioCashFloor: null,
    hasCashFloor: false,
    hasEmergencyGoal: false,
    namedSavingsGoalCount: 0,
    cashOutlook: {
      startingCashAccountCount: 0,
      startingCashManualAccountCount: 0,
      startingCashConnectedAccountCount: 0,
      startingCashOldestAsOf: null,
      startingCashUnknownDateCount: 0,
      startingCashStaleConnectedCount: 0,
    },
    spendable: {
      rollover: estimate(),
      reclaimAll: estimate(),
    },
  };
}

/**
 * Returns one empty spendable path. The amounts are zero and the guardrail is inactive.
 */
function estimate() {
  return {
    from: "2026-10-15",
    through: "2026-11-13",
    spendable: 0,
    lowestAvailable: 0,
    lowestOn: "2026-10-15",
    reliableIncome: 0,
    obligations: 0,
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
  };
}

test("accepts the current Plan trust-fact contract", () => {
  assert.equal(isCurrentPlanRecovery(currentResponse()), true);
});

test("rejects a debt fact from before the offer comparison fields", () => {
  const oldResponse = currentResponse();
  oldResponse.debtFacts = [
    {
      debtId: "card",
      name: "Card",
      balance: 100,
    },
  ];

  assert.equal(isCurrentPlanRecovery(oldResponse), false);
});

test("accepts a debt fact that can be named on the offer comparison", () => {
  const response = currentResponse();
  response.debtFacts = [
    {
      debtId: "card",
      name: "Card",
      kind: "Revolving",
      apr: null,
      currency: "USD",
    },
  ];

  assert.equal(isCurrentPlanRecovery(response), true);
});

test("rejects a response from before debt facts were added", () => {
  const oldResponse = currentResponse();
  delete oldResponse.debtFacts;

  assert.equal(isCurrentPlanRecovery(oldResponse), false);
});

test("rejects a response without starting-cash provenance", () => {
  const oldResponse = currentResponse();
  delete oldResponse.cashOutlook.startingCashAccountCount;

  assert.equal(isCurrentPlanRecovery(oldResponse), false);
});

test("rejects a response from before spendable estimates", () => {
  const oldResponse = currentResponse();
  delete oldResponse.spendable;

  assert.equal(isCurrentPlanRecovery(oldResponse), false);
});

test("rejects a spendable uncertainty the page cannot explain", () => {
  const response = currentResponse();
  response.spendable.rollover.uncertainties = [
    { kind: "MonthlySurplus", name: "" },
  ];

  assert.equal(isCurrentPlanRecovery(response), false);
});
