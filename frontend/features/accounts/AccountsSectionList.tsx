import { EmptyState } from "@/components/ui/empty-state";
import type { AccountGroupDto } from "@/lib/api/types";
import { AccountListRow } from "./AccountListRow";
import { formatCurrency } from "./formatCurrency";

type AccountsSectionListProps = {
  groups: AccountGroupDto[];
};

export function AccountsSectionList({ groups }: AccountsSectionListProps) {
  const detailGroups = groups.filter(
    (group) => group.key !== "net-worth" && group.accounts.length > 0,
  );

  if (detailGroups.length === 0) {
    return (
      <EmptyState
        title="No accounts connected yet"
        description="Use the + button to connect a bank."
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
              {formatCurrency(group.total)}
            </p>
          </div>

          <div className="divide-y divide-border/70 bg-background/40">
            {group.accounts.map((account) => (
              <AccountListRow key={account.id} account={account} />
            ))}
          </div>
        </section>
      ))}
    </div>
  );
}
