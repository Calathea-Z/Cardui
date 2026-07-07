"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { getApiErrorMessage } from "@/lib/api";
import {
  syncPlaidItem,
  type PlaidItemDto,
  type SyncPlaidItemResponseDto,
} from "@/lib/api/plaid";
import { cn } from "@/lib/utils";

type ConnectedInstitutionsPanelProps = {
  initialItems: PlaidItemDto[];
  onSyncComplete?: () => void;
  embedded?: boolean;
};

function formatSyncedAt(value: string | null) {
  if (!value) {
    return "Never synced";
  }

  return new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  }).format(new Date(value));
}

export function ConnectedInstitutionsPanel({
  initialItems,
  onSyncComplete,
  embedded = false,
}: ConnectedInstitutionsPanelProps) {
  const router = useRouter();
  const [items, setItems] = useState(initialItems);
  const [syncingItemId, setSyncingItemId] = useState<string | null>(null);
  const [lastResult, setLastResult] =
    useState<SyncPlaidItemResponseDto | null>(null);
  const [syncError, setSyncError] = useState<string | null>(null);

  async function handleSync(plaidItemId: string) {
    setSyncingItemId(plaidItemId);
    setLastResult(null);
    setSyncError(null);

    try {
      const result = await syncPlaidItem(plaidItemId);
      setLastResult(result);

      setItems((currentItems) =>
        currentItems.map((item) =>
          item.id === plaidItemId
            ? {
                ...item,
                updatedAt: new Date().toISOString(),
                lastTransactionsSyncedAt: new Date().toISOString(),
              }
            : item,
        ),
      );

      if (onSyncComplete) {
        onSyncComplete();
      } else {
        router.refresh();
      }
    } catch (error) {
      setSyncError(
        getApiErrorMessage(error, "Could not sync this institution."),
      );
    } finally {
      setSyncingItemId(null);
    }
  }

  return (
    <section className={cn(embedded ? "flex flex-col" : "app-panel")}>
      {!embedded ? (
        <div className="app-panel-header p-4">
          <h2 className="app-section-title">Connected institutions</h2>
        </div>
      ) : null}

      <div
        className={cn(
          "divide-y divide-border/70",
          embedded && "app-panel",
        )}
      >
        {items.map((item) => (
          <div
            key={item.id}
            className="flex flex-col gap-3 p-4 md:flex-row md:items-center md:justify-between"
          >
            <div>
              <p className="font-medium">{item.institutionName ?? "Connected institution"}</p>
              <p className="mt-1 text-sm text-muted-foreground">
                Last synced {formatSyncedAt(item.lastTransactionsSyncedAt)}
              </p>
            </div>

            <button
              type="button"
              disabled={syncingItemId === item.id}
              onClick={() => handleSync(item.id)}
              className="app-cta-button"
            >
              {syncingItemId === item.id ? "Syncing" : "Sync now"}
            </button>
          </div>
        ))}

        {items.length === 0 ? (
          <div className="p-4 text-sm text-muted-foreground">
            No institutions connected yet.
          </div>
        ) : null}
      </div>

      {syncError ? (
        <div className="border-t border-border p-4 text-sm text-destructive">
          {syncError}
        </div>
      ) : null}

      {lastResult ? (
        <div className="border-t border-border p-4 text-sm text-muted-foreground">
          Added {lastResult.transactions.added}, modified{" "}
          {lastResult.transactions.modified}, removed{" "}
          {lastResult.transactions.removed}.
        </div>
      ) : null}
    </section>
  );
}
