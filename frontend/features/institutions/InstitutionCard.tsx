import { LoaderCircle } from "lucide-react";
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
  actionsDisabled: boolean;
  onSync: () => void;
  onDisconnect: () => void;
};

/**
 * One linked bank, its synced accounts, and sync or disconnect actions.
 * Sync now and Disconnect show a spinner while the request runs and keep the button in place.
 * Disconnect confirms in the app dialog before it removes the bank login.
 * actionsDisabled locks both buttons while another bank in a Sync all is running.
 */
export function InstitutionCard({
  item,
  accounts,
  isSyncing,
  isDisconnecting,
  actionsDisabled,
  onSync,
  onDisconnect,
}: InstitutionCardProps) {
  const institutionName = item.institutionName ?? "Connected institution";
  const busy = isSyncing || isDisconnecting || actionsDisabled;

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
              className={
                isSyncing ? "min-w-24 disabled:opacity-100" : "min-w-24"
              }
              aria-busy={isSyncing}
              aria-label={isSyncing ? `Syncing ${institutionName}` : undefined}
              onClick={onSync}
            >
              {isSyncing ? (
                <LoaderCircle
                  className="size-4 animate-spin motion-reduce:animate-none"
                  aria-hidden="true"
                />
              ) : (
                "Sync now"
              )}
            </Button>
            <Button
              type="button"
              disabled={busy}
              size="lg"
              variant="ghost"
              className={
                isDisconnecting ? "min-w-28 disabled:opacity-100" : "min-w-28"
              }
              aria-busy={isDisconnecting}
              aria-label={
                isDisconnecting ? `Removing ${institutionName}` : undefined
              }
              onClick={onDisconnect}
            >
              {isDisconnecting ? (
                <LoaderCircle
                  className="size-4 animate-spin motion-reduce:animate-none"
                  aria-hidden="true"
                />
              ) : (
                "Disconnect"
              )}
            </Button>
          </div>
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
