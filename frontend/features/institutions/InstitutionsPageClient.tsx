"use client";

import { LoaderCircle } from "lucide-react";
import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import { toast } from "sonner";
import { BackButton } from "@/components/navigation/back-button";
import { PageHeader } from "@/components/navigation/page-header";
import { useSetMobileHeaderLeading } from "@/components/navigation/mobile-header-actions";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { useConfirm } from "@/components/ui/confirm-dialog";
import { EmptyState } from "@/components/ui/empty-state";
import { disconnectPlaidItem, syncPlaidItem } from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";
import type {
  AccountDto,
  PlaidItemDto,
  SyncTransactionsResponseDto,
} from "@/lib/api/types";
import {
  plaidConnectLabel,
  usePlaidLinkFlow,
} from "@/features/plaid/usePlaidLinkFlow";
import { groupAccountsByInstitution } from "./groupAccountsByInstitution";
import { InstitutionCard } from "./InstitutionCard";

const connectionToastId = "connection-action";

type InstitutionsPageClientProps = {
  initialItems: PlaidItemDto[];
  accounts: AccountDto[];
};

/**
 * Lists linked banks and lets the user connect, sync, or disconnect them.
 * Sync progress stays on the bank's button. Sync all walks the banks one at a time. The result is a toast.
 */
