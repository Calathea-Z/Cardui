import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

const sourcePath = path.resolve("features/income/paycheckSchedule.ts");
const source = await readFile(sourcePath, "utf8");
const output = ts.transpileModule(source, {
  compilerOptions: {
    module: ts.ModuleKind.ES2022,
    target: ts.ScriptTarget.ES2022,
  },
});

const tempDir = path.resolve(".tmp-tests");
await mkdir(tempDir, { recursive: true });
const compiledPath = path.join(tempDir, "paycheckSchedule.mjs");
await writeFile(compiledPath, output.outputText);

const schedule = await import(pathToFileURL(compiledPath).href);

test("upcomingPaymentDates keeps three biweekly paychecks in January", () => {
  const dates = schedule.upcomingPaymentDates(
    "Biweekly",
    "2026-01-02",
    "2026-01-01",
  );
  assert.deepEqual(dates, [
    "2026-01-02",
    "2026-01-16",
    "2026-01-30",
    "2026-02-13",
    "2026-02-27",
    "2026-03-13",
    "2026-03-27",
  ]);
  assert.equal(dates.filter((date) => date.startsWith("2026-01")).length, 3);
  assert.equal(schedule.averageMonthlyAmount(1000, "Biweekly"), 2166.67);
  assert.equal(dates.includes("2026-01-01"), false);
});

test("upcomingPaymentDates skips past biweekly dates without moving the weekday", () => {
  const dates = schedule.upcomingPaymentDates(
    "Biweekly",
    "2026-01-02",
    "2026-02-01",
  );
  assert.equal(dates[0], "2026-02-13");
  assert.equal(dates.includes("2026-02-01"), false);
});

test("upcomingPaymentDates restores a month-end day after February", () => {
  assert.deepEqual(
    schedule.upcomingPaymentDates("Monthly", "2026-01-31", "2026-01-31"),
    ["2026-01-31", "2026-02-28", "2026-03-31"],
  );
});

test("upcomingPaymentDates keeps semimonthly on two days", () => {
  const dates = schedule.upcomingPaymentDates(
    "Semimonthly",
    "2026-10-15",
    "2026-10-01",
  );
  assert.deepEqual(dates, [
    "2026-10-15",
    "2026-10-30",
    "2026-11-15",
    "2026-11-30",
    "2026-12-15",
    "2026-12-30",
  ]);
  assert.equal(dates.includes("2026-10-29"), false);
});

test("upcomingPaymentDates is empty when there is no schedule", () => {
  assert.deepEqual(
    schedule.upcomingPaymentDates("Irregular", "2026-10-16", "2026-10-04"),
    [],
  );
  assert.equal(schedule.averageMonthlyAmount(2400, "Irregular"), null);
});

const displaySource = await readFile(
  path.resolve("features/income/incomeSourceDisplay.ts"),
  "utf8",
);
const displayOutput = ts.transpileModule(displaySource, {
  compilerOptions: {
    module: ts.ModuleKind.ES2022,
    target: ts.ScriptTarget.ES2022,
  },
});
const displayPath = path.join(tempDir, "incomeSourceDisplay.mjs");
await writeFile(displayPath, displayOutput.outputText);
const display = await import(pathToFileURL(displayPath).href);

test("formatPayDay keeps the month with the day", () => {
  assert.equal(display.formatPayDay("2026-10-09", "2026-10-05"), "Oct 9");
  assert.equal(display.formatPayDay("2027-01-08", "2026-10-05"), "Jan 8, 2027");
});
