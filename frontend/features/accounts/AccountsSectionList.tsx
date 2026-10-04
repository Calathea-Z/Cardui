import { EmptyState } from "@/components/ui/empty-state";
import type { AccountDto, AccountGroupDto } from "@/lib/api/types";
import { AccountListRow } from "./AccountListRow";
import { formatCurrency } from "./formatCurrency";

type AccountsSectionListProps = {
  groups: AccountGroupDto[];
  archivedAccounts?: AccountDto[];
  planningCurrency?: string;
  onSelectAccount?: (account: AccountDto) => void;
};

/**
 * Lists accounts by group, with archived accounts in their own section.
 * The net-worth group and empty groups are omitted, and each group total uses formatCurrency in the planning currency.
 */
export function AccountsSectionList({
  groups,
  archivedAccounts = [],
  planningCurrency = "USD",
  onSelectAccount,
}: AccountsSectionListProps) {
  const detailGroups = groups.filter(
    (group) => group.key !== "net-worth" && group.accounts.length > 0,
  );

  if (detailGroups.length === 0 && archivedAccounts.length === 0) {
    return (
      <EmptyState
        title="No accounts yet"
        description="Use the + button to add an account."
        className="app-panel py-10"
      />
    );
  }

  return (
    <div className="flex flex-col gap-4">
      {detailGroups.map((group) => (
        <section key={group.key} className="app-panel">
          <div className="app-panel-header flex items-center justify-between gap-4 px-4 py-3.5">
            <div className="min-w-0 pl-2">
              <h2 className="app-section-title">{group.name}</h2>
              <p className="app-section-meta">
                {group.accounts.length}{" "}
                {group.accounts.length === 1 ? "account" : "accounts"}
              </p>
            </div>
            <p className="ledger-amount shrink-0 text-xl text-foreground">
              {formatCurrency(group.total, planningCurrency)}
            </p>
          </div>

          <div className="divide-y divide-border/70 bg-background/40">
            {group.accounts.map((account) => (
              <AccountListRow
                key={account.id}
                account={account}
                planningCurrency={planningCurrency}
                onSelect={onSelectAccount}
              />
            ))}
          </div>
        </section>
      ))}

      {archivedAccounts.length > 0 ? (
        <section className="app-panel">
          <div className="app-panel-header px-4 py-3.5">
            <h2 className="app-section-title pl-2">Archived</h2>
            <p className="app-section-meta pl-2">
              Hidden from balances. Transactions stay in activity until you
              archive them too.
            </p>
          </div>
          <div className="divide-y divide-border/70 bg-background/40">
            {archivedAccounts.map((account) => (
              <AccountListRow
                key={account.id}
                account={account}
                planningCurrency={planningCurrency}
                onSelect={onSelectAccount}
              />
            ))}
          </div>
        </section>
      ) : null}
    </div>
  );
}
