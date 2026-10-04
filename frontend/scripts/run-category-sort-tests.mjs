import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

const sourcePath = path.resolve("features/categories/categorySort.ts");
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
const compiledPath = path.join(tempDir, "categorySort.mjs");
await writeFile(compiledPath, output.outputText);

const categorySort = await import(pathToFileURL(compiledPath).href);

test("sortCategoriesByName sorts alphabetically without mutating input", () => {
  const input = [
    { id: "2", name: "Zebra" },
    { id: "1", name: "Apple" },
    { id: "3", name: "Mango" },
  ];
  const originalOrder = input.map((category) => category.id);

  const sorted = categorySort.sortCategoriesByName(input);

  assert.deepEqual(
    sorted.map((category) => category.name),
    ["Apple", "Mango", "Zebra"],
  );
  assert.deepEqual(
    input.map((category) => category.id),
    originalOrder,
  );
});

test("sortByOrderThenName orders by sort order, then name", () => {
  const input = [
    { sortOrder: 2, name: "Bills" },
    { sortOrder: 1, name: "Zebra" },
    { sortOrder: 1, name: "Apple" },
  ];

  const sorted = categorySort.sortByOrderThenName(input);

  assert.deepEqual(
    sorted.map((item) => item.name),
    ["Apple", "Zebra", "Bills"],
  );
  assert.deepEqual(
    input.map((item) => item.name),
    ["Bills", "Zebra", "Apple"],
  );
});

test("sortCategoriesByName returns a new array for empty input", () => {
  const input = [];
  const sorted = categorySort.sortCategoriesByName(input);

  assert.deepEqual(sorted, []);
  assert.notEqual(sorted, input);
});
