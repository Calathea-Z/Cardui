import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

const sourcePath = path.resolve("features/household/currencyExclusion.ts");
const source = await readFile(sourcePath, "utf8");
const output = ts.transpileModule(source, {
  compilerOptions: {
    module: ts.ModuleKind.ES2022,
    target: ts.ScriptTarget.ES2022,
  },
});

const tempDir = path.resolve(".tmp-tests");
await mkdir(tempDir, { recursive: true });
const compiledPath = path.join(tempDir, "currencyExclusion.mjs");
await writeFile(compiledPath, output.outputText);

const exclusion = await import(pathToFileURL(compiledPath).href);

test("formatCurrencyExclusion stays quiet when every amount is included", () => {
  assert.equal(
    exclusion.formatCurrencyExclusion({
      planningCurrency: "USD",
      excludedAccountCount: 0,
      excludedTransactionCount: 0,
      excludedCurrencies: [],
    }),
    null,
  );
});

test("formatCurrencyExclusion names the currencies left out of totals", () => {
  assert.equal(
    exclusion.formatCurrencyExclusion({
      planningCurrency: "USD",
      excludedAccountCount: 1,
      excludedTransactionCount: 2,
      excludedCurrencies: ["CAD"],
    }),
    "Totals use USD. 1 account and 2 transactions in CAD are left out until conversion is available.",
  );
});
