import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

const sourcePath = path.resolve("features/transactions/importCsv.ts");
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
const compiledPath = path.join(tempDir, "importCsv.mjs");
await writeFile(compiledPath, output.outputText);

const importCsv = await import(pathToFileURL(compiledPath).href);

const columns = {
  dateColumn: "0",
  nameColumn: "1",
  amountMode: "amount",
  amountColumn: "2",
  debitColumn: "",
  creditColumn: "",
  categoryColumn: "",
  notesColumn: "",
  amountSign: "PositiveOut",
  dateOrder: "MonthFirst",
};

test("import steps follow account, columns, rows, then import", () => {
  assert.deepEqual(
    importCsv.importSteps.map((step) => importCsv.importStepHeading(step)),
    [
      "Choose the account and file",
      "Map columns",
      "Preview and choose rows",
      "Import",
    ],
  );
  assert.equal(importCsv.nextImportStep("account"), "columns");
  assert.equal(importCsv.nextImportStep("columns"), "rows");
  assert.equal(importCsv.nextImportStep("rows"), "import");
  assert.equal(importCsv.nextImportStep("import"), null);
  assert.equal(importCsv.previousImportStep("account"), null);
  assert.equal(importCsv.previousImportStep("import"), "rows");
});

test("mapping requires date, name, and an amount column", () => {
  assert.equal(
    importCsv.mappingError({ ...columns, dateColumn: "" }),
    "Choose the date and name columns.",
  );
  assert.equal(
    importCsv.mappingError({ ...columns, amountColumn: "" }),
    "Choose the amount column.",
  );
  assert.equal(importCsv.mappingError(columns), null);
});

test("split amounts require a debit column, a credit column, or both", () => {
  assert.equal(
    importCsv.mappingError({
      ...columns,
      amountMode: "split",
      amountColumn: "",
    }),
    "Choose a debit column, a credit column, or both.",
  );
  assert.equal(
    importCsv.mappingError({
      ...columns,
      amountMode: "split",
      amountColumn: "",
      debitColumn: "2",
    }),
    null,
  );
});

test("a suggested debit column starts in split mode", () => {
  const state = importCsv.columnStateFromSuggestion({
    dateColumn: 0,
    nameColumn: 1,
    amountColumn: null,
    debitColumn: 2,
    creditColumn: 3,
    categoryColumn: null,
    notesColumn: null,
    amountSign: "PositiveOut",
    dateOrder: "DayFirst",
  });

  assert.equal(state.amountMode, "split");
  assert.equal(state.debitColumn, "2");
  assert.equal(state.creditColumn, "3");
  assert.equal(state.dateOrder, "DayFirst");
});
