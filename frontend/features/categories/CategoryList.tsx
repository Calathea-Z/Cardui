import { EmptyState } from "@/components/ui/empty-state";
import type { CategoryDto } from "@/lib/api/types";
import { CategoryRow } from "./CategoryRow";

type CategoryListProps = {
  categories: CategoryDto[];
  onEdit: (category: CategoryDto) => void;
  onDelete: (category: CategoryDto) => void;
};

export function CategoryList({
  categories,
  onEdit,
  onDelete,
}: CategoryListProps) {
  return (
    <div className="app-panel overflow-hidden">
      <div className="grid grid-cols-[1fr_120px_160px] gap-4 border-b border-border px-4 py-3 text-sm font-medium text-muted-foreground">
        <span>Name</span>
        <span>Type</span>
        <span className="text-right">Actions</span>
      </div>

      <div className="divide-y divide-border/70">
        {categories.map((category) => (
          <CategoryRow
            key={category.id}
            category={category}
            onEdit={onEdit}
            onDelete={onDelete}
          />
        ))}

        {categories.length === 0 ? (
          <EmptyState
            title="No categories yet."
            className="py-12 [&_p]:text-sm [&_p]:font-normal [&_p]:text-muted-foreground"
          />
        ) : null}
      </div>
    </div>
  );
}
