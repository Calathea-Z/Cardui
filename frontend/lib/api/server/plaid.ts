import { serverClient } from "../server-client";
import type { PlaidItemDto } from "../types";

/**
 * GET /api/plaid/items
 * Lists connected institutions for the institutions page.
 */
export async function getPlaidItems(): Promise<PlaidItemDto[]> {
  const response = await serverClient.get<PlaidItemDto[]>("/api/plaid/items");

  return response.data;
}
