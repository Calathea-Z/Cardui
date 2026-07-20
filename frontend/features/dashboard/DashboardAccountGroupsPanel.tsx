import Link from "next/link";
import type { AccountGroupDto } from "@/lib/api/types";
import { formatCurrency } from "@/features/accounts/formatCurrency";

type DashboardAccountGroupsPanelProps = {
  groups: AccountGroupDto[];
  emptyMessage: string;
  liability?: boolean;
};

export function DashboardAccountGroupsPanel({
  groups,
  emptyMessage,
  liability = false,
}: DashboardAccountGroupsPanelProps) {
  const visibleGroups = groups.filter((group) => group.accounts.length > 0);

  if (visibleGroups.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center gap-3 px-4 py-8 text-center text-sm text-muted-foreground">
        <p>{emptyMessage}</p>
        <Link href="/accounts" className="text-primary underline underline-offset-2">
          Go to Accounts
        </Link>
      </div>
    );
  }

  return (
    <div className="divide-y divide-border/70 px-4">
      {visibleGroups.map((group) => (
        <div
          key={group.key}
          className="flex items-center justify-between gap-4 py-3.5"
        >
          <p className="font-medium text-foreground">{group.name}</p>
          <p
            className={
              liability
                ? "font-semibold tabular-nums text-destructive"
                : "font-semibold tabular-nums"
            }
          >
            {liability ? "-" : ""}
            {formatCurrency(group.total)}
          </p>
        </div>
      ))}
    </div>
  );
}
