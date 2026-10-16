import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

/**
 * Loads one Plan module after erasing its types.
 * These modules have no runtime imports.
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
  const compiledPath = path.join(tempDir, `${file}.mjs`);
  await writeFile(compiledPath, output.outputText);
  return import(pathToFileURL(compiledPath).href);
}

const money = (amount) => `$${amount.toFixed(2)}`;
const date = (value) => value;

const copy = await loadModule("decisionPlanCopy");
const view = await loadModule("decisionPlanView");

/**
 * A small decision plan with one ranked action and one debt.
 */
function plan(action, kind = "Rollover") {
  return {
    calculationVersion: "1",
    asOf: "2026-10-15",
    planningCurrency: "USD",
    kind,
    facts: {
      startingCash: 5000,
      startingReserve: 1000,
      startingAvailable: 4000,
      startingCashAccountCount: 1,
      startingCashOldestAsOf: "2026-10-14",
      staleCashCount: 0,
      hasIncome: true,
      hasBills: true,
      livingSpendingMonthly: 800,
      cashFloor: 1000,
      hasEmergencyGoal: true,
      namedSavingsGoalCount: 0,
      staleDebtCount: 0,
      paymentReviewCount: 0,
      spendable: 500,
      alreadyShort: false,
      guardrail: {
        active: false,
        ordinaryExpenses: false,
        nearTermObligations: false,
        on: null,
        shortfall: 0,
        alreadyShortWithoutExtra: false,
      },
      shortfallOn: null,
      recoveredOn: null,
      reserveShortfallOn: null,
      startingObligation: 50,
      recurringRoom: 50,
      paidOffOn: "2027-04-15",
      totalInterest: 100,
    },
    scenario: {
      monthlyExtra: 0,
      incomeLossMonths: 0,
      windfallAmount: 0,
      windfallOn: null,
      livingSpendingMonthly: null,
      cashFloor: null,
    },
    debts: [
      {
        debtId: "card",
        name: "Card",
        balance: 2000,
        minimum: 50,
        apr: 19.99,
        stop: "PaidOff",
        paidOffOn: "2027-04-15",
      },
    ],
    steps: [],
    missingBalanceNames: [],
    excludedCurrencies: [],
    actions: [action],
    milestones: [],
    assumptions: [],
    risks: [],
    missing: [],
    reviewConditions: [],
  };
}

test("a cash shortfall is the action and names its date", () => {
  const text = copy.decisionActionCopy(
    {
      order: 1,
      kind: "CoverCashShortfall",
      name: null,
      nextName: null,
      amount: null,
      extra: null,
      on: "2026-11-03",
      count: null,
      alreadyShortWithoutExtra: false,
    },
    money,
    date,
  );

  assert.equal(text.title, "Cover the cash shortfall");
  assert.match(text.effect, /2026-11-03/);
  assert.match(text.why, /conditional/);

  const target = view.decisionActionTarget("CoverCashShortfall");
  assert.equal(target.label, "Review shortfall");
  assert.equal(target.view, "shortfall");
});

test("a stored shortfall uses the captured amount, and an older version does not invent one", () => {
  const stored = plan({
    order: 1,
    kind: "CoverCashShortfall",
    name: null,
    nextName: null,
    amount: null,
    extra: null,
    on: "2026-11-03",
    count: null,
    alreadyShortWithoutExtra: false,
  });
  stored.facts.alreadyShort = true;
  stored.facts.spendable = 0;
  stored.facts.lowestAvailable = -80;
  stored.facts.lowestAvailableOn = "2026-10-20";
  const captured = copy
    .decisionFactLines(stored, money, date)
    .find((line) => line.term === "Shortfall");
  assert.match(captured.detail, /\$80\.00 on 2026-10-20/);
  assert.equal(
    copy
      .decisionFactLines(stored, money, date)
      .some((line) => line.term === "Spendable"),
    false,
  );

  const older = plan({
    order: 1,
    kind: "CoverCashShortfall",
    name: null,
    nextName: null,
    amount: null,
    extra: null,
    on: "2026-11-03",
    count: null,
    alreadyShortWithoutExtra: false,
  });
  older.facts.alreadyShort = true;
  older.facts.spendable = 0;
  older.facts.lowestAvailable = null;
  older.facts.lowestAvailableOn = null;
  const missing = copy
    .decisionFactLines(older, money, date)
    .find((line) => line.term === "Shortfall");
  assert.match(missing.detail, /not stored/);
  assert.doesNotMatch(missing.detail, /\$0\.00/);
  assert.doesNotMatch(missing.detail, /2026-11-03/);
});

