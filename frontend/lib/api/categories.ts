import { apiClient } from "./client";
import type { CategoryDto, CreateCategoryDto, UpdateCategoryDto } from "./types";

export async function getCategories(): Promise<CategoryDto[]> {
    const response = await apiClient.get<CategoryDto[]>("/api/categories");

    return response.data;
}

export async function getCategoryById(id: string): Promise<CategoryDto> {
    const response = await apiClient.get<CategoryDto>(`/api/categories/${id}`);
    return response.data;
}

export async function createCategory(createCategoryDto: CreateCategoryDto): Promise<CategoryDto> {
    const response = await apiClient.post<CategoryDto>("/api/categories", createCategoryDto);
    return response.data;
}

export async function updateCategory(id: string, updateCategoryDto: UpdateCategoryDto): Promise<CategoryDto> {
    const response = await apiClient.patch<CategoryDto>(`/api/categories/${id}`, updateCategoryDto);
    return response.data;
}

export async function deleteCategory(id: string): Promise<void> {
    await apiClient.delete(`/api/categories/${id}`);
}
