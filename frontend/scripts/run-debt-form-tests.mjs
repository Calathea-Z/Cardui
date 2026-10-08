import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

/**
 * Loads one TypeScript module after erasing its types.
 * The debt form and display modules have no runtime imports.
 */
async function loadModule(sourcePath, compiledName) {
  const source = await readFile(path.resolve(sourcePath), "utf8");
  const output = ts.transpileModule(source, {
    compilerOptions: {
      module: ts.ModuleKind.ES2022,
      target: ts.ScriptTarget.ES2022,
    },
  });
  const tempDir = path.resolve(".tmp-tests");
  await mkdir(tempDir, { recursive: true });
  await writeMoneyDigits(tempDir);
  const compiledPath = path.join(tempDir, compiledName);
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

const form = await loadModule(
  "features/debts/debtFormState.ts",
  "debtFormState.mjs",
);
const display = await loadModule(
  "features/debts/debtDisplay.ts",
  "debtDisplay.mjs",
);

test("a blank debt keeps every term unknown", () => {
  const result = form.toDebtUpsert({
    ...form.emptyDebtForm(),
    name: " Store card ",
    kind: "Revolving",
  });

  assert.equal(result.ok, true);
  assert.deepEqual(result.dto, {
    name: "Store card",
    kind: "Revolving",
    accountId: null,
    balance: null,
    balanceAsOf: null,
    apr: null,
    minimumPayment: null,
    nextDueDate: null,
    creditLimit: null,
    remainingTermMonths: null,
    promotionalApr: null,
    promotionalEndsOn: null,
  });
});

test("a known zero stays zero and a balance needs its date", () => {
  const dated = form.toDebtUpsert({
    ...form.emptyDebtForm(),
    name: "Card",
    kind: "Revolving",
    balance: "0",
    balanceAsOf: "2026-10-01",
    apr: "0",
    minimumPayment: "0",
    creditLimit: "1000",
    promotionalApr: "0",
  });

  assert.equal(dated.ok, true);
  assert.equal(dated.dto.balance, 0);
  assert.equal(dated.dto.balanceAsOf, "2026-10-01");
  assert.equal(dated.dto.apr, 0);
  assert.equal(dated.dto.minimumPayment, 0);
  assert.equal(dated.dto.creditLimit, 1000);
  assert.equal(dated.dto.promotionalApr, 0);
  assert.equal(dated.dto.promotionalEndsOn, null);

  const undated = form.toDebtUpsert({
    ...form.emptyDebtForm(),
    name: "Card",
    kind: "Revolving",
    balance: "10",
  });
  assert.deepEqual(undated, {
    ok: false,
    error: "Enter the date this balance was true.",
  });
});

test("a credit limit stays on a revolving debt and months stay on an installment", () => {
  const card = form.toDebtUpsert({
    ...form.emptyDebtForm(),
    name: "Card",
    kind: "Revolving",
    creditLimit: "1000",
    remainingTermMonths: "36",
  });
  assert.equal(card.ok, true);
  assert.equal(card.dto.creditLimit, 1000);
  assert.equal(card.dto.remainingTermMonths, null);

  const loan = form.toDebtUpsert({
    ...form.emptyDebtForm(),
    name: "Car loan",
    kind: "Installment",
    creditLimit: "1000",
    remainingTermMonths: "36",
  });
  assert.equal(loan.ok, true);
  assert.equal(loan.dto.creditLimit, null);
  assert.equal(loan.dto.remainingTermMonths, 36);
});

test("a stored zero comes back as zero and a missing term comes back blank", () => {
  const filled = form.debtToForm({
    id: "debt",
    name: "Card",
    kind: "Revolving",
    accountId: null,
    accountName: null,
    balance: 0,
    balanceAsOf: "2026-10-01",
    currency: "USD",
    apr: null,
    minimumPayment: 0,
    nextDueDate: null,
    creditLimit: null,
    remainingTermMonths: null,
    promotionalApr: null,
    promotionalEndsOn: null,
    utilization: null,
  });

  assert.equal(filled.balance, "0");
  assert.equal(filled.minimumPayment, "0");
  assert.equal(filled.apr, "");
  assert.equal(filled.balanceAsOf, "2026-10-01");
});

test("a followed credit limit shows the one in use, and an override needs an amount", () => {
  const followed = form.debtToForm({
    name: "Card",
    kind: "Revolving",
    accountId: null,
    following: true,
    balance: 1,
    balanceInUse: 2,
    balanceAsOf: null,
    balanceInUseAsOf: null,
    creditLimit: 1000,
    creditLimitInUse: 5000,
    creditLimitSource: "Synced",
    apr: null,
    minimumPayment: null,
    nextDueDate: null,
    remainingTermMonths: null,
    promotionalApr: null,
    promotionalEndsOn: null,
  });
  assert.equal(followed.balance, "2");
  assert.equal(followed.creditLimit, "5000");

  const manual = form.debtToForm({
    name: "Card",
    kind: "Revolving",
    accountId: null,
    following: true,
    balance: null,
    balanceInUse: null,
    balanceAsOf: null,
    balanceInUseAsOf: null,
    creditLimit: 1000,
    creditLimitInUse: 1000,
    creditLimitSource: "Manual",
    apr: null,
    minimumPayment: null,
    nextDueDate: null,
    remainingTermMonths: null,
    promotionalApr: null,
    promotionalEndsOn: null,
  });
  assert.equal(manual.creditLimit, "1000");

  const saved = form.toCreditLimitOverride("2500.5");
  assert.equal(saved.ok, true);
  assert.deepEqual(saved.dto, { creditLimit: 2500.5 });
  assert.deepEqual(form.toCreditLimitOverride(""), {
    ok: false,
    error: "Enter the credit limit.",
  });
  assert.deepEqual(form.toCreditLimitOverride("0"), {
    ok: false,
    error: "Enter a credit limit above zero.",
  });
});

test("a balance override needs an amount, and update balance omits the date", () => {
  const dated = form.toBalanceOverride("10.50", "2026-10-02");
  assert.equal(dated.ok, true);
  assert.deepEqual(dated.dto, { balance: 10.5, balanceAsOf: "2026-10-02" });

  const today = form.toTodayBalanceOverride("0");
  assert.equal(today.ok, true);
  assert.deepEqual(today.dto, { balance: 0, balanceAsOf: null });
  assert.deepEqual(form.toTodayBalanceOverride("30,000").dto, {
    balance: 30000,
    balanceAsOf: null,
  });
  assert.match(form.toTodayBalanceOverride("30,44").error, /30\.44/);

  assert.deepEqual(form.toBalanceOverride("", "2026-10-02"), {
    ok: false,
    error: "Enter the balance.",
  });
  assert.deepEqual(form.toBalanceOverride("10", ""), {
    ok: false,
    error: "Enter the date this balance was true.",
  });
});

test("update balance is offered when the connection is not current", async () => {
  const follow = await loadModule(
    "features/debts/debtFollowCopy.ts",
    "debtFollowCopy.mjs",
  );
  assert.equal(follow.canUpdateBalance("Stale"), true);
  assert.equal(follow.canUpdateBalance("SyncFailing"), true);
  assert.equal(follow.canUpdateBalance("Disconnected"), true);
  assert.equal(follow.canUpdateBalance("Current"), false);
  assert.equal(follow.canUpdateBalance("AccountMissing"), false);
  assert.equal(follow.repairConnectionId("SyncFailing", "item-1"), "item-1");
  assert.equal(follow.repairConnectionId("SyncFailing", null), null);
  assert.equal(follow.repairConnectionId("Disconnected", "item-1"), null);
  assert.equal(follow.repairConnectionId("Stale", "item-1"), null);
  assert.equal(
    follow.followConfirmLead("Store card", "Visa ending 4821", true),
    "Store card will follow Visa ending 4821. The balance and credit limit follow that account. APR, minimum, and due date stay yours.",
  );
  assert.equal(
    follow.followConfirmLead("Car loan", "Auto loan", false),
    "Car loan will follow Auto loan. The balance follows that account. APR, minimum, and due date stay yours.",
  );
  assert.equal(
    follow.creditLimitSourceText(
      "Synced",
      "2026-10-05",
      null,
      (value) => value,
    ),
    "Synced · 2026-10-05",
  );
  assert.equal(
    follow.creditLimitSourceText(
      "Override",
      null,
      "2026-10-03",
      (value) => value,
    ),
    "Your value since 2026-10-03",
  );
  assert.equal(
    follow.creditLimitSourceText("Manual", null, null, (value) => value),
    null,
  );

  const money = (amount) => `$${amount.toFixed(2)}`;
  assert.equal(
    follow.matchReasonText(
      { kind: "Mask", mask: "4821", words: [], difference: null },
      "USD",
      money,
    ),
    "Ends in 4821",
  );
  assert.equal(
    follow.matchReasonText(
      {
        kind: "Name",
        mask: null,
        words: ["Chase", "Sapphire", "Freedom"],
        difference: null,
      },
      "USD",
      money,
    ),
    "Name includes Chase, Sapphire, and Freedom",
  );
  assert.equal(
    follow.matchReasonText(
      { kind: "Balance", mask: null, words: [], difference: 38 },
      "USD",
      money,
    ),
    "Balance within $38.00 of yours",
  );
  assert.equal(
    follow.matchReasonText(
      { kind: "Balance", mask: null, words: [], difference: 0 },
      "USD",
      money,
    ),
    "Same balance as yours",
  );
  assert.equal(
    follow.suggestionLabel("Visa", "4821", ["Balance within $38.00 of yours"]),
    "Visa ending 4821, Balance within $38.00 of yours",
  );
});

test("suggestions come before the full list, and a linked account skips them", async () => {
  const steps = await loadModule(
    "features/debts/debtFollowSteps.ts",
    "debtFollowSteps.mjs",
  );
  const suggested = { suggestionOrder: 1 };
  const other = { suggestionOrder: null };
  assert.deepEqual(steps.suggestedAccounts([other, suggested]), [suggested]);

  const open = {
    linked: false,
    picked: false,
    listRequested: false,
    suggestionCount: 2,
  };
  assert.equal(steps.followPanel(open), "suggestions");
  assert.equal(steps.followBackTarget(open), "close");
  assert.equal(steps.followPanel({ ...open, listRequested: true }), "accounts");
  assert.equal(
    steps.followBackTarget({ ...open, listRequested: true }),
    "suggestions",
  );
  assert.equal(
    steps.followBackTarget({ ...open, picked: true }),
    "suggestions",
  );
  assert.equal(
    steps.followBackTarget({ ...open, picked: true, listRequested: true }),
    "accounts",
  );
  assert.equal(steps.followPanel({ ...open, suggestionCount: 0 }), "accounts");
  assert.equal(
    steps.followBackTarget({ ...open, suggestionCount: 0, picked: true }),
    "accounts",
  );
  assert.equal(steps.followPanel({ ...open, linked: true }), "confirm");
  assert.equal(steps.followBackTarget({ ...open, linked: true }), "close");
});

test("dates and rates keep their meaning on the card", () => {
  assert.equal(display.formatCalendarDate("2026-10-01"), "Oct 1, 2026");
  assert.equal(display.formatApr(19.99), "19.99%");
  assert.equal(display.formatApr(0), "0%");
  assert.equal(display.formatUtilization(0), "0% of the limit");
  assert.equal(display.formatUtilization(0.8425), "84.3% of the limit");
  assert.equal(display.formatUtilization(1.5), "150% of the limit");
  assert.equal(display.utilizationFill(0), 0);
  assert.equal(display.utilizationFill(0.773), 77.3);
  assert.equal(display.utilizationFill(1.5), 100);
  assert.equal(display.utilizationFill(Number.NaN), 0);
});