export function InstitutionsPageClient({
  initialItems,
  accounts,
}: InstitutionsPageClientProps) {
  const router = useRouter();
  const confirm = useConfirm();
  const [items, setItems] = useState(initialItems);
  const [syncingItemId, setSyncingItemId] = useState<string | null>(null);
  const [syncingAll, setSyncingAll] = useState(false);
  const [disconnectingItemId, setDisconnectingItemId] = useState<string | null>(
    null,
  );
  const link = usePlaidLinkFlow({
    onSuccess: () => router.refresh(),
    onUpdated: async (plaidItemId) => {
      await finishReconnect(plaidItemId);
    },
  });

  const accountsByInstitution = useMemo(
    () => groupAccountsByInstitution(accounts),
    [accounts],
  );

  const mobileHeaderLeading = useMemo(
    () => <BackButton fallbackHref="/accounts" />,
    [],
  );

  useSetMobileHeaderLeading(mobileHeaderLeading);

  /**
   * Names the bank for a toast.
   * A missing name uses a generic label.
   */
  function institutionName(plaidItemId: string) {
    return (
      items.find((item) => item.id === plaidItemId)?.institutionName ??
      "This bank"
    );
  }

  /**
   * Marks one bank as synced just now.
   * The last failure is cleared. A later refresh replaces these times with the server's.
   */
  function markSynced(plaidItemId: string) {
    const syncedAt = new Date().toISOString();
    setItems((currentItems) =>
      currentItems.map((item) =>
        item.id === plaidItemId
          ? {
              ...item,
              updatedAt: syncedAt,
              lastTransactionsSyncedAt: syncedAt,
              lastSyncFailedAt: null,
              lastSyncError: null,
              lastSyncCompletedAt: syncedAt,
              needsRepair: false,
            }
          : item,
      ),
    );
  }

  /**
   * Syncs one linked bank and refreshes the page.
   * The button shows a spinner while the request runs.
   * When a sync already holds the bank, the times stay as they are.
   */
  async function handleSync(plaidItemId: string) {
    const name = institutionName(plaidItemId);
    setSyncingItemId(plaidItemId);

    try {
      const result = await syncPlaidItem(plaidItemId);
      if (result.alreadyRunning) {
        toast.warning("Already syncing", {
          id: connectionToastId,
          description: `${name} is syncing, so this click did nothing.`,
        });
        router.refresh();
        return;
      }

      toast.success("Sync finished", {
        id: connectionToastId,
        description: syncCounts(result.transactions),
      });
      markSynced(plaidItemId);
      router.refresh();
    } catch (error) {
      toast.error(
        getApiErrorMessage(error, "Could not sync this institution."),
        { id: connectionToastId },
      );
    } finally {
      setSyncingItemId(null);
    }
  }

  /**
   * Syncs every linked bank, one after another, and refreshes the page.
   * Each bank's Sync now button spins while that bank runs. One toast sums the result.
   */
  async function handleSyncAll() {
    const banks = items;
    setSyncingAll(true);
    const totals = { added: 0, modified: 0, removed: 0 };
    let synced = 0;
    let alreadyRunning = 0;
    const failedNames: string[] = [];
    let failureMessage = "Could not sync this institution.";

    try {
      for (const bank of banks) {
        setSyncingItemId(bank.id);
        try {
          const result = await syncPlaidItem(bank.id);
          if (result.alreadyRunning) {
            alreadyRunning += 1;
            continue;
          }

          totals.added += result.transactions.added;
          totals.modified += result.transactions.modified;
          totals.removed += result.transactions.removed;
          synced += 1;
          markSynced(bank.id);
        } catch (error) {
          failedNames.push(bank.institutionName ?? "This bank");
          failureMessage = getApiErrorMessage(
            error,
            "Could not sync this institution.",
          );
        }
      }
    } finally {
      setSyncingItemId(null);
      setSyncingAll(false);
    }

    reportSyncAll(synced, alreadyRunning, failedNames, failureMessage, totals);
    router.refresh();
  }

  /**
   * Syncs one bank after its login is repaired.
   * The existing connection is kept. A sync that is already running does not change it.
   */
  async function finishReconnect(plaidItemId: string) {
    const name = institutionName(plaidItemId);
    try {
      const result = await syncPlaidItem(plaidItemId);
      if (result.alreadyRunning) {
        toast.warning("Already syncing", {
          id: connectionToastId,
          description: `${name} is syncing, so this reconnect did nothing.`,
        });
        router.refresh();
        return;
      }

      toast.success("Reconnect finished", {
        id: connectionToastId,
        description: syncCounts(result.transactions),
      });
      markSynced(plaidItemId);
      router.refresh();
    } catch (error) {
      toast.error(
        getApiErrorMessage(error, "Reconnect could not finish. Try again."),
        { id: connectionToastId },
      );
    }
  }

  /**
   * Removes one bank login after the user confirms.
   * Accounts and transactions stay. The bank leaves the list after the request succeeds.
   */
  async function handleDisconnect(plaidItemId: string) {
    const name = institutionName(plaidItemId);
    const confirmed = await confirm({
      title: `Remove ${name}?`,
      description:
        "This removes the login at the bank. Accounts and transactions stay in Cardui.",
      confirmLabel: "Remove bank link",
    });
    if (!confirmed) {
      return;
    }

    setDisconnectingItemId(plaidItemId);

    try {
      await disconnectPlaidItem(plaidItemId);
      toast.success(`Removed ${name}`, {
        id: connectionToastId,
        description: "Accounts and transactions stay in Cardui.",
      });
      setItems((currentItems) =>
        currentItems.filter((item) => item.id !== plaidItemId),
      );
      router.refresh();
    } catch (error) {
      toast.error(
        getApiErrorMessage(error, "Could not disconnect this institution."),
        { id: connectionToastId },
      );
    } finally {
      setDisconnectingItemId(null);
    }
  }

  return (
    <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
      <PageHeader
        showBack
        backFallbackHref="/accounts"
        title="Connections"
        description="Manage linked banks and the accounts synced from each institution."
        actions={
          items.length > 0 ? (
            <div className="flex flex-wrap items-center gap-2">
              <Button
                type="button"
                size="lg"
                variant="outline"
                disabled={
                  syncingAll ||
                  syncingItemId !== null ||
                  disconnectingItemId !== null ||
                  link.pendingUpdateId !== null
                }
                className={
                  syncingAll ? "min-w-24 disabled:opacity-100" : "min-w-24"
                }
                aria-busy={syncingAll}
                aria-label={syncingAll ? "Syncing all banks" : undefined}
                onClick={() => void handleSyncAll()}
              >
                {syncingAll ? (
                  <LoaderCircle
                    className="size-4 animate-spin motion-reduce:animate-none"
                    aria-hidden="true"
                  />
                ) : (
                  "Sync all"
                )}
              </Button>
              <ConnectBankButton link={link} />
            </div>
          ) : undefined
        }
      />

      {link.errorMessage ? (
        <Alert variant="destructive">
          {link.errorMessage}{" "}
          <button
            type="button"
            onClick={link.clearError}
            className="cursor-pointer underline underline-offset-2"
          >
            Dismiss
          </button>
        </Alert>
      ) : null}

      {items.length > 0 ? (
        <div className="grid gap-4 lg:grid-cols-2">
          {items.map((item) => (
            <InstitutionCard
              key={item.id}
              item={item}
              accounts={accountsByInstitution.get(item.id) ?? []}
              isSyncing={syncingItemId === item.id}
              isDisconnecting={disconnectingItemId === item.id}
              isReconnecting={link.pendingUpdateId === item.id}
              actionsDisabled={
                (syncingAll && syncingItemId !== item.id) ||
                (link.pendingUpdateId !== null &&
                  link.pendingUpdateId !== item.id)
              }
              onSync={() => handleSync(item.id)}
              onReconnect={() => void link.openUpdate(item.id)}
              onDisconnect={() => handleDisconnect(item.id)}
            />
          ))}
        </div>
      ) : (
        <EmptyState
          title="No institutions connected yet"
          description="Link a bank to start syncing accounts and transactions."
          action={<ConnectBankButton link={link} />}
          className="app-panel bg-card/50 gap-2 px-6 py-12 [&_p:first-of-type]:text-lg [&_p:first-of-type]:font-medium"
        />
      )}
    </section>
  );
}

