import Link from "next/link";
import type { AccountGroupDto } from "@/lib/api/types";
import { EmptyState } from "@/components/ui/empty-state";
import { formatCurrency } from "@/features/accounts/formatCurrency";

type DashboardAccountGroupsPanelProps = {
  groups: AccountGroupDto[];
  emptyMessage: string;
  currency?: string;
  liability?: boolean;
};

/**
 * Lists the account groups on an assets or liabilities slide.
 * Groups with no accounts are omitted, and a liability total uses formatCurrency with a leading minus.
 */
export function DashboardAccountGroupsPanel({
  groups,
  emptyMessage,
  currency = "USD",
  liability = false,
}: DashboardAccountGroupsPanelProps) {
  const visibleGroups = groups.filter((group) => group.accounts.length > 0);

  if (visibleGroups.length === 0) {
    return (
      <EmptyState
        title={emptyMessage}
        action={
          <Link
            href="/accounts"
            className="text-sm text-primary underline underline-offset-2"
          >
            Go to Accounts
          </Link>
        }
        className="py-8 [&_p]:text-sm [&_p]:font-normal [&_p]:text-muted-foreground"
      />
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
                ? "ledger-amount text-destructive"
                : "ledger-amount text-foreground"
            }
          >
            {liability ? "-" : ""}
            {formatCurrency(group.total, currency)}
          </p>
        </div>
      ))}
    </div>
  );
}
