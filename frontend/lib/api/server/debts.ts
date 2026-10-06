import type { DebtDto, DebtSummaryReportDto } from "../types";
import { serverClient } from "../server-client";

/**
 * GET /api/debts
 * Loads the household's debts. Unknown terms stay null.
 */
export async function getDebts(): Promise<DebtDto[]> {
  const response = await serverClient.get<DebtDto[]>("/api/debts");
  return response.data;
}

/**
 * GET /api/debts/summary
 * Loads the debt totals, risks, and missing inputs.
 * A linked account balance is included when it differs. It is not copied onto the debt.
 */
export async function getDebtSummary(): Promise<DebtSummaryReportDto> {
  const response =
    await serverClient.get<DebtSummaryReportDto>("/api/debts/summary");
  return response.data;
}
