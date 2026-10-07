import { browserClient } from "../browser-client";
import type {
  DebtDto,
  DebtFollowAccountDto,
  DebtSummaryReportDto,
  FollowDebtAccountDto,
  UpsertDebtDto,
} from "../types";

/**
 * POST /api/debts
 * Records a debt for the signed-in household.
 * A blank term is stored as unknown. The linked account is omitted when the debt is not tied to one.
 */
export async function createDebt(dto: UpsertDebtDto): Promise<DebtDto> {
  const response = await browserClient.post<DebtDto>("/api/debts", dto);
  return response.data;
}

/**
 * PUT /api/debts/{id}
 * Updates a debt. The stored currency stays.
 */
export async function updateDebt(
  id: string,
  dto: UpsertDebtDto,
): Promise<DebtDto> {
  const response = await browserClient.put<DebtDto>(`/api/debts/${id}`, dto);
  return response.data;
}

/**
 * DELETE /api/debts/{id}
 * Deletes a debt. The linked account and its balance stay unchanged.
 */
export async function deleteDebt(id: string): Promise<void> {
  await browserClient.delete(`/api/debts/${id}`);
}

/**
 * GET /api/debts
 * Reloads the household's debts, including a followed balance.
 */
export async function getDebts(): Promise<DebtDto[]> {
  const response = await browserClient.get<DebtDto[]>("/api/debts");
  return response.data;
}

/**
 * GET /api/debts/summary
 * Reloads the debt totals, risks, and missing inputs.
 * A followed balance is the one in the totals.
 */
export async function getDebtSummary(): Promise<DebtSummaryReportDto> {
  const response =
    await browserClient.get<DebtSummaryReportDto>("/api/debts/summary");
  return response.data;
}

/**
 * POST /api/debts/{id}/use-account-balance
 * Uses the linked account's balance. An eligible account starts a follow.
 * An account that cannot be followed is copied onto the debt once.
 */
export async function chooseAccountBalance(id: string): Promise<DebtDto> {
  const response = await browserClient.post<DebtDto>(
    `/api/debts/${id}/use-account-balance`,
  );
  return response.data;
}

/**
 * GET /api/debts/{id}/follow-accounts
 * Lists the connected credit cards and loans this debt may follow.
 */
export async function getDebtFollowAccounts(
  id: string,
): Promise<DebtFollowAccountDto[]> {
  const response = await browserClient.get<DebtFollowAccountDto[]>(
    `/api/debts/${id}/follow-accounts`,
  );
  return response.data;
}

/**
 * POST /api/debts/{id}/follow
 * Makes the debt follow one connected account.
 * The stored balance stays. keepOwnBalance records a different amount as the person's value.
 */
export async function followDebtAccount(
  id: string,
  dto: FollowDebtAccountDto,
): Promise<DebtDto> {
  const response = await browserClient.post<DebtDto>(
    `/api/debts/${id}/follow`,
    dto,
  );
  return response.data;
}

/**
 * POST /api/debts/{id}/stop-following
 * Stops following and keeps the last balance on the debt. The account stays linked.
 */
export async function stopFollowingDebt(id: string): Promise<DebtDto> {
  const response = await browserClient.post<DebtDto>(
    `/api/debts/${id}/stop-following`,
  );
  return response.data;
}
