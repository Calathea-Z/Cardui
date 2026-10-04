import type { CategoryDto } from "@/lib/api/types";

/**
 * Returns categories in name order without changing the original list.
 */
export function sortCategoriesByName(categories: CategoryDto[]) {
  return categories.slice().sort((a, b) => a.name.localeCompare(b.name));
}

/**
 * Orders items by sort order, then by name.
 * The input list is left unchanged.
 */
export function sortByOrderThenName<
  T extends { sortOrder: number; name: string },
>(items: T[]) {
  return items
    .slice()
    .sort(
      (left, right) =>
        left.sortOrder - right.sortOrder || left.name.localeCompare(right.name),
    );
}
