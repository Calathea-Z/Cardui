import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

const sourcePath = path.resolve("components/ui/select-popover.ts");
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
const compiledPath = path.join(tempDir, "select-popover.mjs");
await writeFile(compiledPath, output.outputText);

const popover = await import(pathToFileURL(compiledPath).href);

test("opens below a trigger that has room underneath", () => {
  const placement = popover.placeSelectPopover(
    { top: 100, bottom: 140, left: 10, width: 300 },
    { width: 800, height: 600 },
  );

  assert.equal(placement.side, "below");
  assert.equal(placement.top, 144);
  assert.equal(placement.left, 10);
  assert.equal(placement.width, 300);
  assert.equal(placement.maxHeight, 320);
});

test("opens above a trigger that is near the bottom", () => {
  const placement = popover.placeSelectPopover(
    { top: 540, bottom: 580, left: 20, width: 200 },
    { width: 800, height: 600 },
  );

  assert.equal(placement.side, "above");
  assert.equal(placement.bottom, 64);
  assert.equal(placement.maxHeight, 320);
});

test("shifts left when the trigger is near the right edge", () => {
  const placement = popover.placeSelectPopover(
    { top: 40, bottom: 80, left: 700, width: 80 },
    { width: 800, height: 600 },
  );

  assert.equal(placement.width, 192);
  assert.equal(placement.left, 600);
});

test("uses a requested width and height when the viewport allows", () => {
  const placement = popover.placeSelectPopover(
    { top: 100, bottom: 140, left: 10, width: 120 },
    { width: 800, height: 700 },
    { minWidth: 320, maxHeight: 448 },
  );

  assert.equal(placement.width, 320);
  assert.equal(placement.left, 10);
  assert.equal(placement.maxHeight, 448);
});

test("keeps a fixed width when the trigger is wider", () => {
  const placement = popover.placeSelectPopover(
    { top: 100, bottom: 140, left: 40, width: 400 },
    { width: 800, height: 600 },
    { minWidth: 320, fixedWidth: true },
  );

  assert.equal(placement.width, 320);
  assert.equal(placement.left, 40);
});

test("shrinks a fixed width to the viewport", () => {
  const placement = popover.placeSelectPopover(
    { top: 10, bottom: 40, left: 10, width: 40 },
    { width: 100, height: 400 },
    { minWidth: 320, fixedWidth: true },
  );

  assert.equal(placement.width, 84);
  assert.equal(placement.left, 8);
});

test("shrinks to the viewport when it is narrower than 12rem", () => {
  const placement = popover.placeSelectPopover(
    { top: 10, bottom: 40, left: 10, width: 40 },
    { width: 100, height: 400 },
  );

  assert.equal(placement.width, 84);
  assert.equal(placement.left, 8);
});
