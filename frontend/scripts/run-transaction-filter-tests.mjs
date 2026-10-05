import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

const sourcePath = path.resolve("features/transactions/transactionFilters.ts");
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
const compiledPath = path.join(tempDir, "transactionFilters.mjs");
await writeFile(compiledPath, output.outputText);

const filters = await import(pathToFileURL(compiledPath).href);

test("counts only account, category, and status filters", () => {
  assert.equal(
    filters.countTransactionChoiceFilters({
      accountId: "",
      categoryId: "",
      pendingFilter: "all",
    }),
    0,
  );
  assert.equal(
    filters.countTransactionChoiceFilters({
      accountId: "account-1",
      categoryId: "category-1",
      pendingFilter: "pending",
    }),
    3,
  );
});
