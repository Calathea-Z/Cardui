"use client";

import { useRouter } from "next/navigation";
import { useCallback, useMemo, useState } from "react";
import { useSetMobileHeaderActions } from "@/components/navigation/mobile-header-actions";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import type { AccountSummaryDto } from "@/lib/api";
import { syncPlaidItem, type PlaidItemDto } from "@/lib/api/plaid";
import { ConnectedInstitutionsPanel } from "@/features/plaid/ConnectedInstitutionsPanel";
import { AccountsActionButtons } from "./AccountsActionButtons";
import { AccountsView } from "./AccountsView";
import { AddAccountSheet } from "./AddAccountSheet";

type AccountsPageClientProps = {
  summary: AccountSummaryDto;
  plaidItems: PlaidItemDto[];
};

export function AccountsPageClient({
  summary,
  plaidItems,
}: AccountsPageClientProps) {
  const router = useRouter();
  const [isAddSheetOpen, setIsAddSheetOpen] = useState(false);
  const [isInstitutionsSheetOpen, setIsInstitutionsSheetOpen] = useState(false);
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isRefreshing, setIsRefreshing] = useState(false);

  const handleRefreshAll = useCallback(async () => {
    if (plaidItems.length === 0) {
      return;
    }

    setIsRefreshing(true);

    try {
      for (const item of plaidItems) {
        await syncPlaidItem(item.id);
      }

      router.refresh();
    } finally {
      setIsRefreshing(false);
    }
  }, [plaidItems, router]);

  const handleManageInstitutions = useCallback(() => {
    setIsInstitutionsSheetOpen(true);
  }, []);

  const handleAdd = useCallback(() => {
    setIsAddSheetOpen(true);
  }, []);

  const actionButtonProps = useMemo(
    () => ({
      onAdd: handleAdd,
      onRefreshAll: handleRefreshAll,
      onManageInstitutions: handleManageInstitutions,
      isMenuOpen,
      onMenuOpenChange: setIsMenuOpen,
      isRefreshing,
    }),
    [
      handleAdd,
      handleRefreshAll,
      handleManageInstitutions,
      isMenuOpen,
      isRefreshing,
    ],
  );

  const mobileHeaderActions = useMemo(
    () => <AccountsActionButtons {...actionButtonProps} />,
    [actionButtonProps],
  );

  const desktopHeaderActions = useMemo(
    () => <AccountsActionButtons {...actionButtonProps} />,
    [actionButtonProps],
  );

  useSetMobileHeaderActions(mobileHeaderActions);

  return (
    <>
      <AccountsView summary={summary} actions={desktopHeaderActions} />

      <AddAccountSheet
        open={isAddSheetOpen}
        onClose={() => setIsAddSheetOpen(false)}
      />

      <BottomSheet
        open={isInstitutionsSheetOpen}
        onClose={() => setIsInstitutionsSheetOpen(false)}
        title="Manage institutions"
      >
        <ConnectedInstitutionsPanel
          initialItems={plaidItems}
          onSyncComplete={() => router.refresh()}
          embedded
        />
      </BottomSheet>
    </>
  );
}
