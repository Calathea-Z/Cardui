import { apiClient } from "./client";
import type { DashboardSummaryDto } from "./types";

export async function getDashboardSummary(): Promise<DashboardSummaryDto> {
    const response = await apiClient.get<DashboardSummaryDto>(
        "/api/dashboard/summary",
    );

    return response.data;
}