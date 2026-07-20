"use client";

import type { CategoryDto } from "@/lib/api/types";
import { CategoryForm } from "./CategoryForm";
import { CategoryList } from "./CategoryList";
import { useCategoriesManager } from "./useCategoriesManager";

type CategoriesClientProps = {
  initialCategories: CategoryDto[];
};

export function CategoriesClient({ initialCategories }: CategoriesClientProps) {
  const {
    form,
    setForm,
    sortedCategories,
    editingCategory,
    error,
    isSaving,
    handleSubmit,
    startEditing,
    cancelEditing,
    handleDelete,
  } = useCategoriesManager(initialCategories);

  return (
    <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
      <div>
        <p className="text-sm text-muted-foreground">Organize spending</p>
        <h1 className="text-3xl font-semibold text-foreground">Categories</h1>
      </div>

      <div className="grid gap-6 lg:grid-cols-[360px_1fr]">
        <CategoryForm
          form={form}
          onFormChange={setForm}
          editingCategory={editingCategory}
          error={error}
          isSaving={isSaving}
          onSubmit={handleSubmit}
          onCancel={cancelEditing}
        />

        <CategoryList
          categories={sortedCategories}
          onEdit={startEditing}
          onDelete={handleDelete}
        />
      </div>
    </section>
  );
}
