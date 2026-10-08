"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { PageHeader } from "@/components/navigation/page-header";
import { buttonVariants } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { SegmentedControl } from "@/components/ui/segmented-control";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import { formatCalendarDate } from "@/features/debts/debtDisplay";
import type { PayoffRolloverKind, PlanRecoveryDto } from "@/lib/api/types";
import { cn } from "@/lib/utils";
import { PlanBalanceChart } from "./PlanBalanceChart";
import { PlanFinishList } from "./PlanFinishList";
import { PlanObligationChart } from "./PlanObligationChart";
import { PlanOwedChart } from "./PlanOwedChart";
import { PlanPayoffOrder } from "./PlanPayoffOrder";
import { PlanSummary } from "./PlanSummary";
import {
  balanceChart,
  debtColors,
  obligationChart,
  owedShares,
  payoffOrder,
} from "./planChartSeries";
import {
  finishPlanItems,
  planAssumptions,
  planDescription,
  planPathOptions,
  planSummary,
} from "./planCopy";

type PlanPageClientProps = {
  report: PlanRecoveryDto;
  failed: boolean;
};

/**
 * Plan page.
 * What you owe today is always shown. The title-row switch picks Rollover or Keep freed payments, and the summary, Finish your plan,
 * both payoff charts, and the payoff order follow it. Until a debt can be paid off, the switch and the payoff charts are hidden,
 * because both paths would be the same. A failed load keeps the error above the page. No debts asks for one on Debts.
 */
export function PlanPageClient({ report, failed }: PlanPageClientProps) {
  const [kind, setKind] = useState<PayoffRolloverKind>("Rollover");
  const [hoveredDebtId, setHoveredDebtId] = useState<string | null>(null);
  const [pinnedDebtId, setPinnedDebtId] = useState<string | null>(null);
  const showPlan = !failed && report.hasDebts;
  const hasPayoff = report.rollover.steps.length > 0;
  const path =
    kind === "Rollover" || !hasPayoff ? report.rollover : report.reclaimAll;
  const currency = report.planningCurrency;

  const view = useMemo(() => {
    const colors = debtColors(report.rollover.debts);
    const money = (amount: number) => formatCurrency(amount, currency);
    const finish = finishPlanItems(report, path, money);
    return {
      summary: planSummary(
        path,
        finish.filter((item) => item.isDebt).length,
        money,
        formatCalendarDate,
      ),
      finish,
      owed: owedShares(report.rollover.debts, colors),
      balance: balanceChart(path, colors),
      obligations: obligationChart(path),
      order: payoffOrder(path, colors),
      assumptions: planAssumptions(path.kind, currency),
    };
  }, [report, path, currency]);

  /**
   * Pins one debt's band, or clears the pin when that debt is pressed again.
   */
  function togglePin(debtId: string) {
    setPinnedDebtId((current) => (current === debtId ? null : debtId));
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Plan"
        description={showPlan ? planDescription(kind, hasPayoff) : undefined}
        actions={
          showPlan && hasPayoff ? (
            <SegmentedControl
              label="Freed payments"
              options={planPathOptions}
              value={kind}
              onChange={setKind}
            />
          ) : undefined
        }
      />
      {failed ? (
        <p className="text-sm text-muted-foreground">
          This plan could not be loaded.
        </p>
      ) : !report.hasDebts ? (
        <div className="rounded-lg border border-border bg-card">
          <EmptyState
            title="A debt is required"
            description="Add a card or loan. This page shows when each debt is paid off and the breathing room that follows."
            action={
              <Link
                href="/debts"
                className={cn(buttonVariants(), "min-h-11 px-4")}
              >
                Go to Debts
              </Link>
            }
          />
        </div>
      ) : (
        <>
          <PlanSummary summary={view.summary} />
          {view.finish.length > 0 ? (
            <PlanFinishList items={view.finish} />
          ) : null}
          {view.owed.length > 0 ? (
            <PlanOwedChart
              shares={view.owed}
              currency={currency}
              missingBalanceNames={report.missingBalance.map(
                (debt) => debt.name,
              )}
            />
          ) : null}
          {view.balance.rows.length > 0 ? (
            <PlanBalanceChart
              bands={view.balance.bands}
              rows={view.balance.rows}
              currency={currency}
              highlightedDebtId={hoveredDebtId ?? pinnedDebtId}
              onHighlight={setHoveredDebtId}
            />
          ) : null}
          {view.obligations.length > 0 ? (
            <PlanObligationChart rows={view.obligations} currency={currency} />
          ) : null}
          {view.order.length > 0 ? (
            <PlanPayoffOrder
              rows={view.order}
              currency={currency}
              pinnedDebtId={pinnedDebtId}
              onHover={setHoveredDebtId}
              onTogglePin={togglePin}
            />
          ) : null}
          <details className="app-panel">
            <summary className="min-h-11 cursor-pointer px-4 py-3 text-sm font-medium text-foreground focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none">
              How this is calculated
            </summary>
            <dl className="divide-y divide-border/70 border-t border-border">
              {view.assumptions.map((assumption) => (
                <div
                  key={assumption.term}
                  className="flex flex-col gap-0.5 px-4 py-2.5 sm:flex-row sm:gap-4"
                >
                  <dt className="text-sm font-medium text-foreground sm:w-36 sm:shrink-0">
                    {assumption.term}
                  </dt>
                  <dd className="text-sm text-muted-foreground">
                    {assumption.detail}
                  </dd>
                </div>
              ))}
            </dl>
          </details>
        </>
      )}
    </div>
  );
}
