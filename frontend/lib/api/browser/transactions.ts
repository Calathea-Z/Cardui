import { browserClient } from "../browser-client";
import type {
  PagedResultDto,
  TransactionDto,
  TransactionQueryDto,
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
