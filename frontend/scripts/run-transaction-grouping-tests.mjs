import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

const sourcePath = path.resolve("features/transactions/transactionGrouping.ts");
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
const compiledPath = path.join(tempDir, "transactionGrouping.mjs");
await writeFile(compiledPath, output.outputText);

const grouping = await import(pathToFileURL(compiledPath).href);

function tx(id, date) {
  return { id, date };
}

test("toDateKey slices to YYYY-MM-DD", () => {
  assert.equal(grouping.toDateKey("2026-07-20T15:30:00Z"), "2026-07-20");
  assert.equal(grouping.toDateKey("2026-01-05"), "2026-01-05");
});

test("formatDateKey formats local calendar date", () => {
  assert.equal(grouping.formatDateKey(new Date(2026, 6, 20)), "2026-07-20");
  assert.equal(grouping.formatDateKey(new Date(2026, 0, 5)), "2026-01-05");
});

test("groupTransactionsByDate groups contiguous same-date rows", () => {
  const groups = grouping.groupTransactionsByDate([
    tx("1", "2026-07-20T10:00:00Z"),
    tx("2", "2026-07-20T12:00:00Z"),
    tx("3", "2026-07-19T09:00:00Z"),
    tx("4", "2026-07-19T11:00:00Z"),
    tx("5", "2026-07-18T08:00:00Z"),
  ]);

  assert.equal(groups.length, 3);
  assert.deepEqual(
    groups.map((group) => ({
      date: group.date,
      ids: group.transactions.map((item) => item.id),
    })),
    [
      { date: "2026-07-20", ids: ["1", "2"] },
      { date: "2026-07-19", ids: ["3", "4"] },
      { date: "2026-07-18", ids: ["5"] },
    ],
  );
});

test("groupTransactionsByDate starts a new group when dates are non-contiguous", () => {
  const groups = grouping.groupTransactionsByDate([
    tx("1", "2026-07-20"),
    tx("2", "2026-07-19"),
    tx("3", "2026-07-20"),
  ]);

  assert.deepEqual(
    groups.map((group) => group.date),
    ["2026-07-20", "2026-07-19", "2026-07-20"],
  );
});

test("formatDateSectionHeader returns Today and Yesterday", () => {
  const todayKey = grouping.formatDateKey(new Date());
  const yesterday = new Date();
  yesterday.setDate(yesterday.getDate() - 1);
  const yesterdayKey = grouping.formatDateKey(yesterday);

  assert.equal(grouping.formatDateSectionHeader(todayKey), "Today");
  assert.equal(grouping.formatDateSectionHeader(yesterdayKey), "Yesterday");
});

test("formatDateSectionHeader formats other dates in the current year", () => {
  const currentYear = new Date().getFullYear();
  const dateKey = `${currentYear}-03-15`;
  const label = grouping.formatDateSectionHeader(dateKey);

  assert.match(
    label,
    /Sunday|Monday|Tuesday|Wednesday|Thursday|Friday|Saturday/,
  );
  assert.match(label, /March/);
  assert.match(label, /15/);
  assert.doesNotMatch(label, new RegExp(String(currentYear)));
});
