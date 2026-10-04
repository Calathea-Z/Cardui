"use client";

import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import { BackButton } from "@/components/navigation/back-button";
import { PageHeader } from "@/components/navigation/page-header";
import { useSetMobileHeaderLeading } from "@/components/navigation/mobile-header-actions";
import { Alert } from "@/components/ui/alert";
import { EmptyState } from "@/components/ui/empty-state";
import { disconnectPlaidItem, syncPlaidItem } from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";
import type {
  AccountDto,
  PlaidItemDto,
  SyncPlaidItemResponseDto,
} from "@/lib/api/types";
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
  const [disconnectingItemId, setDisconnectingItemId] = useState<string | null>(
    null,
  );
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

  async function handleDisconnect(plaidItemId: string) {
    setDisconnectingItemId(plaidItemId);
    setLastResult(null);
    setSyncError(null);

    try {
      await disconnectPlaidItem(plaidItemId);
      setItems((currentItems) =>
        currentItems.filter((item) => item.id !== plaidItemId),
      );
      router.refresh();
    } catch (error) {
      setSyncError(
        getApiErrorMessage(error, "Could not disconnect this institution."),
      );
    } finally {
      setDisconnectingItemId(null);
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

      {syncError ? <Alert variant="panel">{syncError}</Alert> : null}

      {lastResult ? (
        <Alert>
          Last sync added {lastResult.transactions.added}, modified{" "}
          {lastResult.transactions.modified}, removed{" "}
          {lastResult.transactions.removed}.
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
              onSync={() => handleSync(item.id)}
              onDisconnect={() => handleDisconnect(item.id)}
            />
          ))}
        </div>
      ) : (
        <EmptyState
          title="No institutions connected yet"
          description="Link a bank to start syncing accounts and transactions."
          action={<PlaidLinkButton onSuccess={() => router.refresh()} />}
          className="app-panel bg-card/50 gap-2 px-6 py-12 [&_p:first-of-type]:text-lg [&_p:first-of-type]:font-medium"
        />
      )}
    </section>
  );
}
