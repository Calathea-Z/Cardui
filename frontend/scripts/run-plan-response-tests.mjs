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
  };
}

test("accepts the current Plan trust-fact contract", () => {
  assert.equal(isCurrentPlanRecovery(currentResponse()), true);
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
