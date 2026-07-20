import { serverClient } from "../server-client";
import type { PlaidItemDto } from "../types";

export async function getPlaidItems(): Promise<PlaidItemDto[]> {
  const response = await serverClient.get<PlaidItemDto[]>("/api/plaid/items");

  return response.data;
}
