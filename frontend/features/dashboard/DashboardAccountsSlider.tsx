"use client";

import { useCallback, useMemo, useRef, useState } from "react";
import Link from "next/link";
import type { AccountSummaryDto } from "@/lib/api";
import { AccountsBalanceChart } from "@/features/accounts/AccountsBalanceChart";
import { formatCurrency } from "@/features/accounts/formatCurrency";
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

const PANELS = [
  { id: "net-worth", label: "Net Worth" },
  { id: "assets", label: "Assets" },
  { id: "liabilities", label: "Liabilities" },
] as const;

export function DashboardAccountsSlider({
  summary,
}: DashboardAccountsSliderProps) {
  const scrollRef = useRef<HTMLDivElement>(null);
  const [activeIndex, setActiveIndex] = useState(0);

  const assetGroups = getAccountGroups(summary.groups, ASSET_GROUP_KEYS);
  const liabilityGroups = getAccountGroups(summary.groups, LIABILITY_GROUP_KEYS);
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
      <div className="app-panel-header flex items-center justify-between gap-4 px-4 py-3.5">
        <h2 className="app-section-title">{PANELS[activeIndex].label}</h2>
        <p
          className={cn(
            "text-xl font-bold tabular-nums",
            headerTotal.liability ? "text-destructive" : "text-violet-50",
          )}
        >
          {headerTotal.liability ? "-" : ""}
          {formatCurrency(headerTotal.value)}
        </p>
      </div>

      <div
        ref={scrollRef}
        onScroll={handleScroll}
        className="scrollbar-none flex touch-pan-x snap-x snap-mandatory overflow-x-auto"
      >
        <section className="w-full shrink-0 snap-center">
          <div className="px-4 py-4">
            <AccountsBalanceChart
              history={summary.history}
              compact
              embedded
            />
          </div>
        </section>

        <section className="w-full shrink-0 snap-center py-4">
          <DashboardAccountGroupsPanel
            groups={assetGroups}
            emptyMessage="No asset accounts connected yet."
          />
        </section>

        <section className="w-full shrink-0 snap-center py-4">
          <DashboardAccountGroupsPanel
            groups={liabilityGroups}
            emptyMessage="No liability accounts connected yet."
            liability
          />
        </section>
      </div>

      <div className="flex items-center justify-between gap-2 border-t border-border/70 px-4 py-3">
        <div className="flex items-center gap-2">
          {PANELS.map((panel, index) => (
            <button
              key={panel.id}
              type="button"
              aria-label={`Show ${panel.label}`}
              aria-current={activeIndex === index ? "true" : undefined}
              onClick={() => scrollToPanel(index)}
              className={cn(
                "min-h-10 rounded-full px-3 text-xs font-medium transition",
                activeIndex === index
                  ? "bg-primary text-primary-foreground"
                  : "bg-muted text-muted-foreground hover:text-foreground",
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
