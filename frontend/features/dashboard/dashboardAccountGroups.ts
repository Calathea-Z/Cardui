import type { AccountGroupDto } from "@/lib/api/types";

export const ASSET_GROUP_KEYS = ["cash", "investments"] as const;
export const LIABILITY_GROUP_KEYS = ["credit-cards", "loans"] as const;

export function getAccountGroup(
  groups: AccountGroupDto[],
  key: string,
): AccountGroupDto | undefined {
  return groups.find((group) => group.key === key);
}

export function getAccountGroups(
  groups: AccountGroupDto[],
  keys: readonly string[],
): AccountGroupDto[] {
  return keys
    .map((key) => getAccountGroup(groups, key))
    .filter((group): group is AccountGroupDto => group !== undefined);
}

export function sumGroupTotals(groups: AccountGroupDto[]): number {
  return groups.reduce((total, group) => total + group.total, 0);
}
