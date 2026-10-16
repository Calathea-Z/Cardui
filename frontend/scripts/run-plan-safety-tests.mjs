import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

/**
 * Loads a Plan or setup module after erasing its types.
 * A local import of planReturn is rewritten to the compiled file.
 */
async function loadModule(file) {
  const source = await readFile(path.resolve(file), "utf8");
  const output = ts.transpileModule(source, {
    compilerOptions: {
      module: ts.ModuleKind.ES2022,
      target: ts.ScriptTarget.ES2022,
    },
  });
  const tempDir = path.resolve(".tmp-tests");
  await mkdir(tempDir, { recursive: true });
  const compiledPath = path.join(tempDir, `${path.basename(file, ".ts")}.mjs`);
  const text = output.outputText
    .replaceAll('from "@/features/plan/planReturn"', 'from "./planReturn.mjs"')
    .replaceAll('from "./planReturn"', 'from "./planReturn.mjs"');
  await writeFile(compiledPath, text);
  return import(pathToFileURL(compiledPath).href);
}

await loadModule("features/plan/planReturn.ts");
const safety = await loadModule("features/plan/planSafetyAction.ts");
const outlook = await loadModule("features/plan/planDebtOutlook.ts");
const setup = await loadModule("features/setup/planSetupSummary.ts");
const work = await loadModule("features/plan/planWorkState.ts");
const money = (amount) => `$${amount.toFixed(2)}`;

/**
 * A memory stand-in for sessionStorage.
 */
function memoryStore() {
  const values = new Map();
  return {
    getItem: (key) => values.get(key) ?? null,
    setItem: (key, value) => values.set(key, value),
    removeItem: (key) => values.delete(key),
  };
}

test("a shortfall opens the steady-pay review instead of typical pay", () => {
  const action = safety.planSafetyAction({
    status: "short",
    hasDebts: true,
    debtAttentionCount: 2,
    noCash: false,
    noIncome: false,
    spendingUnset: false,
  });

  assert.equal(action.label, "Review shortfall");
  assert.equal(action.view, "shortfall");
  assert.equal(action.href, undefined);
});

test("missing debt terms are the next action when the 30-day window is covered", () => {
  const action = safety.planSafetyAction({
    status: "covered",
    hasDebts: true,
    debtAttentionCount: 1,
    noCash: false,
    noIncome: false,
    spendingUnset: false,
  });

  assert.equal(action.href, "/debts?from=plan");
  assert.equal(action.label, "Fix debt terms");
});

test("debt outlook keeps payment priority apart from the earliest payoff", () => {
  const copy = outlook.planDebtOutlook(
    {
      debts: [{ name: "Card" }, { name: "Store" }],
      steps: [
        { name: "Store", endedOn: "2026-02-15", breathingRoom: 0 },
        { name: "Card", endedOn: "2026-04-20", breathingRoom: 40 },
      ],
    },
    "Rollover",
    true,
    (value) => value,
  );

  assert.match(copy.priority, /Card is first/);
  assert.equal(copy.payoff, "Store · 2026-02-15");
  assert.match(copy.availability, /rolls to the next debt/);
  assert.match(copy.conditionalNote, /assumes the cash shortfall is resolved/);
  assert.doesNotMatch(copy.payoff, /lowest/i);
});

test("setup groups keep bills and debts separate and do not invent a saved split", () => {
  const groups = setup.planSetupGroups(
    {
      currency: "USD",
      incomeAvailable: true,
      sourceCount: 2,
      sharesAvailable: true,
      contributions: [{ monthlyAmount: null }, { monthlyAmount: 500 }],
      billsAvailable: true,
      billCount: 3,
      debtsAvailable: true,
      debtCount: 1,
      livingAvailable: true,
      livingMonthly: 800,
      savingsAvailable: true,
      floorAmount: 1000,
      emergencyName: "Emergency",
      emergencyTarget: 2000,
      namedGoalCount: 0,
      allocationAvailable: true,
      allocationSaved: false,
      variableIncome: { debtPercent: 0, savingsPercent: 0, cashPercent: 100 },
      windfall: { debtPercent: 0, savingsPercent: 0, cashPercent: 100 },
      debtTermsIssue:
        "1 debt needs a term before the payoff estimate is complete.",
    },
    money,
  );

  const bills = groups.find((group) => group.key === "bills");
  assert.deepEqual(bills.lines, ["3 bills", "1 debt"]);
  assert.deepEqual(
    bills.actions.map((action) => action.href),
    ["/bills?from=setup", "/debts?from=setup"],
  );
  assert.equal(
    bills.issue,
    "1 debt needs a term before the payoff estimate is complete.",
  );
  assert.equal(
    groups.find((group) => group.key === "living").actions[0].label,
    "Edit living costs",
  );
  assert.equal(
    groups.find((group) => group.key === "protect").actions[0].label,
    "Edit money to protect",
  );
  assert.equal(
    groups.find((group) => group.key === "allocation").actions[0].label,
    "Edit extra-money preferences",
  );
  assert.equal(groups.find((group) => group.key === "income").issue, null);
  assert.equal(
    groups.find((group) => group.key === "living").lines[0],
    "Flexible monthly spending is $800.00 a month.",
  );
  assert.deepEqual(groups.find((group) => group.key === "allocation").lines, [
    "Not saved.",
  ]);
});

test("a failed setup load is not described as zero saved facts", () => {
  const groups = setup.planSetupGroups(
    {
      currency: "USD",
      incomeAvailable: false,
      sourceCount: 0,
      sharesAvailable: false,
      contributions: [],
      billsAvailable: false,
      billCount: 0,
      debtsAvailable: true,
      debtCount: 0,
      livingAvailable: false,
      livingMonthly: null,
      savingsAvailable: false,
      floorAmount: null,
      emergencyName: null,
      emergencyTarget: null,
      namedGoalCount: 0,
      allocationAvailable: false,
      allocationSaved: false,
      variableIncome: null,
      windfall: null,
      debtTermsIssue: null,
    },
    money,
  );

  assert.match(groups[0].lines[0], /Could not be read/);
  assert.match(groups[1].lines[0], /Could not be read/);
  assert.equal(groups[1].lines[1], "0 debts");
  assert.equal(groups[1].issue, null);
});

test("the setup debt warning uses Plan's debt count and stays quiet at zero", () => {
  assert.equal(setup.planSetupDebtTermsIssue(null), null);
  assert.equal(setup.planSetupDebtTermsIssue(0), null);
  assert.match(setup.planSetupDebtTermsIssue(1), /1 debt needs/);
  assert.match(setup.planSetupDebtTermsIssue(2), /2 debts need/);
});

test("the working preview round-trips and ignores a broken store value", () => {
  const store = memoryStore();
  const scenario = {
    monthlyExtra: 50,
    incomeLossMonths: 0,
    windfallAmount: 0,
    windfallOn: null,
    livingSpendingMonthly: null,
    cashFloor: null,
  };

  work.writePlanWork(store, { scenario, kind: "ReclaimAll" });
  assert.deepEqual(work.readPlanWork(store), {
    scenario: {
      ...scenario,
      priority: "Avalanche",
      constraintDebtId: null,
      constraintKind: null,
      reclaimAmount: 0,
    },
    kind: "ReclaimAll",
  });
  store.setItem("cardui.plan.work", "{");
  assert.equal(work.readPlanWork(store), null);
  work.clearPlanWork(store);
  assert.equal(work.readPlanWork(store), null);
  assert.equal(work.readPlanWork(null), null);
});
