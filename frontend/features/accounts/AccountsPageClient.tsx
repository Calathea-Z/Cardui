"use client";

import { useRouter } from "next/navigation";
import { useCallback, useMemo, useState } from "react";
import { useSetMobileHeaderActions } from "@/components/navigation/mobile-header-actions";
import { syncPlaidItem } from "@/lib/api/browser";
import type {
  AccountDto,
  AccountSummaryDto,
  PlaidItemDto,
} from "@/lib/api/types";
import { AccountDetailSheet } from "./AccountDetailSheet";
import { AccountsActionButtons } from "./AccountsActionButtons";
import { AccountsView } from "./AccountsView";
import { AddAccountSheet } from "./AddAccountSheet";

type AccountsPageClientProps = {
  summary: AccountSummaryDto;
  plaidItems: PlaidItemDto[];
};

/**
 * Accounts page with add, refresh, institution management, and account detail.
 * Refresh syncs each linked institution and then reloads the page.
 */
export function AccountsPageClient({
  summary,
  plaidItems,
}: AccountsPageClientProps) {
  const router = useRouter();
  const [isAddSheetOpen, setIsAddSheetOpen] = useState(false);
  const [selectedAccount, setSelectedAccount] = useState<AccountDto | null>(
    null,
  );
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isRefreshing, setIsRefreshing] = useState(false);

  /**
   * Syncs every linked institution, then reloads the accounts page.
   * An empty institution list leaves the page as it is.
   */
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
    router.push("/institutions");
  }, [router]);

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
      <AccountsView
        summary={summary}
        actions={desktopHeaderActions}
        onSelectAccount={setSelectedAccount}
      />

      <AddAccountSheet
        open={isAddSheetOpen}
        onClose={() => setIsAddSheetOpen(false)}
      />

      <AccountDetailSheet
        account={selectedAccount}
        planningCurrency={summary.planningCurrency}
        onClose={() => setSelectedAccount(null)}
        onChanged={() => router.refresh()}
      />
    </>
  );
}
