"use client";

import type { CategoryDto, GroupDto, SubGroupDto } from "@/lib/api/types";
import { CategoryForm } from "./CategoryForm";
import { CategoryList } from "./CategoryList";
import { useCategoriesManager } from "./useCategoriesManager";

type CategoriesClientProps = {
  initialCategories: CategoryDto[];
  groups: GroupDto[];
  subGroups: SubGroupDto[];
};

/**
 * Category manager.
 * Shows the category form beside the list of categories.
 */
export function CategoriesClient({
  initialCategories,
  groups,
  subGroups: initialSubGroups,
}: CategoriesClientProps) {
  const {
    form,
    setForm,
    sortedCategories,
    subGroups,
    editingCategory,
    error,
    isSaving,
    handleSubmit,
    startEditing,
    cancelEditing,
    handleDelete,
  } = useCategoriesManager(initialCategories, initialSubGroups);

  return (
    <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
      <h1 className="text-[1.75rem] font-semibold leading-none tracking-[-0.02em] text-foreground">
        Categories
      </h1>

      <div className="grid gap-6 lg:grid-cols-[360px_1fr]">
        <CategoryForm
          form={form}
          onFormChange={setForm}
          editingCategory={editingCategory}
          groups={groups}
          subGroups={subGroups}
          error={error}
          isSaving={isSaving}
          onSubmit={handleSubmit}
          onCancel={cancelEditing}
        />

        <CategoryList
          categories={sortedCategories}
          subGroups={subGroups}
          onEdit={startEditing}
          onDelete={handleDelete}
        />
      </div>
    </section>
  );
}
