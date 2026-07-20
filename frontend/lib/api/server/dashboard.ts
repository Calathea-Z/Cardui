import { serverClient } from "../server-client";
import type { DashboardSummaryDto } from "../types";

export async function getDashboardSummary(): Promise<DashboardSummaryDto> {
  const response = await serverClient.get<DashboardSummaryDto>(
    "/api/dashboard/summary",
  );

  return response.data;
}
