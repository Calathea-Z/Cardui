import type { AccountDto } from "@/lib/api/types";

/**
 * Groups active accounts under the bank that synced them.
 * Only accounts with a Plaid item id are included.
 */
export function groupAccountsByInstitution(accounts: AccountDto[]) {
  return accounts.reduce<Map<string, AccountDto[]>>((groups, account) => {
    if (!account.plaidItemId || !account.isActive) {
      return groups;
    }

    const existing = groups.get(account.plaidItemId) ?? [];
    existing.push(account);
    groups.set(account.plaidItemId, existing);
    return groups;
  }, new Map());
}
