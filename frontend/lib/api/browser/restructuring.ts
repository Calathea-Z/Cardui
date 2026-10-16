import { browserClient } from "../browser-client";
import type {
  CompareRestructuringDto,
  RestructuringComparisonDto,
} from "../types";

/**
 * POST /api/plan/restructuring
 * Compares one offer with the selected debts. Nothing is saved.
 */
export async function compareRestructuring(
  dto: CompareRestructuringDto,
): Promise<RestructuringComparisonDto> {
  const response = await browserClient.post<RestructuringComparisonDto>(
    "/api/plan/restructuring",
    dto,
  );
  return response.data;
}
