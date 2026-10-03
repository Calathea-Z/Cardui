import { browserClient } from "../browser-client";
import type {
  AccountDto,
  BalanceReconciliationResultDto,
  CreateManualAccountDto,
  ReconcileAccountBalanceDto,
  UpdateManualAccountDto,
} from "../types";

export async function createManualAccount(
  dto: CreateManualAccountDto,
): Promise<AccountDto> {
  const response = await browserClient.post<AccountDto>("/api/accounts", dto);
  return response.data;
}

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

export async function archiveAccount(id: string): Promise<AccountDto> {
  const response = await browserClient.post<AccountDto>(
    `/api/accounts/${id}/archive`,
  );
  return response.data;
}

export async function restoreAccount(id: string): Promise<AccountDto> {
  const response = await browserClient.post<AccountDto>(
    `/api/accounts/${id}/restore`,
  );
  return response.data;
}

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
