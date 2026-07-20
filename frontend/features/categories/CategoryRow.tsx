import type { CategoryDto } from "@/lib/api/types";
import { Button } from "@/components/ui/button";

type CategoryRowProps = {
  category: CategoryDto;
  onEdit: (category: CategoryDto) => void;
  onDelete: (category: CategoryDto) => void;
};

export function CategoryRow({ category, onEdit, onDelete }: CategoryRowProps) {
  return (
    <div className="grid grid-cols-[1fr_120px_160px] items-center gap-4 px-4 py-4 text-sm">
      <div className="flex min-w-0 items-center gap-3">
        <span
          className="h-3 w-3 rounded-full"
          style={{ backgroundColor: category.color ?? "#64748b" }}
        />
        <div className="min-w-0">
          <p className="truncate font-medium">{category.name}</p>
          {category.icon ? (
            <p className="text-xs text-muted-foreground">Icon: {category.icon}</p>
          ) : null}
        </div>
      </div>

      <span className="text-muted-foreground">
        {category.isSystem ? "System" : "Custom"}
      </span>

      <div className="flex justify-end gap-2">
        <Button
          type="button"
          variant="outline"
          size="xs"
          onClick={() => onEdit(category)}
        >
          Edit
        </Button>

        <Button
          type="button"
          variant="destructive"
          size="xs"
          disabled={category.isSystem}
          onClick={() => onDelete(category)}
        >
          Delete
        </Button>
      </div>
    </div>
  );
}
