import type { ObligationDto } from "../types";
import { serverClient } from "../server-client";

/**
 * GET /api/obligations
 * Loads the household's bills. Each amount is one payment.
 */
export async function getObligations(): Promise<ObligationDto[]> {
  const response = await serverClient.get<ObligationDto[]>("/api/obligations");
  return response.data;
}
