import { browserClient } from "../browser-client";
import type {
  CreateManualTransactionDto,
  MerchantHistoryDto,
  MerchantHistoryGranularity,
  PagedResultDto,
  TransactionDto,
  TransactionQueryDto,
  UpdateTransactionDetailsDto,
} from "../types";

/**
 * GET /api/transactions
 * Lists one page of the household's transactions.
 */
export async function getTransactions(
  query?: TransactionQueryDto,
): Promise<PagedResultDto<TransactionDto>> {
  const response = await browserClient.get<PagedResultDto<TransactionDto>>(
    "/api/transactions",
    {
      params: query,
    },
  );

  return response.data;
}

/**
 * GET /api/transactions/{id}/merchant-history
 * Loads spending at the same merchant, grouped by the requested period.
 */
export async function getMerchantHistory(
  id: string,
  query?: {
    granularity?: MerchantHistoryGranularity | string;
  },
): Promise<MerchantHistoryDto> {
  const response = await browserClient.get<MerchantHistoryDto>(
    `/api/transactions/${id}/merchant-history`,
    {
      params: query,
    },
  );

  return response.data;
}

/**
 * POST /api/transactions
 * Records a transaction the household entered by hand.
 */
export async function createManualTransaction(
  dto: CreateManualTransactionDto,
): Promise<TransactionDto> {
  const response = await browserClient.post<TransactionDto>(
    "/api/transactions",
    dto,
  );
  return response.data;
}

/**
 * POST /api/transactions/{id}/archive
 * Hides a transaction without deleting it.
 */
export async function archiveTransaction(id: string): Promise<TransactionDto> {
  const response = await browserClient.post<TransactionDto>(
    `/api/transactions/${id}/archive`,
  );
  return response.data;
}

/**
 * POST /api/transactions/{id}/restore
 * Puts an archived transaction back in the list.
 */
export async function restoreTransaction(id: string): Promise<TransactionDto> {
  const response = await browserClient.post<TransactionDto>(
    `/api/transactions/${id}/restore`,
  );
  return response.data;
}

/**
 * PATCH /api/transactions/{id}
 * Saves edits to a transaction's category, date, notes, or other details.
 */
export async function updateTransactionDetails(
  id: string,
  dto: UpdateTransactionDetailsDto,
): Promise<TransactionDto> {
  const response = await browserClient.patch<TransactionDto>(
    `/api/transactions/${id}`,
    dto,
  );

  return response.data;
}
