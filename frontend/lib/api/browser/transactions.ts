import { browserClient } from "../browser-client";
import type {
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
