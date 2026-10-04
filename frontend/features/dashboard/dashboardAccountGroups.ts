import type { AccountGroupDto } from "@/lib/api/types";

/**
 * Account groups that count as assets on the dashboard.
 */
export const ASSET_GROUP_KEYS = ["cash", "investments"] as const;
/**
 * Account groups that count as liabilities on the dashboard.
 */
export const LIABILITY_GROUP_KEYS = ["credit-cards", "loans"] as const;

/**
 * Finds one account group by its key.
 */
export function getAccountGroup(
  groups: AccountGroupDto[],
  key: string,
): AccountGroupDto | undefined {
  return groups.find((group) => group.key === key);
}

/**
 * Returns the requested groups in key order, skipping any the API did not send.
 */
export function getAccountGroups(
  groups: AccountGroupDto[],
  keys: readonly string[],
): AccountGroupDto[] {
  return keys
    .map((key) => getAccountGroup(groups, key))
    .filter((group): group is AccountGroupDto => group !== undefined);
}

/**
 * Adds the group totals. Callers use this for the asset and liability headings.
 */
export function sumGroupTotals(groups: AccountGroupDto[]): number {
  return groups.reduce((total, group) => total + group.total, 0);
}
