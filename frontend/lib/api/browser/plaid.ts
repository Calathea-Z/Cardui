import { browserClient } from "../browser-client";
import type {
  CreatePlaidLinkTokenResponse,
  ExchangePlaidPublicTokenRequest,
  ExchangePlaidPublicTokenResponse,
  SyncPlaidItemResponseDto,
} from "../types";

/**
 * POST /api/plaid/link-token
 * Starts a Plaid Link session for the signed-in household.
 */
export async function createPlaidLinkToken(): Promise<CreatePlaidLinkTokenResponse> {
  const response = await browserClient.post<CreatePlaidLinkTokenResponse>(
    "/api/plaid/link-token",
  );

  return response.data;
}

/**
 * POST /api/plaid/exchange-public-token
 * Turns a completed Link public token into a stored institution connection.
 */
export async function exchangePlaidPublicToken(
  dto: ExchangePlaidPublicTokenRequest,
): Promise<ExchangePlaidPublicTokenResponse> {
  const response = await browserClient.post<ExchangePlaidPublicTokenResponse>(
    "/api/plaid/exchange-public-token",
    dto,
  );

  return response.data;
}

/**
 * POST /api/plaid/{plaidItemId}/sync
 * Pulls the latest accounts and transactions for one institution.
 */
export async function syncPlaidItem(
  plaidItemId: string,
): Promise<SyncPlaidItemResponseDto> {
  const response = await browserClient.post<SyncPlaidItemResponseDto>(
    `/api/plaid/${plaidItemId}/sync`,
  );

  return response.data;
}

/**
 * DELETE /api/plaid/{plaidItemId}
 * Removes the bank login at Plaid and the stored access token.
 * Accounts and transactions stay in Cardui.
 */
export async function disconnectPlaidItem(plaidItemId: string): Promise<void> {
  await browserClient.delete(`/api/plaid/${plaidItemId}`);
}
