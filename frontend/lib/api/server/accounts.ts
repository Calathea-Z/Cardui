import { serverClient } from "../server-client";
import type { AccountDto, AccountSummaryDto } from "../types";

export async function getAccounts(): Promise<AccountDto[]> {
  const response = await serverClient.get<AccountDto[]>("/api/accounts");

  return response.data;
}

export async function getAccountsSummary(): Promise<AccountSummaryDto> {
  const response = await serverClient.get<AccountSummaryDto>(
    "/api/accounts/summary",
  );

  return response.data;
}
