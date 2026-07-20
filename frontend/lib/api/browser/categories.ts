import { browserClient } from "../browser-client";
import type {
  CategoryDto,
  CreateCategoryDto,
  UpdateCategoryDto,
} from "../types";

export async function createCategory(
  createCategoryDto: CreateCategoryDto,
): Promise<CategoryDto> {
  const response = await browserClient.post<CategoryDto>(
    "/api/categories",
    createCategoryDto,
  );

  return response.data;
}

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

export async function deleteCategory(id: string): Promise<void> {
  await browserClient.delete(`/api/categories/${id}`);
}
