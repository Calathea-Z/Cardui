import type { CategoryDto } from "@/lib/api";

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
        <button
          type="button"
          onClick={() => onEdit(category)}
          className="cursor-pointer rounded-md border border-border px-3 py-1.5 text-xs text-muted-foreground transition hover:bg-accent"
        >
          Edit
        </button>

        <button
          type="button"
          disabled={category.isSystem}
          onClick={() => onDelete(category)}
          className="cursor-pointer rounded-md border border-destructive/40 px-3 py-1.5 text-xs text-destructive transition hover:bg-destructive/10 disabled:cursor-not-allowed disabled:opacity-40"
        >
          Delete
        </button>
      </div>
    </div>
  );
}
