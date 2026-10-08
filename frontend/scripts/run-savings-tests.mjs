import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

/**
 * Loads the savings copy module after erasing its types.
 * The module does not import the app at runtime.
 */
async function loadModule() {
  const source = await readFile(
    path.resolve("features/savings/savingsCopy.ts"),
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
  const compiledPath = path.join(tempDir, "savingsCopy.mjs");
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

const copy = await loadModule();
const money = (amount) => `$${amount.toFixed(2)}`;

test("a blank amount set aside is zero and a blank target is not", () => {
  assert.equal(copy.reservedFromField(""), 0);
  assert.equal(copy.reservedFromField("  10.005 "), 10.01);
  assert.equal(copy.reservedFromField("-1"), null);
  assert.equal(copy.reservedFromField("nope"), null);
  assert.equal(copy.targetFromField(""), null);
  assert.equal(copy.targetFromField("0"), null);
  assert.equal(copy.targetFromField("25"), 25);
  assert.equal(copy.targetFromField("30,000"), 30000);
  assert.equal(copy.targetFromField("$30,000.50"), 30000.5);
  assert.equal(copy.targetFromField("30,00"), null);
});

test("the progress sentence names a funded goal, a past date, and the last month", () => {
  assert.equal(
    copy.goalProgress(
      {
        alreadyMet: true,
        datePassed: false,
        beyondHorizon: false,
        remaining: 0,
        amountNeededPerMonth: null,
        finalAmountNeeded: null,
      },
      money,
    ),
    "This is funded.",
  );
  assert.equal(
    copy.goalProgress(
      {
        alreadyMet: false,
        datePassed: true,
        beyondHorizon: false,
        remaining: 40,
        amountNeededPerMonth: null,
        finalAmountNeeded: null,
      },
      money,
    ),
    "$40.00 is due now.",
  );
  assert.equal(
    copy.goalProgress(
      {
        alreadyMet: false,
        datePassed: false,
        beyondHorizon: false,
        remaining: 100,
        amountNeededPerMonth: 33.33,
        finalAmountNeeded: 33.34,
      },
      money,
    ),
    "Set aside $33.33 a month. The last month is $33.34.",
  );
});

test("a savings payload rejects a missing name and an account that is not offered", () => {
  const form = {
    kind: "Sinking",
    name: "",
    targetAmount: "100",
    targetDate: "2026-12-15",
    reservedAmount: "",
    accountId: "",
    useAccountBalance: false,
  };
  assert.equal(copy.toSavingsPayload(form, true).ok, false);

  const ready = copy.toSavingsPayload(
    { ...form, name: "Tires", reservedAmount: "" },
    true,
  );
  assert.equal(ready.ok, true);
  assert.equal(ready.dto.reservedAmount, 0);
  assert.equal(ready.dto.accountId, null);

  const blocked = copy.toSavingsPayload(
    { ...form, name: "Tires", accountId: "gone" },
    false,
  );
  assert.equal(blocked.ok, false);

  const comma = copy.toSavingsPayload(
    { ...form, name: "Tires", targetAmount: "30,44" },
    true,
  );
  assert.equal(comma.ok, false);
  assert.match(comma.error, /30\.44/);
  assert.match(comma.error, /30,000/);
});

test("the follow note names an override, a negative balance, and a broken link", () => {
  assert.equal(
    copy.followNote({
      following: false,
      reservedOverridden: false,
      accountUnavailable: false,
      negativeBalance: false,
      accountName: null,
    }),
    null,
  );
  assert.match(
    copy.followNote({
      following: true,
      reservedOverridden: true,
      accountUnavailable: false,
      negativeBalance: false,
      accountName: "Savings",
    }),
    /your amount/i,
  );
  assert.match(
    copy.followNote({
      following: true,
      reservedOverridden: false,
      accountUnavailable: false,
      negativeBalance: true,
      accountName: "Savings",
    }),
    /below zero/,
  );
  assert.match(
    copy.followNote({
      following: false,
      reservedOverridden: false,
      accountUnavailable: true,
      negativeBalance: false,
      accountName: "Savings",
    }),
    /no longer be followed/,
  );
});
