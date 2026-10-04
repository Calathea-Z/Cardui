import { serverClient } from "../server-client";
import type { CategoryDto } from "../types";

export async function getCategories(): Promise<CategoryDto[]> {
  const response = await serverClient.get<CategoryDto[]>("/api/categories");

  return response.data;
}
