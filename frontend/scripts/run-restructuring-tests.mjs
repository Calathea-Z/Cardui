import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

/**
 * Loads one restructuring module after erasing its types.
 */
async function loadModule(filename) {
  const source = await readFile(
    path.resolve("features/plan", filename),
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

const rules = await loadModule("restructuringRules.ts");
const copy = await loadModule("restructuringCopy.ts");
const money = (amount) => `$${amount.toFixed(2)}`;
const date = (value) => value;

const card = {
  debtId: "card",
  name: "Card",
  kind: "Revolving",
  balance: 1000,
  currency: "USD",
};
const loan = {
  debtId: "loan",
  name: "Loan",
  kind: "Installment",
  balance: 500,
  currency: "USD",
};

/**
 * A comparison whose payment is lower and whose total cost is higher.
 * The sentences state both facts and do not rank the payment.
 */
function costlyOffer() {
  return {
    kind: "ReplacementLoan",
    debts: [
      {
        debtId: "loan",
        name: "Loan",
        kind: "Installment",
        balance: 1200,
        apr: 0,
        promotionalApr: null,
        promotionalEndsOn: null,
        stop: "PaidOff",
        paidOffOn: "2026-12-01",
      },
    ],
    coveredBalance: 1200,
    fee: 0,
    feeFunding: "Financed",
    upfrontCash: 0,
    cashDueNow: 0,
    newPrincipal: 1200,
    offerApr: 12,
    termMonths: 24,
    offerPaymentCount: 24,
    offerMonthlyPayment: 56.49,
    keepMonthlyPayment: 100,
    monthlyCashFlow: 43.51,
    keepTotalInterest: 0,
    offerTotalInterest: 155.76,
    keepTotalCost: 0,
    offerTotalCost: 155.76,
    keepPaidOffOn: "2026-12-01",
    offerPaidOffOn: "2027-12-01",
    offerStop: "PaidOff",
    breakEven: "Never",
    breakEvenOn: null,
    cash: {
      cashOnHand: 0,
      reserve: 0,
      cashToKeep: 0,
      hasCashFloor: false,
      exceedsCashOnHand: false,
      usesReserve: false,
      leavesLessThanCashToKeep: false,
    },
  };
}

test("a form asks again for a missing rate, a bad fee, and the wrong debt count", () => {
  const blank = rules.readRestructuringForm(
    {
      ...rules.initialRestructuringForm(),
      debtIds: ["loan"],
      apr: "",
      termMonths: "12",
    },
    [loan],
    "USD",
  );
  assert.equal(blank.ok, false);
  assert.equal(blank.error, "Enter the APR as a percent.");

  const fee = rules.readRestructuringForm(
    {
      ...rules.initialRestructuringForm(),
      debtIds: ["loan"],
      apr: "8",
      termMonths: "12",
      fee: "1.234",
    },
    [loan],
    "USD",
  );
  assert.equal(fee.error, "Enter the fee in dollars and cents, or zero.");

  const consolidation = rules.readRestructuringForm(
    {
      ...rules.initialRestructuringForm(),
      kind: "ConsolidationLoan",
      debtIds: ["loan"],
      apr: "8",
      termMonths: "12",
    },
    [loan],
    "USD",
  );
  assert.equal(
    consolidation.error,
    "A consolidation loan replaces two or more debts. A single debt is a replacement loan.",
  );
});

test("a balance transfer rejects an installment loan and another currency", () => {
  const form = {
    ...rules.initialRestructuringForm(),
    kind: "BalanceTransfer",
    debtIds: ["loan"],
    apr: "0",
    termMonths: "12",
  };
  assert.equal(
    rules.readRestructuringForm(form, [loan], "USD").error,
    "A balance transfer can include only revolving debts. Loan is an installment loan.",
  );
  assert.equal(
    rules.debtSelectionBlock({ ...card, currency: "EUR" }, "USD"),
    "Card is in another currency, so it can't be part of this comparison.",
  );
});

test("a lower payment is not treated as better when the offer costs more", () => {
  const comparison = costlyOffer();
  assert.match(copy.costSentence(comparison, money), /costs \$155\.76 more/);
  assert.match(
    copy.paymentSentence(comparison, money),
    /\$43\.51 lower each month/,
  );
  assert.match(copy.offerNotRanked, /not treated as the better offer/);
  assert.match(copy.breakEvenSentence(comparison, date), /no break-even date/);
  assert.equal(copy.costSentence(comparison, money).includes("better"), false);
  assert.equal(
    copy.paymentSentence(comparison, money).includes("better"),
    false,
  );
});

test("break-even, an unknown path, and upfront cash each stay explicit", () => {
  const reached = {
    ...costlyOffer(),
    breakEven: "Reached",
    breakEvenOn: "2026-04-01",
    offerTotalCost: 30,
    keepTotalCost: 80,
  };
  assert.match(copy.breakEvenSentence(reached, date), /from 2026-04-01/);
  assert.match(copy.costSentence(reached, money), /costs \$50\.00 less/);

  const unknown = {
    ...costlyOffer(),
    keepTotalCost: null,
    keepTotalInterest: null,
    monthlyCashFlow: null,
    keepMonthlyPayment: null,
    breakEven: "Unknown",
    debts: [
      { ...costlyOffer().debts[0], stop: "DoesNotPayDown", paidOffOn: null },
    ],
  };
  assert.match(
    copy.unknownCostSentence(unknown),
    /does not reduce the balance/,
  );
  assert.match(copy.paymentSentence(unknown, money), /unknown/);
  assert.match(copy.breakEvenSentence(unknown, date), /not shown/);

  const cash = {
    ...costlyOffer(),
    cashDueNow: 200,
    upfrontCash: 200,
    cash: {
      ...costlyOffer().cash,
      cashOnHand: 100,
      exceedsCashOnHand: true,
    },
  };
  assert.equal(copy.opportunityWarns(cash), true);
  assert.match(
    copy.opportunitySentence(cash, money),
    /more than the \$100\.00/,
  );
  assert.match(copy.opportunitySentence(cash, money), /investment return/);
  assert.match(copy.offerNotApproved, /does not treat them as approved/);
  assert.match(copy.offerScope, /current minimum payments/);
  assert.match(copy.offerScope, /not saved with Your plan/);
  assert.match(copy.balanceTransferRateLimit, /whole term/);
});
