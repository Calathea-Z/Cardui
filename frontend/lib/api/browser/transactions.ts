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

export async function getTransactionById(id: string): Promise<TransactionDto> {
  const response = await browserClient.get<TransactionDto>(
    `/api/transactions/${id}`,
  );

  return response.data;
}

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

export async function createManualTransaction(
  dto: CreateManualTransactionDto,
): Promise<TransactionDto> {
  const response = await browserClient.post<TransactionDto>(
    "/api/transactions",
    dto,
  );
  return response.data;
}

export async function archiveTransaction(id: string): Promise<TransactionDto> {
  const response = await browserClient.post<TransactionDto>(
    `/api/transactions/${id}/archive`,
  );
  return response.data;
}

export async function restoreTransaction(id: string): Promise<TransactionDto> {
  const response = await browserClient.post<TransactionDto>(
    `/api/transactions/${id}/restore`,
  );
  return response.data;
}

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
