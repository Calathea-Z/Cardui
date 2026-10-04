import { serverClient } from "../server-client";
import type { CategoryDto } from "../types";

/**
 * GET /api/categories
 * Lists categories for server-rendered pages.
 */
export async function getCategories(): Promise<CategoryDto[]> {
  const response = await serverClient.get<CategoryDto[]>("/api/categories");

  return response.data;
}
