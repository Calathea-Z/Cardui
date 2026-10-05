import type { DebtDto } from "../types";
import { serverClient } from "../server-client";

/**
 * GET /api/debts
 * Loads the household's debts. Unknown terms stay null.
 */
export async function getDebts(): Promise<DebtDto[]> {
  const response = await serverClient.get<DebtDto[]>("/api/debts");
  return response.data;
}
