import { browserClient } from "../browser-client";
import type {
  TransactionImportBatchDto,
  TransactionImportInspectDto,
  TransactionImportPreviewDto,
} from "../types";

/**
 * Multipart settings for CSV upload calls.
 * The longer timeout covers reading and checking a file. The false content type lets the browser set the form boundary.
 */
const importRequest = {
  timeout: 30_000,
  headers: {
    "Content-Type": false,
  },
};

/**
 * POST /api/transaction-imports/inspect
 * Reads a CSV and suggests which columns map to date, name, and amount.
 */
export async function inspectTransactionImport(
  file: File,
): Promise<TransactionImportInspectDto> {
  const form = new FormData();
  form.append("file", file);
  const response = await browserClient.post<TransactionImportInspectDto>(
    "/api/transaction-imports/inspect",
    form,
    importRequest,
  );
  return response.data;
}

/**
 * POST /api/transaction-imports/preview
 * Checks the mapped rows before anything is saved.
 */
export async function previewTransactionImport(
  form: FormData,
): Promise<TransactionImportPreviewDto> {
  const response = await browserClient.post<TransactionImportPreviewDto>(
    "/api/transaction-imports/preview",
    form,
    importRequest,
  );
  return response.data;
}

/**
 * POST /api/transaction-imports
 * Saves the chosen preview rows as a transaction import batch.
 */
export async function commitTransactionImport(
  form: FormData,
): Promise<TransactionImportBatchDto> {
  const response = await browserClient.post<TransactionImportBatchDto>(
    "/api/transaction-imports",
    form,
    importRequest,
  );
  return response.data;
}

/**
 * GET /api/transaction-imports
 * Lists import batches that can still be undone.
 */
export async function listTransactionImports(): Promise<
  TransactionImportBatchDto[]
> {
  const response = await browserClient.get<TransactionImportBatchDto[]>(
    "/api/transaction-imports",
  );
  return response.data;
}

/**
 * POST /api/transaction-imports/{id}/undo
 * Archives the transactions that belonged to one import batch.
 */
export async function undoTransactionImport(
  id: string,
): Promise<TransactionImportBatchDto> {
  const response = await browserClient.post<TransactionImportBatchDto>(
    `/api/transaction-imports/${id}/undo`,
  );
  return response.data;
}
