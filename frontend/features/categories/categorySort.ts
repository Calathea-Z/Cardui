import type { CategoryDto } from "@/lib/api/types";

export function sortCategoriesByName(categories: CategoryDto[]) {
  return categories.slice().sort((a, b) => a.name.localeCompare(b.name));
}
