import assert from "node:assert/strict";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import test from "node:test";
import path from "node:path";
import { pathToFileURL } from "node:url";
import ts from "typescript";

const sourcePath = path.resolve("lib/api/errors.ts");
const source = await readFile(sourcePath, "utf8");
const output = ts.transpileModule(source, {
  compilerOptions: {
    module: ts.ModuleKind.ES2022,
    target: ts.ScriptTarget.ES2022,
  },
});

const tempDir = path.resolve(".tmp-tests");
await mkdir(tempDir, { recursive: true });
const compiledPath = path.join(tempDir, "api-errors.mjs");
await writeFile(compiledPath, output.outputText);

const errors = await import(pathToFileURL(compiledPath).href);

const serverError = {
  status: 500,
  message: "The database operation was expected to affect 1 row.",
  originalError: null,
};

test("a 500 uses the action sentence and adds the exception in development", () => {
  const text = errors.describeApiError(
    serverError,
    "That income source could not be saved.",
    true,
  );
  assert.equal(text.isServerError, true);
  assert.equal(text.message, "That income source could not be saved.");
  assert.equal(
    text.detail,
    "The database operation was expected to affect 1 row.",
  );
  assert.equal(
    errors.getApiErrorMessage(
      serverError,
      "That income source could not be saved.",
      true,
    ),
    "That income source could not be saved.\nThe database operation was expected to affect 1 row.",
  );
});

test("a 500 outside development leaves the exception off the message", () => {
  const text = errors.describeApiError(
    serverError,
    "That income source could not be saved.",
    false,
  );
  assert.equal(text.message, "That income source could not be saved.");
  assert.equal(text.detail, undefined);
  assert.equal(
    errors.getApiErrorMessage(
      serverError,
      "That income source could not be saved.",
      false,
    ),
    "That income source could not be saved.",
  );
});

test("a generic 500 sentence does not become a second line", () => {
  const text = errors.describeApiError(
    {
      status: 500,
      message: "An unexpected error occurred.",
      originalError: null,
    },
    "That income source could not be saved.",
    true,
  );
  assert.equal(text.message, "That income source could not be saved.");
  assert.equal(text.detail, undefined);
});

test("validation text stays on the message", () => {
  const text = errors.describeApiError(
    {
      status: 400,
      message: "Low net pay cannot be higher than the typical amount.",
      originalError: null,
    },
    "That income source could not be saved.",
    true,
  );
  assert.equal(text.isServerError, false);
  assert.equal(
    text.message,
    "Low net pay cannot be higher than the typical amount.",
  );
  assert.equal(text.detail, undefined);
});

test("a failure without a status keeps its message", () => {
  const text = errors.describeApiError(
    new Error("Network Error"),
    "That income source could not be saved.",
    true,
  );
  assert.equal(text.isServerError, false);
  assert.equal(text.message, "Network Error");
});
