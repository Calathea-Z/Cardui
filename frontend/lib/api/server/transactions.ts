import { serverClient } from "../server-client";
import type {
  MerchantHistoryDto,
  MerchantHistoryGranularity,
  PagedResultDto,
  TransactionDto,
  TransactionQueryDto,
  UpdateTransactionCategoryDto,
  UpdateTransactionDetailsDto,
} from "../types";

export async function getTransactions(
  query?: TransactionQueryDto,
): Promise<PagedResultDto<TransactionDto>> {
  const response = await serverClient.get<PagedResultDto<TransactionDto>>(
    "/api/transactions",
    {
      params: query,
    },
  );

  return response.data;
}

export async function getTransactionById(id: string): Promise<TransactionDto> {
  const response = await serverClient.get<TransactionDto>(
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
  const response = await serverClient.get<MerchantHistoryDto>(
    `/api/transactions/${id}/merchant-history`,
    {
      params: query,
    },
  );

  return response.data;
}

export async function updateTransactionCategory(
  id: string,
  dto: UpdateTransactionCategoryDto,
): Promise<TransactionDto> {
  const response = await serverClient.patch<TransactionDto>(
    `/api/transactions/${id}/category`,
    dto,
  );

  return response.data;
}

export async function updateTransactionDetails(
  id: string,
  dto: UpdateTransactionDetailsDto,
): Promise<TransactionDto> {
  const response = await serverClient.patch<TransactionDto>(
    `/api/transactions/${id}`,
    dto,
  );

  return response.data;
}
