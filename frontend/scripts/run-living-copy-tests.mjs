import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

/**
 * Loads the Plan budget copy module after erasing its types.
 */
async function loadCopy() {
  const source = await readFile(
    path.resolve("features/living/livingCopy.ts"),
    "utf8",
  );
  const output = ts.transpileModule(source, {
    compilerOptions: {
      module: ts.ModuleKind.ES2022,
      target: ts.ScriptTarget.ES2022,
    },
  });
  const moneySource = await readFile(
    path.resolve("features/accounts/formatCurrency.ts"),
    "utf8",
  );
  const moneyOutput = ts.transpileModule(moneySource, {
    compilerOptions: {
      module: ts.ModuleKind.ES2022,
      target: ts.ScriptTarget.ES2022,
    },
  });
  const tempDir = path.resolve(".tmp-tests");
  await mkdir(tempDir, { recursive: true });
  await writeFile(
    path.join(tempDir, "formatCurrency.mjs"),
    moneyOutput.outputText,
  );
  await writeFile(
    path.join(tempDir, "livingCopy.mjs"),
    output.outputText.replaceAll(
      'from "@/features/accounts/formatCurrency"',
      'from "./formatCurrency.mjs"',
    ),
  );
  return import(pathToFileURL(path.join(tempDir, "livingCopy.mjs")).href);
}

const copy = await loadCopy();
const money = (amount) => `$${amount.toFixed(2)}`;

test("a saved contribution benchmark stays exact when the paycheck average rounds differently", () => {
  const note = copy.contributionNote(
    {
      contributorId: "alex",
      name: "Alex",
      monthlyAmount: 300,
      recordedMonthly: 4333.33,
      sharedMonthly: 300.04,
      keptMonthly: 4033.29,
      limit: "Shared",
      hasUnscheduledPay: false,
    },
    money,
  );

  assert.match(note, /saved monthly benchmark is \$300\.00/);
  assert.match(note, /current monthly average is \$300\.04/);
});
