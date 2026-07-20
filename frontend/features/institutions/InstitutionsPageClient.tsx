"use client";

import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import { BackButton } from "@/components/navigation/back-button";
import { PageHeader } from "@/components/navigation/page-header";
import { useSetMobileHeaderLeading } from "@/components/navigation/mobile-header-actions";
import {
  getApiErrorMessage,
  syncPlaidItem,
  type AccountDto,
  type PlaidItemDto,
  type SyncPlaidItemResponseDto,
} from "@/lib/api";
import { PlaidLinkButton } from "@/features/plaid/PlaidLinkButton";
import { InstitutionCard } from "./InstitutionCard";

type InstitutionsPageClientProps = {
  initialItems: PlaidItemDto[];
  accounts: AccountDto[];
};

function groupAccountsByInstitution(accounts: AccountDto[]) {
  return accounts.reduce<Map<string, AccountDto[]>>((groups, account) => {
    if (!account.plaidItemId || !account.isActive) {
      return groups;
    }

    const existing = groups.get(account.plaidItemId) ?? [];
    existing.push(account);
    groups.set(account.plaidItemId, existing);
    return groups;
  }, new Map());
}

export function InstitutionsPageClient({
  initialItems,
  accounts,
}: InstitutionsPageClientProps) {
  const router = useRouter();
  const [items, setItems] = useState(initialItems);
  const [syncingItemId, setSyncingItemId] = useState<string | null>(null);
  const [lastResult, setLastResult] = useState<SyncPlaidItemResponseDto | null>(
    null,
  );
  const [syncError, setSyncError] = useState<string | null>(null);

  const accountsByInstitution = useMemo(
    () => groupAccountsByInstitution(accounts),
    [accounts],
  );

  const mobileHeaderLeading = useMemo(
    () => <BackButton fallbackHref="/accounts" />,
    [],
  );

  useSetMobileHeaderLeading(mobileHeaderLeading);

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
                lastSyncFailedAt: null,
                lastSyncError: null,
                lastSyncCompletedAt: new Date().toISOString(),
              }
            : item,
        ),
      );

      router.refresh();
    } catch (error) {
      setSyncError(
        getApiErrorMessage(error, "Could not sync this institution."),
      );
    } finally {
      setSyncingItemId(null);
    }
  }

  return (
    <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
      <PageHeader
        backFallbackHref="/accounts"
        eyebrow="Bank connections"
        title="Institutions"
        description="Manage linked banks and the accounts synced from each institution."
        actions={
          items.length > 0 ? (
            <PlaidLinkButton
              onSuccess={() => router.refresh()}
              className="w-full sm:w-auto"
            />
          ) : undefined
        }
      />

      {syncError ? (
        <div className="app-panel border-destructive/40 p-4 text-sm text-destructive">
          {syncError}
        </div>
      ) : null}

      {lastResult ? (
        <div className="app-panel p-4 text-sm text-muted-foreground">
          Last sync added {lastResult.transactions.added}, modified{" "}
          {lastResult.transactions.modified}, removed{" "}
          {lastResult.transactions.removed}.
        </div>
      ) : null}

      {items.length > 0 ? (
        <div className="grid gap-4 lg:grid-cols-2">
          {items.map((item) => (
            <InstitutionCard
              key={item.id}
              item={item}
              accounts={accountsByInstitution.get(item.id) ?? []}
              isSyncing={syncingItemId === item.id}
              onSync={() => handleSync(item.id)}
            />
          ))}
        </div>
      ) : (
        <div className="app-panel bg-card/50 px-6 py-12 text-center">
          <p className="text-lg font-medium text-foreground">
            No institutions connected yet
          </p>
          <p className="mt-2 text-sm text-muted-foreground">
            Link a bank to start syncing accounts and transactions.
          </p>
          <div className="mt-6 flex justify-center">
            <PlaidLinkButton onSuccess={() => router.refresh()} />
          </div>
        </div>
      )}
    </section>
  );
}
