import { browserClient } from "../browser-client";
import type { DebtDto, DebtSummaryReportDto, UpsertDebtDto } from "../types";

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
 * GET /api/debts/summary
 * Reloads the debt totals, risks, and missing inputs.
 */
export async function getDebtSummary(): Promise<DebtSummaryReportDto> {
  const response =
    await browserClient.get<DebtSummaryReportDto>("/api/debts/summary");
  return response.data;
}

/**
 * POST /api/debts/{id}/use-account-balance
 * Stores the linked account's dated balance on the debt.
 * The account balance is not changed. APR, minimum, and due date stay as they are.
 */
export async function chooseAccountBalance(id: string): Promise<DebtDto> {
  const response = await browserClient.post<DebtDto>(
    `/api/debts/${id}/use-account-balance`,
  );
  return response.data;
}
