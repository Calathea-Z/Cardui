"use client";

import { Check, Plus, Search } from "lucide-react";
import { useMemo, useState } from "react";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Input } from "@/components/ui/input";
import { getCategoryEmoji } from "@/features/categories/categoryEmoji";
import { sortCategoriesByName } from "@/features/categories/categorySort";
import type { CategoryDto, GroupDto, SubGroupDto } from "@/lib/api/types";
import { AddCategoryDrawer } from "./AddCategoryDrawer";
import { FULL_SCREEN_SHEET_CLASSNAME } from "./fullScreenSheet";

type ChangeCategoryDrawerProps = {
  open: boolean;
  categories: CategoryDto[];
  groups: GroupDto[];
  subGroups: SubGroupDto[];
  categoryId: string;
  onClose: () => void;
  onSelect: (categoryId: string) => void;
  onCategoryCreated: (category: CategoryDto) => void;
};

type CategorySection = {
  subGroup: SubGroupDto;
  categories: CategoryDto[];
};

/**
 * Orders subgroups by sort order, then by name.
 * The input list is left unchanged.
 */
function sortSubGroups(subGroups: SubGroupDto[]) {
  return subGroups
    .slice()
    .sort(
      (left, right) =>
        left.sortOrder - right.sortOrder || left.name.localeCompare(right.name),
    );
}

/**
 * Orders groups by sort order, then by name.
 * The input list is left unchanged.
 */
function sortGroups(groups: GroupDto[]) {
  return groups
    .slice()
    .sort(
      (left, right) =>
        left.sortOrder - right.sortOrder || left.name.localeCompare(right.name),
    );
}

/**
 * Lets the household pick a category or start a new one.
 * Closing the sheet while add-category is open dismisses that drawer and leaves the picker open.
 */
export function ChangeCategoryDrawer({
  open,
  categories,
  groups,
  subGroups,
  categoryId,
  onClose,
  onSelect,
  onCategoryCreated,
}: ChangeCategoryDrawerProps) {
  const [isAddCategoryOpen, setIsAddCategoryOpen] = useState(false);

  /**
   * Closes the add-category drawer and the category picker together.
   */
  function handleClose() {
    setIsAddCategoryOpen(false);
    onClose();
  }

  /**
   * Closes the sheet the household is looking at.
   * An open add-category drawer closes first and leaves the picker open.
   */
  function handleSheetClose() {
    if (isAddCategoryOpen) {
      setIsAddCategoryOpen(false);
      return;
    }

    handleClose();
  }

  return (
    <>
      <BottomSheet
        open={open}
        onClose={handleSheetClose}
        title="Change Category"
        headerAction="close"
        closeOnEscape={!isAddCategoryOpen}
        overlayClassName="z-70"
        className={FULL_SCREEN_SHEET_CLASSNAME}
      >
        <ChangeCategoryDrawerContent
          categories={categories}
          groups={groups}
          subGroups={subGroups}
          categoryId={categoryId}
          onSelect={(id) => {
            onSelect(id);
            handleClose();
          }}
          onNewCategory={() => setIsAddCategoryOpen(true)}
        />
      </BottomSheet>

      <AddCategoryDrawer
        open={isAddCategoryOpen}
        groups={groups}
        subGroups={subGroups}
        onClose={() => setIsAddCategoryOpen(false)}
        onCreated={(category) => {
          onCategoryCreated(category);
          onSelect(category.id);
          setIsAddCategoryOpen(false);
          handleClose();
        }}
      />
    </>
  );
}

/**
 * Lists categories in subgroup sections and filters them as the household types.
 * A match can be the category name, the subgroup name, or the group name.
 */
function ChangeCategoryDrawerContent({
  categories,
  groups,
  subGroups,
  categoryId,
  onSelect,
  onNewCategory,
}: {
  categories: CategoryDto[];
  groups: GroupDto[];
  subGroups: SubGroupDto[];
  categoryId: string;
  onSelect: (categoryId: string) => void;
  onNewCategory: () => void;
}) {
  const [search, setSearch] = useState("");
  const query = search.trim().toLowerCase();

  const sections = useMemo(() => {
    const sortedGroups = sortGroups(groups);
    const result: CategorySection[] = [];

    for (const group of sortedGroups) {
      const groupSubGroups = sortSubGroups(
        subGroups.filter((subGroup) => subGroup.groupId === group.id),
      );

      for (const subGroup of groupSubGroups) {
        const sectionCategories = sortCategoriesByName(
          categories.filter((category) => {
            if (category.subGroupId !== subGroup.id) {
              return false;
            }

            if (!query) {
              return true;
            }

            return (
              category.name.toLowerCase().includes(query) ||
              subGroup.name.toLowerCase().includes(query) ||
              group.name.toLowerCase().includes(query)
            );
          }),
        );

        if (sectionCategories.length > 0) {
          result.push({
            subGroup,
            categories: sectionCategories,
          });
        }
      }
    }

    return result;
  }, [categories, groups, query, subGroups]);

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
          placeholder="Search categories"
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
        Add Category
      </button>

      {sections.length === 0 ? (
        <p className="text-sm text-muted-foreground">
          {query ? "No categories match." : "No categories yet."}
        </p>
      ) : (
        <div className="flex flex-col gap-5">
          {sections.map((section) => (
            <section key={section.subGroup.id}>
              <h3 className="px-0.5 text-xs font-semibold tracking-[0.14em] text-muted-foreground uppercase">
                {section.subGroup.name}
              </h3>
              <div className="mt-2 divide-y divide-border/70 border-y border-border/70">
                {section.categories.map((category) => (
                  <CategoryRow
                    key={category.id}
                    category={category}
                    categoryId={categoryId}
                    onSelect={onSelect}
                  />
                ))}
              </div>
            </section>
          ))}
        </div>
      )}
    </div>
  );
}

/**
 * Shows one category and selects it.
 * The category already on the transaction shows a check.
 */
function CategoryRow({
  category,
  categoryId,
  onSelect,
}: {
  category: CategoryDto;
  categoryId: string;
  onSelect: (categoryId: string) => void;
}) {
  const isSelected = category.id === categoryId;

  return (
    <button
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
        <Check className="size-4 shrink-0 text-primary" aria-hidden="true" />
      ) : null}
    </button>
  );
}
