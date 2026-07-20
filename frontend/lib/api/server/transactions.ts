import { serverClient } from "../server-client";
import type {
  PagedResultDto,
  TransactionDto,
  TransactionQueryDto,
  UpdateTransactionCategoryDto,
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