test("the first debt names its payment and a rolled payment names the next debt", () => {
  const pay = copy.decisionActionCopy(
    {
      order: 1,
      kind: "PayFirstDebt",
      name: "Card",
      nextName: null,
      amount: 50,
      extra: 25,
      on: "2027-04-15",
      count: null,
      alreadyShortWithoutExtra: false,
    },
    money,
    date,
  );
  assert.equal(pay.title, "Pay Card first");
  assert.match(pay.effect, /\$50\.00/);
  assert.match(pay.effect, /\$25\.00/);

  const rolled = copy.decisionActionCopy(
    {
      order: 2,
      kind: "UseFreedPayment",
      name: "Card",
      nextName: "Loan",
      amount: 50,
      extra: 50,
      on: "2027-04-15",
      count: null,
      alreadyShortWithoutExtra: false,
    },
    money,
    date,
  );
  assert.equal(rolled.title, "Send the freed payment to Loan");
  assert.match(rolled.effect, /goes to Loan/);

  const kept = copy.decisionActionCopy(
    {
      order: 2,
      kind: "UseFreedPayment",
      name: "Card",
      nextName: null,
      amount: 50,
      extra: 50,
      on: "2027-04-15",
      count: null,
      alreadyShortWithoutExtra: false,
    },
    money,
    date,
  );
  assert.equal(kept.title, "Keep the freed payment");
  assert.match(kept.effect, /not sent to another debt/);
});

test("an extra that is already short says lowering it may not close the gap", () => {
  const text = copy.decisionActionCopy(
    {
      order: 1,
      kind: "LowerExtra",
      name: null,
      nextName: null,
      amount: 40,
      extra: 200,
      on: "2026-10-28",
      count: 3,
      alreadyShortWithoutExtra: true,
    },
    money,
    date,
  );

  assert.match(text.why, /ordinary spending and a near-term bill/);
  assert.match(text.why, /already short without the extra/);
  assert.match(text.effect, /\$40\.00/);
});

test("an unsaved preview offers save, and a matching draft offers accept", () => {
  const preview = plan({
    order: 1,
    kind: "PayFirstDebt",
    name: "Card",
    nextName: null,
    amount: 50,
    extra: null,
    on: "2027-04-15",
    count: null,
    alreadyShortWithoutExtra: false,
  });
  const draft = {
    id: "draft",
    number: 2,
    status: "Draft",
    createdAt: "2026-10-15T00:00:00Z",
    acceptedAt: null,
    supersededAt: null,
    plan: preview,
  };

  const unsaved = view.decisionPlanChrome(preview, [], null, true);
  assert.equal(unsaved.label, "Current facts");
  assert.equal(unsaved.action, "save");
  assert.equal(unsaved.replacesDraftId, null);

  const trying = view.decisionPlanChrome(preview, [], null, false);
  assert.equal(trying.label, "Trying changes — not saved");
  assert.equal(trying.action, "save");

  const open = view.decisionPlanChrome(preview, [draft], null, true);
  assert.equal(open.label, "Saved draft");
  assert.equal(open.action, "accept");
  assert.equal(open.acceptId, "draft");
  assert.match(open.note, /replaces the accepted plan/);
  assert.match(open.note, /history/);

  const changed = view.decisionPlanChrome(
    { ...preview, asOf: "2026-10-16" },
    [draft],
    null,
  );
  assert.equal(changed.action, "save");
  assert.equal(changed.replacesDraftId, "draft");
  assert.match(changed.note, /replaces the open draft/);
});

test("an accepted match prints, and a superseded version goes back to the preview", () => {
  const preview = plan({
    order: 1,
    kind: "AddDebt",
    name: null,
    nextName: null,
    amount: null,
    extra: null,
    on: null,
    count: null,
    alreadyShortWithoutExtra: false,
  });
  const accepted = {
    id: "accepted",
    number: 1,
    status: "Accepted",
    createdAt: "2026-10-15T00:00:00Z",
    acceptedAt: "2026-10-15T00:00:00Z",
    supersededAt: null,
    plan: preview,
  };
  const old = { ...accepted, id: "old", number: 0, status: "Superseded" };

  const current = view.decisionPlanChrome(preview, [accepted], null, true);
  assert.equal(current.label, "Accepted plan");
  assert.equal(current.action, "print");

  const history = view.decisionPlanChrome(preview, [accepted, old], "old");
  assert.equal(history.label, "History");
  assert.equal(history.action, "back");
  assert.match(history.note, /earlier version/);

  const printed = view.planVersionPrintLines(history, date);
  assert.equal(printed.status, "History");
  assert.equal(printed.version, "Version 0");
  assert.equal(printed.date, "As of 2026-10-15");
  assert.equal(printed.recorded, "Recorded 2026-10-15");

  const working = view.planVersionPrintLines(
    view.decisionPlanChrome(null, [], null, false),
    date,
  );
  assert.equal(working.status, "Trying changes — not saved");
  assert.equal(working.version, "Working preview");
  assert.equal(working.recorded, null);
});
