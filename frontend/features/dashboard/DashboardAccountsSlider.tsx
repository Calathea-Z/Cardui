"use client";

import { useCallback, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import type { AccountSummaryDto } from "@/lib/api/types";
import { AccountsBalanceChart } from "@/features/accounts/AccountsBalanceChart";
import { ChartTimeRangeSelector } from "@/features/accounts/ChartTimeRangeSelector";
import { PeriodDeltaLabel } from "@/features/accounts/AccountsBalanceChartSection";
import {
  DEFAULT_CHART_TIME_RANGE,
  type ChartTimeRange,
} from "@/features/accounts/chartTimeRange";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import { PlaidLinkButton } from "@/features/plaid/PlaidLinkButton";
import { EmptyState } from "@/components/ui/empty-state";
import { cn } from "@/lib/utils";
import { DashboardAccountGroupsPanel } from "./DashboardAccountGroupsPanel";
import {
  ASSET_GROUP_KEYS,
  getAccountGroups,
  LIABILITY_GROUP_KEYS,
  sumGroupTotals,
} from "./dashboardAccountGroups";

type DashboardAccountsSliderProps = {
  summary: AccountSummaryDto;
};

/**
 * Slides in the dashboard accounts card, in scroll order.
 * Net worth is first, then assets, then liabilities.
 */
const PANELS = [
  { id: "net-worth", label: "Net Worth" },
  { id: "assets", label: "Assets" },
  { id: "liabilities", label: "Liabilities" },
] as const;

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
 * Swipes between net worth, assets, and liabilities.
 * The heading follows the visible slide, liability amounts use formatCurrency with a leading minus, and the period change appears on the net-worth slide when accounts exist.
 */
export function DashboardAccountsSlider({
  summary,
}: DashboardAccountsSliderProps) {
  const router = useRouter();
  const scrollRef = useRef<HTMLDivElement>(null);
  const [activeIndex, setActiveIndex] = useState(0);
  const [chartRange, setChartRange] = useState<ChartTimeRange>(
    DEFAULT_CHART_TIME_RANGE,
  );
  const showNetWorthEmptyState = !hasConnectedAccounts(summary.groups);

  const assetGroups = getAccountGroups(summary.groups, ASSET_GROUP_KEYS);
  const liabilityGroups = getAccountGroups(
    summary.groups,
    LIABILITY_GROUP_KEYS,
  );
  const totalAssets = sumGroupTotals(assetGroups);
  const totalLiabilities = sumGroupTotals(liabilityGroups);

  const headerTotal = useMemo(() => {
    switch (activeIndex) {
      case 0:
        return { value: summary.netWorth, liability: false };
      case 1:
        return { value: totalAssets, liability: false };
      case 2:
        return { value: totalLiabilities, liability: true };
      default:
        return { value: summary.netWorth, liability: false };
    }
  }, [activeIndex, summary.netWorth, totalAssets, totalLiabilities]);

  /**
   * Updates the active slide from how far the card has scrolled.
   * The index is the nearest panel and stays within the three slides.
   */
  const handleScroll = useCallback(() => {
    const container = scrollRef.current;
    if (!container) {
      return;
    }

    const panelWidth = container.clientWidth;
    if (panelWidth === 0) {
      return;
    }

    const index = Math.round(container.scrollLeft / panelWidth);
    setActiveIndex(Math.min(index, PANELS.length - 1));
  }, []);

  /**
   * Scrolls the accounts card to a chosen slide.
   * The heading updates to that slide as the scroll starts.
   */
  const scrollToPanel = useCallback((index: number) => {
    const container = scrollRef.current;
    if (!container) {
      return;
    }

    container.scrollTo({
      left: container.clientWidth * index,
      behavior: "smooth",
    });
    setActiveIndex(index);
  }, []);

  return (
    <section className="app-panel overflow-hidden">
      <div className="app-panel-header px-4 py-3.5">
        <p className="text-[11px] font-semibold tracking-[0.14em] text-muted-foreground uppercase">
          {PANELS[activeIndex].label}
        </p>
        <p
          className={cn(
            "ledger-amount mt-1 text-3xl",
            headerTotal.liability ? "text-destructive" : "text-foreground",
          )}
        >
          {headerTotal.liability ? "-" : ""}
          {formatCurrency(headerTotal.value, summary.planningCurrency)}
        </p>
        {activeIndex === 0 && !showNetWorthEmptyState ? (
          <PeriodDeltaLabel
            history={summary.history}
            range={chartRange}
            currency={summary.planningCurrency}
            compact
            className="mt-1"
          />
        ) : null}
      </div>

      <div
        ref={scrollRef}
        onScroll={handleScroll}
        className="scrollbar-none flex touch-pan-x snap-x snap-mandatory overflow-x-auto"
      >
        <section className="w-full shrink-0 snap-center">
          <div className="px-4 py-4">
            {showNetWorthEmptyState ? (
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
                  compact
                />
              </div>
            )}
          </div>
        </section>

        <section className="w-full shrink-0 snap-center py-4">
          <DashboardAccountGroupsPanel
            groups={assetGroups}
            currency={summary.planningCurrency}
            emptyMessage="No asset accounts connected yet."
          />
        </section>

        <section className="w-full shrink-0 snap-center py-4">
          <DashboardAccountGroupsPanel
            groups={liabilityGroups}
            currency={summary.planningCurrency}
            emptyMessage="No liability accounts connected yet."
            liability
          />
        </section>
      </div>

      <div className="flex items-center justify-between gap-2 border-t border-border/70 px-4 py-3">
        <div className="flex items-center gap-1">
          {PANELS.map((panel, index) => (
            <button
              key={panel.id}
              type="button"
              aria-label={`Show ${panel.label}`}
              aria-current={activeIndex === index ? "true" : undefined}
              onClick={() => scrollToPanel(index)}
              className={cn(
                "min-h-10 border-b-2 px-3 text-xs font-medium tracking-wide transition",
                activeIndex === index
                  ? "border-primary text-primary"
                  : "border-transparent text-muted-foreground hover:text-foreground",
              )}
            >
              {panel.label}
            </button>
          ))}
        </div>

        <Link
          href="/accounts"
          className="text-xs text-muted-foreground underline underline-offset-2 hover:text-foreground"
        >
          View all
        </Link>
      </div>
    </section>
  );
}
