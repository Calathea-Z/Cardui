import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

/**
 * Loads the card-activity rule after erasing its types.
 */
async function loadRules() {
  const source = await readFile(
    path.resolve("features/transactions/cardActivity.ts"),
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
  const compiledPath = path.join(tempDir, "cardActivity.mjs");
  await writeFile(compiledPath, output.outputText);
  return import(pathToFileURL(compiledPath).href);
}

const { isCardPaymentActivity } = await loadRules();

test("a card purchase is not a payment", () => {
  assert.equal(
    isCardPaymentActivity({
      accountType: "credit",
      amount: 40,
      name: "Trader Joe",
    }),
    false,
  );
});

test("a described payment from cash is a card payment", () => {
  assert.equal(
    isCardPaymentActivity({
      accountType: "depository",
      amount: 40,
      name: "Chase credit crd autopay",
    }),
    true,
  );
});

test("a payment credit on the card is a card payment", () => {
  assert.equal(
    isCardPaymentActivity({
      accountType: "credit",
      amount: -40,
      name: "Payment thank you",
    }),
    true,
  );
});

test("a refund on the card stays a refund", () => {
  assert.equal(
    isCardPaymentActivity({
      accountType: "credit",
      amount: -12,
      name: "Amazon refund",
    }),
    false,
  );
});

test("an Amex e-payment is a card payment and a repayment is not", () => {
  assert.equal(
    isCardPaymentActivity({
      accountType: "depository",
      amount: 40,
      name: "Amex epayment",
    }),
    true,
  );
  assert.equal(
    isCardPaymentActivity({
      accountType: "credit",
      amount: -15,
      name: "Repayment",
    }),
    false,
  );
});

test("a mortgage and an electric autopay stay spending", () => {
  assert.equal(
    isCardPaymentActivity({
      accountType: "depository",
      amount: 400,
      name: "Mortgage",
    }),
    false,
  );
  assert.equal(
    isCardPaymentActivity({
      accountType: "depository",
      amount: 90,
      name: "Autopay electric",
    }),
    false,
  );
});
