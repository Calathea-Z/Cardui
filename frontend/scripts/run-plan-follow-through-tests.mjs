import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

/**
 * Loads a Plan module after erasing its types.
 * The planReturn import is rewritten to the compiled file.
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
  const text = output.outputText.replaceAll(
    'from "./planReturn"',
    'from "./planReturn.mjs"',
  );
  await writeFile(compiledPath, text);
  return import(pathToFileURL(compiledPath).href);
}

await loadModule("features/plan/planReturn.ts");
const follow = await loadModule("features/plan/planHomeFollowThrough.ts");

/**
 * One ranked action with empty optional fields.
 */
function action(order, kind) {
  return {
    order,
    kind,
    name: null,
    nextName: null,
    amount: null,
    extra: null,
    on: null,
    count: null,
    alreadyShortWithoutExtra: false,
  };
}

/**
 * One stored detail.
 */
function detail(kind, name = null) {
  return { kind, name, amount: null, on: null, count: null };
}

/**
 * The fields the follow-through rule reads.
 */
function plan(overrides = {}) {
  return {
    actions: [],
    milestones: [],
    missing: [],
    reviewConditions: [],
    risks: [],
    assumptions: [detail("Order"), detail("Payments")],
    ...overrides,
  };
}

test("the 30-day shortfall stays out of the ranked list and leads it", () => {
  const result = follow.planFollowThrough({
    plan: plan({
      actions: [
        action(1, "CoverCashShortfall"),
        action(2, "PayFirstDebt"),
        action(3, "UseFreedPayment"),
      ],
      milestones: [
        { kind: "CashShortfall", name: null, amount: null, on: "2026-10-20" },
      ],
    }),
    safetyKey: "shortfall",
    safetyLabel: "Review shortfall",
    reserveShort: false,
  });

  assert.deepEqual(
    result.actions.map((item) => item.kind),
    ["PayFirstDebt", "UseFreedPayment"],
  );
  assert.equal(result.actions[0].order, 2);
  assert.equal(result.safetyLeads, true);
  assert.equal(result.milestones[0].on, "2026-10-20");
  assert.equal(result.payoffDatesConditional, false);
});

test("trying a change keeps the extra-payment action", () => {
  const result = follow.planFollowThrough({
    plan: plan({ actions: [action(1, "LowerExtra")] }),
    safetyKey: "changes",
    safetyLabel: "Try a change",
    reserveShort: false,
  });

  assert.deepEqual(
    result.actions.map((item) => item.kind),
    ["LowerExtra"],
  );
  assert.equal(result.safetyLeads, false);
});

test("add a debt and fix debt terms cover different actions", () => {
  const actions = [action(1, "AddDebt"), action(2, "FinishDebtTerms")];
  const addDebt = follow.planFollowThrough({
    plan: plan({ actions }),
    safetyKey: "debts",
    safetyLabel: "Add a debt",
    reserveShort: false,
  });
  const fixTerms = follow.planFollowThrough({
    plan: plan({ actions }),
    safetyKey: "debts",
    safetyLabel: "Fix debt terms",
    reserveShort: false,
  });

  assert.deepEqual(
    addDebt.actions.map((item) => item.kind),
    ["FinishDebtTerms"],
  );
  assert.ok(addDebt.hiddenIssueKeys.includes("debts"));
  assert.ok(fixTerms.hiddenIssueKeys.includes("debts"));
  assert.deepEqual(
    fixTerms.actions.map((item) => item.kind),
    ["AddDebt"],
  );
});

test("unset bills, spending, and cash to keep are the assumptions to confirm", () => {
  const result = follow.planFollowThrough({
    plan: plan({
      missing: [
        detail("UnknownRate", "Card"),
        detail("NoBills"),
        detail("NoFlexibleSpending"),
        detail("NoCashFloor"),
        detail("VariableIncomeLeftOut", "Tips"),
      ],
      reviewConditions: [detail("EmergencyChoice"), detail("NoBills")],
    }),
    safetyKey: "changes",
    safetyLabel: "Try a change",
    reserveShort: false,
  });

  assert.deepEqual(
    result.confirmations.map((item) => item.kind),
    ["NoBills", "NoFlexibleSpending", "NoCashFloor"],
  );
  assert.equal(result.confirmations[0].href, "/bills?from=plan");
  assert.equal(result.confirmations[0].label, "Review Bills");
  assert.equal(result.confirmations[1].href, "/living?from=plan");
  assert.equal(result.confirmations[2].href, "/savings?from=plan");
  assert.deepEqual(result.hiddenIssueKeys, ["bills", "budget", "savings"]);
});

test("a listed action replaces the matching assumption", () => {
  const result = follow.planFollowThrough({
    plan: plan({
      actions: [action(1, "ConfirmBills"), action(2, "AddIncome")],
      missing: [detail("NoBills"), detail("NoIncome")],
    }),
    safetyKey: "changes",
    safetyLabel: "Try a change",
    reserveShort: false,
  });

  assert.deepEqual(
    result.confirmations.map((item) => item.kind),
    [],
  );
  assert.deepEqual(result.hiddenIssueKeys, ["income", "bills"]);
});

test("the living-costs action hides flexible spending from both lists", () => {
  const result = follow.planFollowThrough({
    plan: plan({
      actions: [action(1, "SetFlexibleSpending")],
      missing: [detail("NoFlexibleSpending")],
    }),
    safetyKey: "budget",
    safetyLabel: "Set living costs",
    reserveShort: false,
  });

  assert.equal(result.actions.length, 0);
  assert.equal(result.confirmations.length, 0);
  assert.deepEqual(result.hiddenIssueKeys, []);
});

test("a protected-savings shortfall keeps that issue beside an unset cash floor", () => {
  const result = follow.planFollowThrough({
    plan: plan({ missing: [detail("NoCashFloor")] }),
    safetyKey: "changes",
    safetyLabel: "Try a change",
    reserveShort: true,
  });

  assert.equal(result.confirmations[0].kind, "NoCashFloor");
  assert.deepEqual(result.hiddenIssueKeys, []);
});

test("a payoff milestone stays conditional only when the plan says so", () => {
  const dated = {
    kind: "FirstPayoff",
    name: "Card",
    amount: 40,
    on: "2027-01-15",
  };
  const conditional = follow.planFollowThrough({
    plan: plan({
      milestones: [dated],
      risks: [detail("PayoffConditional")],
    }),
    safetyKey: "changes",
    safetyLabel: "Try a change",
    reserveShort: false,
  });
  const quiet = follow.planFollowThrough({
    plan: plan({
      milestones: [
        { kind: "CashRecovered", name: null, amount: null, on: null },
      ],
      risks: [detail("PayoffConditional")],
    }),
    safetyKey: "changes",
    safetyLabel: "Try a change",
    reserveShort: false,
  });

  assert.equal(conditional.payoffDatesConditional, true);
  assert.equal(conditional.milestones[0].amount, 40);
  assert.equal(quiet.payoffDatesConditional, false);
  assert.equal(quiet.milestones[0].on, null);
});

test("a preview that has not loaded adds nothing", () => {
  const result = follow.planFollowThrough({
    plan: null,
    safetyKey: "shortfall",
    safetyLabel: "Review shortfall",
    reserveShort: true,
  });

  assert.deepEqual(result.actions, []);
  assert.deepEqual(result.confirmations, []);
  assert.deepEqual(result.milestones, []);
  assert.equal(result.payoffDatesConditional, false);
  assert.equal(result.safetyLeads, false);
  assert.deepEqual(result.hiddenIssueKeys, []);
});
