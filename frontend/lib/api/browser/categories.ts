import { browserClient } from "../browser-client";
import type {
  CategoryDto,
  CreateCategoryDto,
  UpdateCategoryDto,
} from "../types";

/**
 * POST /api/categories
 * Creates a household category.
 */
export async function createCategory(
  createCategoryDto: CreateCategoryDto,
): Promise<CategoryDto> {
  const response = await browserClient.post<CategoryDto>(
    "/api/categories",
    createCategoryDto,
  );

  return response.data;
}

/**
 * PATCH /api/categories/{id}
 * Updates a household category's name, color, icon, or subgroup.
 */
export async function updateCategory(
  id: string,
  updateCategoryDto: UpdateCategoryDto,
): Promise<CategoryDto> {
  const response = await browserClient.patch<CategoryDto>(
    `/api/categories/${id}`,
    updateCategoryDto,
  );

  return response.data;
}

/**
 * DELETE /api/categories/{id}
 * Removes a household category.
 */
export async function deleteCategory(id: string): Promise<void> {
  await browserClient.delete(`/api/categories/${id}`);
}
