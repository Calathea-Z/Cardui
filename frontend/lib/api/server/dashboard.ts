import { serverClient } from "../server-client";
import type { DashboardSummaryDto } from "../types";

/**
 * GET /api/dashboard/summary
 * Loads the current-period totals for the dashboard.
 */
export async function getDashboardSummary(): Promise<DashboardSummaryDto> {
  const response = await serverClient.get<DashboardSummaryDto>(
    "/api/dashboard/summary",
  );

  return response.data;
}
