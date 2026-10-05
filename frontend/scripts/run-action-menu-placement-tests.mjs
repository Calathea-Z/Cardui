import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

const tempDir = path.resolve(".tmp-tests");
await mkdir(tempDir, { recursive: true });

/**
 * Loads a TypeScript module after erasing its types.
 * A relative import is pointed at the compiled file for that module.
 */
async function loadModule(sourcePath, compiledName, replacements = []) {
  const source = await readFile(path.resolve(sourcePath), "utf8");
  let output = ts.transpileModule(source, {
    compilerOptions: {
      module: ts.ModuleKind.ES2022,
      target: ts.ScriptTarget.ES2022,
    },
  }).outputText;

  for (const [from, to] of replacements) {
    output = output.replaceAll(from, to);
  }

  const compiledPath = path.join(tempDir, compiledName);
  await writeFile(compiledPath, output);
  return import(pathToFileURL(compiledPath).href);
}

const selectUrl = pathToFileURL(path.join(tempDir, "select-popover.mjs")).href;
await loadModule("components/ui/select-popover.ts", "select-popover.mjs");
const placement = await loadModule(
  "components/ui/action-menu-placement.ts",
  "action-menu-placement.mjs",
  [['"./select-popover"', JSON.stringify(selectUrl)]],
);

test("a menu beside the sidebar stays in the page column", () => {
  const menu = placement.placeActionMenu(
    { top: 40, bottom: 76, left: 272, width: 36 },
    { width: 1100, height: 800 },
    256,
  );

  assert.equal(menu.left, 256);
  assert.equal(menu.width, 192);
  assert.ok(menu.left + menu.width <= 1100 - 8);
  assert.equal(menu.side, "below");
});

test("a menu on the right lines up with the trigger", () => {
  const menu = placement.placeActionMenu(
    { top: 40, bottom: 76, left: 1000, width: 36 },
    { width: 1100, height: 800 },
    256,
  );

  assert.equal(menu.left, 844);
  assert.equal(menu.left + menu.width, 1036);
});

test("a menu at the left edge of a phone stays on screen", () => {
  const menu = placement.placeActionMenu(
    { top: 40, bottom: 76, left: 16, width: 36 },
    { width: 390, height: 800 },
    0,
  );

  assert.equal(menu.left, 8);
  assert.equal(menu.width, 192);
  assert.ok(menu.left + menu.width < 390);
});
