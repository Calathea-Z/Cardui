import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

const sourcePath = path.resolve(
  "features/transactions/transactionListPresentation.ts",
);
const source = await readFile(sourcePath, "utf8");
const output = ts.transpileModule(source, {
  compilerOptions: {
    module: ts.ModuleKind.ES2022,
    target: ts.ScriptTarget.ES2022,
    importsNotUsedAsValues: ts.ImportsNotUsedAsValues.Remove,
  },
});

const tempDir = path.resolve(".tmp-tests");
await mkdir(tempDir, { recursive: true });
const compiledPath = path.join(tempDir, "transactionListPresentation.mjs");
await writeFile(compiledPath, output.outputText);

const presentation = await import(pathToFileURL(compiledPath).href);

test("uses the merchant name when the bank sent one", () => {
  assert.equal(
    presentation.transactionListTitle({
      name: "STARBUCKS STORE 12345 SEATTLE WA",
      merchantName: "Starbucks",
    }),
    "Starbucks",
  );
});

test("uses the stored name when no merchant name was sent", () => {
  assert.equal(
    presentation.transactionListTitle({
      name: "ACH PAYMENT 9981",
      merchantName: null,
    }),
    "ACH PAYMENT 9981",
  );
});

test("ignores a blank merchant name", () => {
  assert.equal(
    presentation.transactionListTitle({
      name: "ACH PAYMENT 9981",
      merchantName: "   ",
    }),
    "ACH PAYMENT 9981",
  );
});

test("joins the account and category under the title", () => {
  assert.equal(
    presentation.transactionListContext({
      account: { name: "Checking" },
      category: { name: "Groceries" },
    }),
    "Checking · Groceries",
  );
});

test("uses Uncategorized when the category is missing", () => {
  assert.equal(
    presentation.transactionListContext({
      account: { name: "Checking" },
      category: null,
    }),
    "Checking · Uncategorized",
  );
});
