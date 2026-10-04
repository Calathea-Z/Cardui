import { serverClient } from "../server-client";
import type {
  PagedResultDto,
  TransactionDto,
  TransactionQueryDto,
} from "../types";

/**
 * GET /api/transactions
 * Lists one page of transactions for server-rendered pages.
 */
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
