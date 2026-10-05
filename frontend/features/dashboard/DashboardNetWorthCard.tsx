"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import type { AccountSummaryDto } from "@/lib/api/types";
import { AccountsBalanceChart } from "@/features/accounts/AccountsBalanceChart";
import { PeriodDeltaLabel } from "@/features/accounts/AccountsBalanceChartSection";
import { ChartTimeRangeSelector } from "@/features/accounts/ChartTimeRangeSelector";
import {
  DEFAULT_CHART_TIME_RANGE,
  type ChartTimeRange,
} from "@/features/accounts/chartTimeRange";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import { PlaidLinkButton } from "@/features/plaid/PlaidLinkButton";
import { EmptyState } from "@/components/ui/empty-state";
import {
  ASSET_GROUP_KEYS,
  getAccountGroups,
  LIABILITY_GROUP_KEYS,
  sumGroupTotals,
} from "./dashboardAccountGroups";

type DashboardNetWorthCardProps = {
  summary: AccountSummaryDto;
};

/**
 * True when a group other than net worth contains an account.
 * The net-worth group is passed over, and a group counts when its account list contains an account.
 */
function hasConnectedAccounts(groups: AccountSummaryDto["groups"]) {
  return groups
    .filter((group) => group.key !== "net-worth")
    .some((group) => group.accounts.length > 0);
}

/**
 * Home net-worth card: one figure, one chart, and asset and liability totals.
 * The range control is text. Assets and liabilities stay figures, not swipe panels.
 */
export function DashboardNetWorthCard({ summary }: DashboardNetWorthCardProps) {
  const router = useRouter();
  const [chartRange, setChartRange] = useState<ChartTimeRange>(
    DEFAULT_CHART_TIME_RANGE,
  );
  const showEmptyState = !hasConnectedAccounts(summary.groups);
  const totalAssets = sumGroupTotals(
    getAccountGroups(summary.groups, ASSET_GROUP_KEYS),
  );
  const totalLiabilities = sumGroupTotals(
    getAccountGroups(summary.groups, LIABILITY_GROUP_KEYS),
  );

  return (
    <section className="app-panel">
      <div className="app-panel-header px-4 py-4">
        <p className="text-sm font-semibold text-foreground">Net worth</p>
        <p className="ledger-amount mt-2 text-[2rem] text-foreground">
          {formatCurrency(summary.netWorth, summary.planningCurrency)}
        </p>
        {!showEmptyState ? (
          <PeriodDeltaLabel
            history={summary.history}
            range={chartRange}
            currency={summary.planningCurrency}
            compact
            className="mt-1"
          />
        ) : null}
      </div>

      <div className="flex flex-col gap-4 px-4 py-4 sm:flex-row sm:items-start">
        <div className="min-w-0 flex-1">
          {showEmptyState ? (
            <EmptyState
              title="No accounts connected yet"
              description="Link a bank to start tracking your net worth."
              action={<PlaidLinkButton onSuccess={() => router.refresh()} />}
              className="h-40 py-0"
            />
          ) : (
            <div className="flex flex-col gap-3">
              <AccountsBalanceChart
                history={summary.history}
                range={chartRange}
                currency={summary.planningCurrency}
                compact
                embedded
              />
              <ChartTimeRangeSelector
                value={chartRange}
                onChange={setChartRange}
                variant="text"
              />
            </div>
          )}
        </div>

        <dl className="grid w-full grid-cols-2 gap-3 sm:w-40 sm:grid-cols-1">
          <div className="rounded-lg bg-muted px-3 py-2">
            <dt className="text-xs font-medium text-muted-foreground">
              Assets
            </dt>
            <dd className="ledger-amount mt-1 text-sm text-foreground">
              {formatCurrency(totalAssets, summary.planningCurrency)}
            </dd>
          </div>
          <div className="rounded-lg bg-muted px-3 py-2">
            <dt className="text-xs font-medium text-muted-foreground">
              Liabilities
            </dt>
            <dd className="ledger-amount mt-1 text-sm text-destructive">
              -{formatCurrency(totalLiabilities, summary.planningCurrency)}
            </dd>
          </div>
        </dl>
      </div>

      <div className="border-t border-border px-4 py-3">
        <Link
          href="/accounts"
          className="text-xs font-medium text-primary hover:underline"
        >
          View accounts
        </Link>
      </div>
    </section>
  );
}
