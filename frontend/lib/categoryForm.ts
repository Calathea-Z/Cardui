/**
 * Fields the category form edits.
 * An empty `subGroupId` means the category is not in a subgroup.
 */
export type CategoryFormState = {
  name: string;
  color: string;
  icon: string;
  subGroupId: string;
};

/**
 * Blank category form.
 * New categories start on the same green until the household picks a color.
 */
export const emptyCategoryForm: CategoryFormState = {
  name: "",
  color: "#22c55e",
  icon: "",
  subGroupId: "",
};
