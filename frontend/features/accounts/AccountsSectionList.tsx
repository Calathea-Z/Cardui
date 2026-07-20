import type { AccountGroupDto } from "@/lib/api";
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
      <div className="app-panel p-6 text-center text-sm text-muted-foreground">
        No accounts connected yet. Use the + button to connect a bank.
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-4">
      {detailGroups.map((group) => (
        <section key={group.key} className="app-panel">
          <div className="app-panel-header flex items-center justify-between gap-4 px-4 py-3.5">
            <div className="min-w-0 border-l-2 border-primary/70 pl-3">
              <h2 className="app-section-title">{group.name}</h2>
              <p className="app-section-meta">
                {group.accounts.length}{" "}
                {group.accounts.length === 1 ? "account" : "accounts"}
              </p>
            </div>
            <p className="shrink-0 text-xl font-bold tabular-nums text-foreground">
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
