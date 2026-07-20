"use client";

import { useState } from "react";
import type { AccountSummaryDto } from "@/lib/api";
import {
  DEFAULT_ACCOUNT_CHART_METRIC,
  type AccountChartMetric,
} from "./accountChartMetric";
import { AccountChartMetricSelector } from "./AccountChartMetricSelector";
import { AccountsBalanceChartSection } from "./AccountsBalanceChartSection";
import { AccountsSectionList } from "./AccountsSectionList";

type AccountsViewProps = {
  summary: AccountSummaryDto;
  actions?: React.ReactNode;
};

export function AccountsView({ summary, actions }: AccountsViewProps) {
  const [metric, setMetric] = useState<AccountChartMetric>(
    DEFAULT_ACCOUNT_CHART_METRIC,
  );

  return (
    <section className="flex flex-col gap-6">
      {actions ? (
        <div className="hidden justify-end md:flex">{actions}</div>
      ) : null}

      <AccountChartMetricSelector value={metric} onChange={setMetric} />

      <AccountsBalanceChartSection
        history={summary.history}
        groups={summary.groups}
        metric={metric}
        netWorth={summary.netWorth}
        showPeriodDelta
        embedded
      />

      <AccountsSectionList groups={summary.groups} />
    </section>
  );
}
