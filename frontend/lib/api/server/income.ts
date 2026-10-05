import type { IncomeSourceDto } from "../types";
import { serverClient } from "../server-client";

/**
 * GET /api/income-sources
 * Loads the household's income sources.
 */
export async function getIncomeSources(): Promise<IncomeSourceDto[]> {
  const response = await serverClient.get<IncomeSourceDto[]>(
    "/api/income-sources",
  );
  return response.data;
}
