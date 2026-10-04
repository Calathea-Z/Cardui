import type { CategoryDto } from "@/lib/api/types";
import { Button } from "@/components/ui/button";
import { getCategoryEmoji } from "./categoryEmoji";

type CategoryRowProps = {
  category: CategoryDto;
  subGroupName: string;
  onEdit: (category: CategoryDto) => void;
  onDelete: (category: CategoryDto) => void;
};

export function CategoryRow({
  category,
  subGroupName,
  onEdit,
  onDelete,
}: CategoryRowProps) {
  return (
    <div className="grid grid-cols-[1fr_140px_120px_160px] items-center gap-4 px-4 py-4 text-sm">
      <div className="flex min-w-0 items-center gap-3">
        <span className="text-base" aria-hidden="true">
          {getCategoryEmoji(category)}
        </span>
        <div className="min-w-0">
          <p className="truncate font-medium">{category.name}</p>
          {category.color ? (
            <p className="truncate text-xs text-muted-foreground">
              {category.color}
            </p>
          ) : null}
        </div>
      </div>

      <span className="truncate text-muted-foreground">{subGroupName}</span>

      <span className="text-muted-foreground">
        {category.isSystem ? "System" : "Custom"}
      </span>

      <div className="flex justify-end gap-2">
        <Button
          type="button"
          variant="outline"
          size="xs"
          disabled={category.isSystem}
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
