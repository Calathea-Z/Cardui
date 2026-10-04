import type { CategoryDto } from "@/lib/api/types";

/**
 * Returns categories in name order without changing the original list.
 */
export function sortCategoriesByName(categories: CategoryDto[]) {
  return categories.slice().sort((a, b) => a.name.localeCompare(b.name));
}
