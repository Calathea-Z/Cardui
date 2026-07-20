import type { AccountDto, PlaidItemDto } from "@/lib/api";
import {
  Card,
  CardAction,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { AccountListRow } from "@/features/accounts/AccountListRow";
import { formatSyncedAt } from "./formatSyncedAt";

type InstitutionCardProps = {
  item: PlaidItemDto;
  accounts: AccountDto[];
  isSyncing: boolean;
  onSync: () => void;
};

export function InstitutionCard({
  item,
  accounts,
  isSyncing,
  onSync,
}: InstitutionCardProps) {
  const institutionName = item.institutionName ?? "Connected institution";

  return (
    <Card className="bg-card/80">
      <CardHeader>
        <CardTitle>{institutionName}</CardTitle>
        <CardDescription>
          Last synced {formatSyncedAt(item.lastTransactionsSyncedAt)}
        </CardDescription>
        <CardAction>
          <button
            type="button"
            disabled={isSyncing}
            onClick={onSync}
            className="app-cta-button"
          >
            {isSyncing ? "Syncing" : "Sync now"}
          </button>
        </CardAction>
      </CardHeader>

      <CardContent className="space-y-4">
        {item.lastSyncFailedAt ? (
          <p className="text-sm text-destructive">
            Sync failed: {item.lastSyncError ?? "Unknown error"}
          </p>
        ) : item.lastSyncCompletedAt ? (
          <p className="text-sm text-success">
            Last daily sync succeeded {formatSyncedAt(item.lastSyncCompletedAt)}
          </p>
        ) : null}

        {accounts.length > 0 ? (
          <div className="overflow-hidden rounded-lg border border-border/70">
            {accounts.map((account) => (
              <AccountListRow
                key={account.id}
                account={account}
                compact
                showBalance={false}
              />
            ))}
          </div>
        ) : (
          <p className="text-sm text-muted-foreground">
            No accounts synced for this institution yet.
          </p>
        )}
      </CardContent>
    </Card>
  );
}
