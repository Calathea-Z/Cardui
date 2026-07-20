import { apiClient } from "./client";

export type ApiHealthDto = {
  status: string;
};

export async function checkApiHealth(): Promise<ApiHealthDto> {
  const response = await apiClient.get<ApiHealthDto>("/api/health", {
    timeout: 3_000,
  });

  return response.data;
}
