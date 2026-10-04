import { browserClient } from "../browser-client";
import type {
  AccountDto,
  BalanceReconciliationResultDto,
  CreateManualAccountDto,
  ReconcileAccountBalanceDto,
  UpdateManualAccountDto,
} from "../types";

/**
 * POST /api/accounts
 * Creates a manual account for the signed-in household.
 */
export async function createManualAccount(
  dto: CreateManualAccountDto,
): Promise<AccountDto> {
  const response = await browserClient.post<AccountDto>("/api/accounts", dto);
  return response.data;
}

/**
 * PATCH /api/accounts/{id}
 * Updates a manual account. Linked Plaid accounts are not edited here.
 */
export async function updateManualAccount(
  id: string,
  dto: UpdateManualAccountDto,
): Promise<AccountDto> {
  const response = await browserClient.patch<AccountDto>(
    `/api/accounts/${id}`,
    dto,
  );
  return response.data;
}

/**
 * POST /api/accounts/{id}/archive
 * Hides an account without deleting its transactions.
 */
export async function archiveAccount(id: string): Promise<AccountDto> {
  const response = await browserClient.post<AccountDto>(
    `/api/accounts/${id}/archive`,
  );
  return response.data;
}

/**
 * POST /api/accounts/{id}/restore
 * Puts an archived account back in the active list.
 */
export async function restoreAccount(id: string): Promise<AccountDto> {
  const response = await browserClient.post<AccountDto>(
    `/api/accounts/${id}/restore`,
  );
  return response.data;
}

/**
 * POST /api/accounts/{id}/reconciliation
 * Sets the account balance and records the difference as an adjustment.
 */
export async function reconcileAccountBalance(
  id: string,
  dto: ReconcileAccountBalanceDto,
): Promise<BalanceReconciliationResultDto> {
  const response = await browserClient.post<BalanceReconciliationResultDto>(
    `/api/accounts/${id}/reconciliation`,
    dto,
  );
  return response.data;
}
