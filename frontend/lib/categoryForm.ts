export type CategoryFormState = {
  name: string;
  color: string;
  icon: string;
  subGroupId: string;
};

export const emptyCategoryForm: CategoryFormState = {
  name: "",
  color: "#22c55e",
  icon: "",
  subGroupId: "",
};
