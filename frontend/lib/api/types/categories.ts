/**
 * A category the household can assign to transactions.
 * `isSystem` marks a built-in category. `key` is the stable id used for emoji and activity rules.
 */
export type CategoryDto = {
  id: string;
  name: string;
  key: string;
  subGroupId: string;
  color: string | null;
  icon: string | null;
  isSystem: boolean;
};

export type CreateCategoryDto = {
  name: string;
  subGroupId: string;
  color?: string | null;
  icon?: string | null;
};

export type UpdateCategoryDto = {
  name: string;
  subGroupId: string;
  color?: string | null;
  icon?: string | null;
};

export type GroupDto = {
  id: string;
  key: string;
  name: string;
  sortOrder: number;
};

export type SubGroupDto = {
  id: string;
  groupId: string;
  key: string;
  name: string;
  isSystem: boolean;
  sortOrder: number;
};