type ConnectBankButtonProps = {
  link: {
    open: () => Promise<void>;
    canAttemptConnect: boolean;
    isCreatingToken: boolean;
    isExchangingToken: boolean;
    pendingUpdateId: string | null;
    connectErrorMessage: string | null;
  };
};

/**
 * Starts a new bank connection from the page's Plaid Link flow.
 * Preparing names a new connection. A repair uses Reconnect on that bank.
 */
function ConnectBankButton({ link }: ConnectBankButtonProps) {
  return (
    <Button
      type="button"
      disabled={!link.canAttemptConnect}
      onClick={() => void link.open()}
      size="lg"
      className="w-fit"
    >
      {plaidConnectLabel({
        isCreatingToken: link.isCreatingToken && link.pendingUpdateId === null,
        isExchangingToken: link.isExchangingToken,
        errorMessage: link.connectErrorMessage,
      })}
    </Button>
  );
}

/**
 * Formats added, modified, and removed counts for a sync toast.
 */
function syncCounts(counts: SyncTransactionsResponseDto) {
  return `Added ${counts.added}, modified ${counts.modified}, removed ${counts.removed}.`;
}

/**
 * Reports one Sync all result.
 * One failure uses that bank's error. Several failures name the banks. A click where every bank was already syncing is a warning.
 */
function reportSyncAll(
  synced: number,
  alreadyRunning: number,
  failedNames: string[],
  failureMessage: string,
  totals: SyncTransactionsResponseDto,
) {
  if (failedNames.length > 0) {
    toast.error(
      failedNames.length === 1 && synced === 0
        ? failureMessage
        : "Could not sync every bank",
      {
        id: connectionToastId,
        description:
          failedNames.length === 1 && synced === 0
            ? undefined
            : synced > 0
              ? `Could not sync ${failedNames.join(", ")}. ${syncCounts(totals)}`
              : `Could not sync ${failedNames.join(", ")}.`,
      },
    );
    return;
  }

  if (synced === 0 && alreadyRunning > 0) {
    toast.warning("Already syncing", {
      id: connectionToastId,
      description:
        alreadyRunning === 1
          ? "That bank is syncing, so this click did nothing."
          : "Those banks are syncing, so this click did nothing.",
    });
    return;
  }

  const already =
    alreadyRunning === 0
      ? ""
      : alreadyRunning === 1
        ? " 1 bank was already syncing."
        : ` ${alreadyRunning} banks were already syncing.`;

  toast.success("Sync finished", {
    id: connectionToastId,
    description: `${syncCounts(totals)}${already}`,
  });
}
