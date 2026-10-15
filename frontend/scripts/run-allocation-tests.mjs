import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

/**
 * Loads one allocation module after erasing its types.
 */
async function loadModule(filename) {
  const source = await readFile(
    path.resolve("features/allocation", filename),
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
  const compiledPath = path.join(tempDir, filename.replace(/\.ts$/, ".mjs"));
  await writeFile(compiledPath, output.outputText);
  return import(pathToFileURL(compiledPath).href);
}

const rules = await loadModule("allocationRules.ts");
const copy = await loadModule("allocationCopy.ts");

test("a percent field accepts a whole number from 0 to 100", () => {
  assert.deepEqual(rules.parsePercent("0"), { ok: true, percent: 0 });
  assert.deepEqual(rules.parsePercent("100"), { ok: true, percent: 100 });
  assert.equal(rules.parsePercent("").ok, false);
  assert.equal(rules.parsePercent("50.5").ok, false);
  assert.equal(rules.parsePercent("101").ok, false);
});

test("debt and savings that pass 100 percent are not stored", () => {
  const rejected = rules.readAllocationForm({
    variableIncomeDebtPercent: "60",
    variableIncomeSavingsPercent: "50",
    windfallDebtPercent: "0",
    windfallSavingsPercent: "0",
  });

  assert.equal(rejected.ok, false);
  assert.match(rejected.error, /Variable income/);
});

test("the split keeps the amount and gives a rounded cent to debt before savings", () => {
  assert.deepEqual(rules.splitAllocation(10, 50, 50), {
    debt: 5,
    savings: 5,
    cash: 0,
  });
  assert.deepEqual(rules.splitAllocation(10, 33, 33), {
    debt: 3.3,
    savings: 3.3,
    cash: 3.4,
  });
  assert.deepEqual(rules.splitAllocation(0.01, 50, 50), {
    debt: 0.01,
    savings: 0,
    cash: 0,
  });
  assert.deepEqual(rules.splitAllocation(100, 0, 0), {
    debt: 0,
    savings: 0,
    cash: 100,
  });
  assert.equal(rules.cashPercent(40, 40), 20);
  assert.equal(rules.cashPercent(60, 50), null);
});

test("the review says the rule does not move money", () => {
  assert.match(
    copy.variableSplitSentence(
      "Snooze",
      "$200.00",
      "$80.00",
      "$80.00",
      "$40.00",
    ),
    /does not move this money/,
  );
  assert.match(copy.windfallSentence(40, 40, 20), /still stays in cash/);
  assert.match(
    copy.leftOutSentence("Side job", "Another currency"),
    /another currency/i,
  );
  assert.match(copy.allocationStatusSentence(false), /No rule is saved yet/);
  assert.match(copy.allocationStatusSentence(true), /does not move money/);
});
