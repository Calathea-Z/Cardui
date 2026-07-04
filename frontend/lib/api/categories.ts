import { apiClient } from "./client";
import type { CategoryDto } from "./types";

export async function getCategories(): Promise<CategoryDto[]> {
    const response = await apiClient.get<CategoryDto[]>("/api/categories");

    return response.data;
}