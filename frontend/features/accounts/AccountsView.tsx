"use client";

import { useState } from "react";
import type { AccountDto, AccountSummaryDto } from "@/lib/api/types";
import {
  DEFAULT_ACCOUNT_CHART_METRIC,
  type AccountChartMetric,
} from "./accountChartMetric";
import { AccountChartMetricSelector } from "./AccountChartMetricSelector";
import { AccountsBalanceChartSection } from "./AccountsBalanceChartSection";
import { AccountsSectionList } from "./AccountsSectionList";
import { CurrencyExclusionNotice } from "@/features/household/CurrencyExclusionNotice";

type AccountsViewProps = {
  summary: AccountSummaryDto;
  actions?: React.ReactNode;
  onSelectAccount?: (account: AccountDto) => void;
};

export function AccountsView({
  summary,
  actions,
  onSelectAccount,
}: AccountsViewProps) {
  const [metric, setMetric] = useState<AccountChartMetric>(
    DEFAULT_ACCOUNT_CHART_METRIC,
  );

  return (
    <section className="flex flex-col gap-6">
      {actions ? (
        <div className="hidden justify-end md:flex">{actions}</div>
      ) : null}

      <AccountChartMetricSelector value={metric} onChange={setMetric} />

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
        metric={metric}
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
