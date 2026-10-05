import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

const sourcePath = path.resolve("components/ui/date-field-calendar.ts");
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
const compiledPath = path.join(tempDir, "date-field-calendar.mjs");
await writeFile(compiledPath, output.outputText);

const calendar = await import(pathToFileURL(compiledPath).href);

test("October 2026 starts on Thursday and keeps Sunday as the first column", () => {
  const days = calendar.buildMonthGrid(2026, 10);

  assert.equal(days.length, 42);
  assert.deepEqual(days[0], { date: "2026-09-27", inMonth: false });
  assert.deepEqual(days[4], { date: "2026-10-01", inMonth: true });
  assert.deepEqual(days[7], { date: "2026-10-04", inMonth: true });
  assert.deepEqual(days[34], { date: "2026-10-31", inMonth: true });
  assert.deepEqual(days[35], { date: "2026-11-01", inMonth: false });
});

test("addDays crosses a month boundary", () => {
  assert.equal(calendar.addDays("2026-10-31", 1), "2026-11-01");
  assert.equal(calendar.addDays("2026-03-01", -1), "2026-02-28");
});

test("isDateInRange keeps the inclusive bounds", () => {
  assert.equal(
    calendar.isDateInRange("1999-12-31", "2000-01-01", "2100-12-31"),
    false,
  );
  assert.equal(
    calendar.isDateInRange("2000-01-01", "2000-01-01", "2100-12-31"),
    true,
  );
  assert.equal(
    calendar.isDateInRange("2100-12-31", "2000-01-01", "2100-12-31"),
    true,
  );
  assert.equal(calendar.isDateInRange("2026-02-31"), false);
});

test("monthHasSelectableDay rejects a month entirely outside the bounds", () => {
  assert.equal(
    calendar.monthHasSelectableDay(1999, 12, "2000-01-01", "2100-12-31"),
    false,
  );
  assert.equal(
    calendar.monthHasSelectableDay(2000, 1, "2000-01-01", "2100-12-31"),
    true,
  );
});
