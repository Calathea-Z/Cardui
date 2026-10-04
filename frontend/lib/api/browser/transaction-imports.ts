import { browserClient } from "../browser-client";
import type {
  TransactionImportBatchDto,
  TransactionImportInspectDto,
  TransactionImportPreviewDto,
} from "../types";

const importRequest = {
  timeout: 30_000,
  headers: {
    "Content-Type": false,
  },
};

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

export async function listTransactionImports(): Promise<
  TransactionImportBatchDto[]
> {
  const response = await browserClient.get<TransactionImportBatchDto[]>(
    "/api/transaction-imports",
  );
  return response.data;
}

export async function undoTransactionImport(
  id: string,
): Promise<TransactionImportBatchDto> {
  const response = await browserClient.post<TransactionImportBatchDto>(
    `/api/transaction-imports/${id}/undo`,
  );
  return response.data;
}
