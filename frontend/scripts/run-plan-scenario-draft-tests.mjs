import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

/**
 * Loads one Plan module after erasing its types.
 * Money parsing is rewritten to the local reader.
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
  await writeFile(
    compiledPath,
    output.outputText
      .replaceAll(
        'from "@/features/accounts/formatCurrency"',
        'from "./formatCurrency.mjs"',
      )
      .replaceAll('from "./planExtra"', 'from "./planExtra.mjs"'),
  );
  return import(pathToFileURL(compiledPath).href);
}

/**
 * Writes the shared money reader next to a transpiled test module.
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

await loadModule("planExtra");
const scenario = await loadModule("planScenario");
const comparison = await loadModule("planScenarioComparison");
const money = (amount) => `$${amount.toFixed(2)}`;
const date = (value) => value;

/**
 * A recovery report with only the fields the comparison reads.
 */
function report(overrides = {}) {
  const spendable = {
    from: "2026-01-01",
    through: "2026-01-30",
    spendable: 400,
    lowestAvailable: 400,
    alreadyShort: false,
    uncertainties: [],
    ...(overrides.spendable ?? {}),
  };
  const rest = { ...overrides };
  delete rest.spendable;
  delete rest.rollover;
  delete rest.cashOutlook;
  return {
    planningCurrency: "USD",
    hasDebts: true,
    incomeLossMonths: 0,
    windfallAmount: 0,
    cashOutlook: {
      asOf: "2026-01-01",
      startingCash: 1000,
      startingCashAccountCount: 1,
      ...(overrides.cashOutlook ?? {}),
    },
    spendable: {
      rollover: spendable,
      reclaimAll: spendable,
    },
    rollover: {
      paidOffOn: "2027-04-15",
      totalInterest: 80,
      debts: [{ stop: "PaidOff" }],
      ...(overrides.rollover ?? {}),
    },
    reclaimAll: {
      paidOffOn: "2027-04-15",
      totalInterest: 80,
      debts: [{ stop: "PaidOff" }],
    },
    ...rest,
  };
}

test("update preview reads every valid field together", () => {
  const draft = {
    ...scenario.currentFactsDraft(),
    monthlyExtra: "25",
    incomeLossMonths: "2",
    windfallAmount: "250",
    windfallOn: "2026-02-01",
    livingChoice: "amount",
    livingAmount: "0",
    floorChoice: "amount",
    floorAmount: "500",
    kind: "ReclaimAll",
  };
  const read = scenario.readScenarioDraft(draft, "2026-01-01", 800);
  assert.equal(read.ok, true);
  assert.equal(read.kind, "ReclaimAll");
  assert.deepEqual(read.scenario, {
    monthlyExtra: 25,
    incomeLossMonths: 2,
    windfallAmount: 250,
    windfallOn: "2026-02-01",
    livingSpendingMonthly: 0,
    cashFloor: 500,
  });
});

test("one invalid field blocks the whole update and keeps the draft unmatched", () => {
  const draft = {
    ...scenario.currentFactsDraft(),
    monthlyExtra: "25",
    incomeLossMonths: "nope",
  };
  const read = scenario.readScenarioDraft(draft, "2026-01-01", 800);
  assert.equal(read.ok, false);
  assert.equal(read.errors.incomeLossMonths, "months");
  assert.equal(read.scenario, undefined);
  assert.equal(
    scenario.canSaveWorkingScenario({
      status: "ready",
      draft,
      applied: scenario.currentFactsScenario(),
      kind: "Rollover",
      asOf: "2026-01-01",
      savedLiving: 800,
    }),
    false,
  );
});

test("a failed update cannot be saved even when the draft matches the previous result", () => {
  const applied = scenario.currentFactsScenario();
  const draft = scenario.draftFromApplied(applied, "Rollover");
  assert.equal(
    scenario.canSaveWorkingScenario({
      status: "error",
      draft,
      applied,
      kind: "Rollover",
      asOf: "2026-01-01",
      savedLiving: 800,
    }),
    false,
  );
  assert.equal(
    scenario.draftMatchesApplied(draft, applied, "Rollover", "2026-01-01", 800),
    true,
  );
});

test("reset restores current facts, including use-saved spending", () => {
  const facts = scenario.currentFactsDraft();
  const read = scenario.readScenarioDraft(facts, "2026-01-01", 800);
  assert.equal(read.ok, true);
  assert.equal(read.scenario.livingSpendingMonthly, null);
  assert.equal(read.scenario.cashFloor, null);
  assert.equal(read.scenario.monthlyExtra, 0);
  assert.equal(scenario.isCurrentFacts(read.scenario, read.kind), true);
});

test("comparison uses the same steady-pay safety and leaves unknown payoff unknown", () => {
  const baseline = report();
  const trying = report({
    spendable: {
      spendable: 120,
      lowestAvailable: -40,
      alreadyShort: true,
    },
    rollover: {
      paidOffOn: null,
      totalInterest: 12,
      debts: [{ stop: "DoesNotPayDown" }],
    },
  });
  const compared = comparison.planScenarioComparison(
    baseline,
    trying,
    "Rollover",
    money,
    date,
  );
  assert.equal(compared.rows[0].current.text, "$400.00");
  assert.equal(compared.rows[0].trying.text, "Shortfall $40.00");
  assert.equal(compared.rows[0].comparable, true);
  assert.equal(compared.rows[1].trying.text, "Unknown");
  assert.equal(compared.rows[2].trying.text, "Unknown");
  assert.equal(compared.rows[1].comparable, false);
  assert.match(
    comparison.scenarioComparisonNote(compared),
    /Same starting cash/,
  );
});

test("a different income basis and a windfall are not presented as the same safety", () => {
  const baseline = report();
  const trying = report({
    incomeLossMonths: 2,
    windfallAmount: 250,
  });
  const compared = comparison.planScenarioComparison(
    baseline,
    trying,
    "Rollover",
    money,
    date,
  );
  assert.equal(compared.sameIncomeBasis, false);
  assert.equal(compared.rows[0].comparable, false);
  assert.match(compared.windfallNote, /not included in steady-pay/);
  assert.match(
    comparison.scenarioComparisonNote(compared),
    /not a direct comparison/,
  );
});

test("missing steady pay and a household with no debts stay honest", () => {
  const baseline = report({
    hasDebts: false,
    spendable: {
      uncertainties: [{ kind: "NoReliableIncome", name: "" }],
    },
  });
  const compared = comparison.planScenarioComparison(
    baseline,
    baseline,
    "Rollover",
    money,
    date,
  );
  assert.equal(compared.rows[0].current.text, "Unknown");
  assert.equal(compared.rows[1].current.text, "No debts");
  assert.equal(compared.rows[2].current.text, "No debts");
});
