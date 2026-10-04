import { serverClient } from "../server-client";
import type { AccountDto, AccountSummaryDto } from "../types";

/**
 * GET /api/accounts
 * Lists the household's accounts for server-rendered pages.
 */
export async function getAccounts(): Promise<AccountDto[]> {
  const response = await serverClient.get<AccountDto[]>("/api/accounts");

  return response.data;
}

/**
 * GET /api/accounts/summary
 * Loads net worth, balance history, and account groups for the accounts page.
 */
export async function getAccountsSummary(): Promise<AccountSummaryDto> {
  const response = await serverClient.get<AccountSummaryDto>(
    "/api/accounts/summary",
  );

  return response.data;
}
