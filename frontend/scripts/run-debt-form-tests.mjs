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
  const compiledPath = path.join(tempDir, compiledName);
  await writeFile(compiledPath, output.outputText);
  return import(pathToFileURL(compiledPath).href);
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

test("dates and rates keep their meaning on the card", () => {
  assert.equal(display.formatCalendarDate("2026-10-01"), "Oct 1, 2026");
  assert.equal(display.formatApr(19.99), "19.99%");
  assert.equal(display.formatApr(0), "0%");
  assert.equal(display.formatUtilization(0), "0% of the limit");
  assert.equal(display.formatUtilization(0.8425), "84.3% of the limit");
  assert.equal(display.formatUtilization(1.5), "150% of the limit");
});
