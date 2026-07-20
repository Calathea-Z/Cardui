import { serverClient } from "../server-client";
import type { ApiHealthDto } from "../types";

export async function checkApiHealth(): Promise<ApiHealthDto> {
  const response = await serverClient.get<ApiHealthDto>("/api/health", {
    timeout: 3_000,
  });

  return response.data;
}
