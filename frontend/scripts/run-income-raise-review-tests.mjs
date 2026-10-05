import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

const sourcePath = path.resolve("features/income/incomeRaiseReview.ts");
const source = await readFile(sourcePath, "utf8");
const output = ts.transpileModule(source, {
  compilerOptions: {
    module: ts.ModuleKind.ES2022,
    target: ts.ScriptTarget.ES2022,
  },
});

const tempDir = path.resolve(".tmp-tests");
await mkdir(tempDir, { recursive: true });
const compiledPath = path.join(tempDir, "incomeRaiseReview.mjs");
await writeFile(compiledPath, output.outputText);

const review = await import(pathToFileURL(compiledPath).href);

test("calendarDateInTimeZone uses the household calendar day", () => {
  const eveningBeforeInDenver = new Date("2026-10-05T05:30:00Z");
  assert.equal(
    review.calendarDateInTimeZone("America/Denver", eveningBeforeInDenver),
    "2026-10-04",
  );
  assert.equal(
    review.calendarDateInTimeZone(
      "America/Denver",
      new Date("2026-10-05T06:00:00Z"),
    ),
    "2026-10-05",
  );
});

test("calendarDateInTimeZone uses the UTC date when the zone is unrecognized", () => {
  assert.equal(
    review.calendarDateInTimeZone(
      "Not/A_Zone",
      new Date("2026-10-05T12:00:00Z"),
    ),
    "2026-10-05",
  );
});

test("isRaiseDue is true on the raise date and false the day before", () => {
  assert.equal(review.isRaiseDue("2026-12-01", "2026-12-01"), true);
  assert.equal(review.isRaiseDue("2026-12-01T00:00:00", "2026-12-01"), true);
  assert.equal(review.isRaiseDue("2026-12-02", "2026-12-01"), false);
});

test("amountsAfterConfirmingRaise clears a scenario that no longer fits", () => {
  const belowLow = review.amountsAfterConfirmingRaise(
    {
      takeHomeAmount: 2400,
      lowTakeHomeAmount: 2200,
      strongTakeHomeAmount: 2600,
    },
    2000,
  );
  assert.equal(belowLow.takeHomeAmount, 2000);
  assert.equal(belowLow.lowTakeHomeAmount, null);
  assert.equal(belowLow.strongTakeHomeAmount, 2600);
  assert.equal(belowLow.clearedLow, true);
  assert.equal(belowLow.clearedStrong, false);

  const aboveStrong = review.amountsAfterConfirmingRaise(
    {
      takeHomeAmount: 2400,
      lowTakeHomeAmount: 1800,
      strongTakeHomeAmount: 3000,
    },
    3200,
  );
  assert.equal(aboveStrong.lowTakeHomeAmount, 1800);
  assert.equal(aboveStrong.strongTakeHomeAmount, null);
  assert.equal(aboveStrong.clearedLow, false);
  assert.equal(aboveStrong.clearedStrong, true);

  const kept = review.amountsAfterConfirmingRaise(
    {
      takeHomeAmount: 2400,
      lowTakeHomeAmount: 1800,
      strongTakeHomeAmount: 3000,
    },
    2600,
  );
  assert.equal(kept.lowTakeHomeAmount, 1800);
  assert.equal(kept.strongTakeHomeAmount, 3000);
  assert.equal(kept.clearedLow, false);
  assert.equal(kept.clearedStrong, false);
});

test("raise messages name the typical amount and a cleared scenario", () => {
  assert.equal(
    review.typicalPayUpdatedMessage("Paycheck", "$2,600.00", false, false),
    "Typical pay for Paycheck is now $2,600.00.",
  );
  assert.equal(
    review.typicalPayUpdatedMessage("Paycheck", "$2,000.00", true, false),
    "Typical pay for Paycheck is now $2,000.00. Low pay was cleared because it was higher than that amount.",
  );
  assert.equal(
    review.raiseRemovedMessage("Paycheck", "$2,400.00"),
    "The expected raise was removed. Typical pay for Paycheck is still $2,400.00.",
  );
});
