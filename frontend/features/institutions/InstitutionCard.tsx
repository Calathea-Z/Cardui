import { useState } from "react";
import type { AccountDto, PlaidItemDto } from "@/lib/api/types";
import { Button } from "@/components/ui/button";
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
  isDisconnecting: boolean;
  onSync: () => void;
  onDisconnect: () => void;
};

/**
 * One linked bank, its synced accounts, and sync or disconnect actions.
 * Disconnect asks for confirmation and says accounts and transactions stay in Cardui.
 */
export function InstitutionCard({
  item,
  accounts,
  isSyncing,
  isDisconnecting,
  onSync,
  onDisconnect,
}: InstitutionCardProps) {
  const [confirmingDisconnect, setConfirmingDisconnect] = useState(false);
  const institutionName = item.institutionName ?? "Connected institution";
  const busy = isSyncing || isDisconnecting;

  return (
    <Card className="bg-card/80">
      <CardHeader>
        <CardTitle>{institutionName}</CardTitle>
        <CardDescription>
          Last synced {formatSyncedAt(item.lastTransactionsSyncedAt)}
        </CardDescription>
        <CardAction>
          <div className="flex flex-wrap justify-end gap-2">
            <Button
              type="button"
              disabled={busy}
              size="lg"
              variant="outline"
              onClick={onSync}
            >
              {isSyncing ? "Syncing" : "Sync now"}
            </Button>
            {confirmingDisconnect ? (
              <Button
                type="button"
                disabled={busy}
                size="lg"
                variant="destructive"
                onClick={onDisconnect}
              >
                {isDisconnecting ? "Removing" : "Remove bank link"}
              </Button>
            ) : (
              <Button
                type="button"
                disabled={busy}
                size="lg"
                variant="ghost"
                onClick={() => setConfirmingDisconnect(true)}
              >
                Disconnect
              </Button>
            )}
          </div>
        </CardAction>
      </CardHeader>

      <CardContent className="space-y-4">
        {confirmingDisconnect ? (
          <div className="flex flex-col gap-3 rounded-lg border border-border/70 px-3 py-3 sm:flex-row sm:items-center sm:justify-between">
            <p className="text-sm text-muted-foreground">
              This removes the login at the bank. Accounts and transactions stay
              in Cardui.
            </p>
            <Button
              type="button"
              disabled={busy}
              variant="ghost"
              onClick={() => setConfirmingDisconnect(false)}
            >
              Cancel
            </Button>
          </div>
        ) : null}
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
