import { browserClient } from "../browser-client";
import type {
  CreatePlaidLinkTokenResponse,
  ExchangePlaidPublicTokenRequest,
  ExchangePlaidPublicTokenResponse,
  SyncPlaidItemResponseDto,
} from "../types";

export async function createPlaidLinkToken(): Promise<CreatePlaidLinkTokenResponse> {
  const response = await browserClient.post<CreatePlaidLinkTokenResponse>(
    "/api/plaid/link-token",
  );

  return response.data;
}

export async function exchangePlaidPublicToken(
  dto: ExchangePlaidPublicTokenRequest,
): Promise<ExchangePlaidPublicTokenResponse> {
  const response = await browserClient.post<ExchangePlaidPublicTokenResponse>(
    "/api/plaid/exchange-public-token",
    dto,
  );

  return response.data;
}

export async function syncPlaidItem(
  plaidItemId: string,
): Promise<SyncPlaidItemResponseDto> {
  const response = await browserClient.post<SyncPlaidItemResponseDto>(
    `/api/plaid/${plaidItemId}/sync`,
  );

  return response.data;
}
