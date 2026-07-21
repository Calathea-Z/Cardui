"use client";

import { Check, Plus, Search } from "lucide-react";
import { useMemo, useState } from "react";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Input } from "@/components/ui/input";
import { getCategoryEmoji } from "@/features/categories/categoryEmoji";
import { sortCategoriesByName } from "@/features/categories/categorySort";
import type { CategoryDto } from "@/lib/api/types";
import { AddCategoryDrawer } from "./AddCategoryDrawer";
import { FULL_SCREEN_SHEET_CLASSNAME } from "./fullScreenSheet";

type ChangeCategoryDrawerProps = {
  open: boolean;
  categories: CategoryDto[];
  categoryId: string;
  onClose: () => void;
  onSelect: (categoryId: string) => void;
  onCategoryCreated: (category: CategoryDto) => void;
};

export function ChangeCategoryDrawer({
  open,
  categories,
  categoryId,
  onClose,
  onSelect,
  onCategoryCreated,
}: ChangeCategoryDrawerProps) {
  const [isAddOpen, setIsAddOpen] = useState(false);

  function handleClose() {
    setIsAddOpen(false);
    onClose();
  }

  return (
    <>
      <BottomSheet
        open={open}
        onClose={handleClose}
        title="Change Category"
        headerAction="close"
        closeOnEscape={!isAddOpen}
        overlayClassName="z-70"
        className={FULL_SCREEN_SHEET_CLASSNAME}
      >
        {open ? (
          <ChangeCategoryDrawerContent
            categories={categories}
            categoryId={categoryId}
            onSelect={(id) => {
              onSelect(id);
              handleClose();
            }}
            onNewCategory={() => setIsAddOpen(true)}
          />
        ) : null}
      </BottomSheet>

      <AddCategoryDrawer
        open={isAddOpen}
        onClose={() => setIsAddOpen(false)}
        onCreated={(category) => {
          onCategoryCreated(category);
          onSelect(category.id);
          setIsAddOpen(false);
          onClose();
        }}
      />
    </>
  );
}

function ChangeCategoryDrawerContent({
  categories,
  categoryId,
  onSelect,
  onNewCategory,
}: {
  categories: CategoryDto[];
  categoryId: string;
  onSelect: (categoryId: string) => void;
  onNewCategory: () => void;
}) {
  const [search, setSearch] = useState("");
  const sortedCategories = useMemo(
    () => sortCategoriesByName(categories),
    [categories],
  );

  const filteredCategories = useMemo(() => {
    const query = search.trim().toLowerCase();
    if (!query) {
      return sortedCategories;
    }

    return sortedCategories.filter((category) =>
      category.name.toLowerCase().includes(query),
    );
  }, [search, sortedCategories]);

  return (
    <div className="flex flex-col gap-4">
      <label className="relative block">
        <Search
          className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground"
          aria-hidden="true"
        />
        <Input
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Search"
          className="h-10 pl-9"
          autoFocus
        />
      </label>

      <button
        type="button"
        onClick={onNewCategory}
        className="flex min-h-12 w-full items-center gap-3 text-left text-sm font-medium text-transfer"
      >
        <Plus className="size-4 shrink-0" aria-hidden="true" />
        New Category
      </button>

      <div className="divide-y divide-border/70 border-y border-border/70">
        <button
          type="button"
          onClick={() => onSelect("")}
          className="flex min-h-12 w-full items-center gap-3 py-3 text-left"
        >
          <span className="text-base" aria-hidden="true">
            {getCategoryEmoji(null)}
          </span>
          <span className="min-w-0 flex-1 truncate text-sm font-medium">
            None
          </span>
          {!categoryId ? (
            <Check className="size-4 shrink-0 text-primary" aria-hidden="true" />
          ) : null}
        </button>

        {filteredCategories.map((category) => {
          const isSelected = category.id === categoryId;

          return (
            <button
              key={category.id}
              type="button"
              onClick={() => onSelect(category.id)}
              className="flex min-h-12 w-full items-center gap-3 py-3 text-left"
            >
              <span className="text-base" aria-hidden="true">
                {getCategoryEmoji(category)}
              </span>
              <span className="min-w-0 flex-1 truncate text-sm font-medium">
                {category.name}
              </span>
              {isSelected ? (
                <Check
                  className="size-4 shrink-0 text-primary"
                  aria-hidden="true"
                />
              ) : null}
            </button>
          );
        })}
      </div>

      {filteredCategories.length === 0 ? (
        <p className="text-sm text-muted-foreground">No categories match.</p>
      ) : null}
    </div>
  );
}
