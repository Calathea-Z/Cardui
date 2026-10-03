import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

const sourcePath = path.resolve(
  "features/dashboard/dashboardMonthlyActivity.ts",
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
const compiledPath = path.join(tempDir, "dashboardMonthlyActivity.mjs");
await writeFile(compiledPath, output.outputText);

const activity = await import(pathToFileURL(compiledPath).href);

test("formatDashboardPeriod formats a current-month range", () => {
  assert.equal(
    activity.formatDashboardPeriod("2026-10-01", "2026-10-02"),
    "October 1–2, 2026",
  );
});

test("formatDashboardPeriod falls back for missing API period data", () => {
  assert.equal(activity.formatDashboardPeriod("", ""), "Current month");
});

test("calculateCategoryPercentage handles normal and invalid totals", () => {
  assert.equal(activity.calculateCategoryPercentage(25, 100), 25);
  assert.equal(activity.calculateCategoryPercentage(50, 0), 0);
  assert.equal(activity.calculateCategoryPercentage(125, 100), 100);
});
