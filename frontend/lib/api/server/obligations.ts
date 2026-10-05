import type { ObligationDto, ObligationSuggestionDto } from "../types";
import { serverClient } from "../server-client";

/**
 * GET /api/obligations
 * Loads the household's bills. Each amount is one payment.
 */
export async function getObligations(): Promise<ObligationDto[]> {
  const response = await serverClient.get<ObligationDto[]>("/api/obligations");
  return response.data;
}

/**
 * GET /api/obligations/suggestions
 * Loads recurring payments noticed in activity. They are not bills.
 */
export async function getObligationSuggestions(): Promise<
  ObligationSuggestionDto[]
> {
  const response = await serverClient.get<ObligationSuggestionDto[]>(
    "/api/obligations/suggestions",
  );
  return response.data;
}
