"use client";

import { ChevronDown } from "lucide-react";
import { useState } from "react";
import { sortCategoriesByName } from "@/features/categories/categorySort";
import type { CategoryDto } from "@/lib/api/types";
import { cn } from "@/lib/utils";
import { ChangeCategoryDrawer } from "./ChangeCategoryDrawer";

type TransactionCategorySelectProps = {
  categories: CategoryDto[];
  categoryId: string;
  onCategoryIdChange: (categoryId: string) => void;
  onCategoryCreated: (category: CategoryDto) => void;
  disabled?: boolean;
  onOpenChange?: (open: boolean) => void;
};

export function TransactionCategorySelect({
  categories,
  categoryId,
  onCategoryIdChange,
  onCategoryCreated,
  disabled = false,
  onOpenChange,
}: TransactionCategorySelectProps) {
  const [isOpen, setIsOpen] = useState(false);
  const sortedCategories = sortCategoriesByName(categories);
  const selectedCategory = sortedCategories.find(
    (category) => category.id === categoryId,
  );
  const displayValue = selectedCategory?.name ?? "None";

  function setOpen(open: boolean) {
    setIsOpen(open);
    onOpenChange?.(open);
  }

  return (
    <>
      <button
        type="button"
        disabled={disabled}
        onClick={() => setOpen(true)}
        className={cn(
          "flex min-h-12 w-full items-center gap-3 text-left",
          disabled && "opacity-50",
        )}
      >
        <span className="shrink-0 text-sm font-medium text-foreground">
          Category
        </span>
        <span className="flex min-w-0 flex-1 items-center justify-end gap-1.5 text-sm text-muted-foreground">
          <span className="truncate">{displayValue}</span>
          <ChevronDown className="size-4 shrink-0" aria-hidden="true" />
        </span>
      </button>

      <ChangeCategoryDrawer
        open={isOpen}
        categories={categories}
        categoryId={categoryId}
        onClose={() => setOpen(false)}
        onSelect={onCategoryIdChange}
        onCategoryCreated={onCategoryCreated}
      />
    </>
  );
}
