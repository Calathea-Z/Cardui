"use client";

import type { AccountDto, AccountSummaryDto } from "@/lib/api/types";
import { AccountsBalanceChartSection } from "./AccountsBalanceChartSection";
import { AccountsSectionList } from "./AccountsSectionList";
import { PageHeader } from "@/components/navigation/page-header";
import { CurrencyExclusionNotice } from "@/features/household/CurrencyExclusionNotice";

type AccountsViewProps = {
  summary: AccountSummaryDto;
  actions?: React.ReactNode;
  onSelectAccount?: (account: AccountDto) => void;
};

/**
 * Accounts screen with the balance chart and grouped list.
 * The chart starts on net worth until another series is chosen.
 */
export function AccountsView({
  summary,
  actions,
  onSelectAccount,
}: AccountsViewProps) {
  return (
    <section className="flex flex-col gap-6">
      <PageHeader title="Accounts" actions={actions} />

      <CurrencyExclusionNotice
        exclusion={{
          planningCurrency: summary.planningCurrency,
          excludedAccountCount: summary.excludedAccountCount,
          excludedCurrencies: summary.excludedCurrencies,
        }}
      />

      <AccountsBalanceChartSection
        history={summary.history}
        groups={summary.groups}
        netWorth={summary.netWorth}
        currency={summary.planningCurrency}
        showPeriodDelta
        embedded
      />

      <AccountsSectionList
        groups={summary.groups}
        archivedAccounts={summary.archivedAccounts ?? []}
        planningCurrency={summary.planningCurrency}
        onSelectAccount={onSelectAccount}
      />
    </section>
  );
}
