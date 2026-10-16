"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { PageHeader } from "@/components/navigation/page-header";
import { buttonVariants } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import { formatCalendarDate } from "@/features/debts/debtDisplay";
import type { PayoffRolloverKind, PlanRecoveryDto } from "@/lib/api/types";
import { cn } from "@/lib/utils";
import { PlanAttentionSummary } from "./PlanAttentionSummary";
import { PlanCashOutlook } from "./PlanCashOutlook";
import { PlanCashPreview } from "./PlanCashPreview";
import { PlanDebtPayoff } from "./PlanDebtPayoff";
import { PlanDecision } from "./PlanDecision";
import { PlanOfferComparison } from "./PlanOfferComparison";
import { PlanScenarioControls } from "./PlanScenarioControls";
import { PlanSummary } from "./PlanSummary";
import { PlanTabs } from "./PlanTabs";
import { useRestructuringComparison } from "./useRestructuringComparison";
import { scenarioFromReport, usePlanScenario } from "./usePlanScenario";
import {
  balanceChart,
  cashChart,
  debtColors,
  obligationChart,
  owedShares,
  payoffOrder,
} from "./planChartSeries";
import {
  cashOutlookDescription,
  cashOutlookNotes,
  finishPlanItems,
  planAssumptions,
  planSpendingLine,
  planSummary,
} from "./planCopy";
import { cashRangeView, type PlanCashRange } from "./planCashRange";
import { debtMilestones } from "./planDebtSummary";
import type { PlanTab } from "./planNavigation";
import { planReadiness } from "./planReadiness";
import { spendableCopy } from "./spendableCopy";

type PlanPageClientProps = {
  report: PlanRecoveryDto;
  failed: boolean;
};

/**
 * Plan page with Overview, Cash outlook, Debt payoff, and Compare an offer.
 * Scenario, timeframe, chart selection, and the offer form live above the selected panel so switching views never resets them.
 */
export function PlanPageClient({
  report: baseline,
  failed,
}: PlanPageClientProps) {
  const { report, status, applyScenario } = usePlanScenario(baseline);
  const offer = useRestructuringComparison();
  const scenario = scenarioFromReport(report);
  const [activeTab, setActiveTab] = useState<PlanTab>("overview");
  const [kind, setKind] = useState<PayoffRolloverKind>("Rollover");
  const [cashRange, setCashRange] = useState<PlanCashRange>("30-days");
  const [hoveredDebtId, setHoveredDebtId] = useState<string | null>(null);
  const [pinnedDebtId, setPinnedDebtId] = useState<string | null>(null);
  const hasPayoff = report.rollover.steps.length > 0;
  const onRollover = kind === "Rollover" || !hasPayoff;
  const selectedKind: PayoffRolloverKind = onRollover
    ? "Rollover"
    : "ReclaimAll";
  const path = onRollover ? report.rollover : report.reclaimAll;
  const outlook = onRollover
    ? report.cashOutlook.rollover
    : report.cashOutlook.reclaimAll;
  const currency = report.planningCurrency;

  const view = useMemo(() => {
    const colors = debtColors(report.rollover.debts);
    const money = (amount: number) => formatCurrency(amount, currency);
    const finish = finishPlanItems(report, path, money);
    const typicalChart = cashChart(outlook.typical);
    const lowPayChart = outlook.lowPay ? cashChart(outlook.lowPay) : null;
    const spendable = spendableCopy(
      selectedKind === "Rollover"
        ? report.spendable.rollover
        : report.spendable.reclaimAll,
      report.cashOutlook,
      report.debtFacts,
      report.hasDebts,
      money,
      formatCalendarDate,
    );
    const summary = planSummary(
      path,
      outlook.typical,
      finish.filter((item) => item.isDebt).length,
      money,
      formatCalendarDate,
      report.monthlyExtra,
    );
    return {
      cash: {
        sourceDescription: cashOutlookDescription(
          selectedKind,
          hasPayoff,
          report.cashOutlook.startingCash,
          money,
          report.monthlyExtra,
          report.cashOutlook.startingReserve,
          report.cashOutlook.startingAvailable,
          scenario,
          formatCalendarDate,
        ),
        spendingLine: planSpendingLine(
          report.livingSpendingMonthly,
          report.scenarioLivingSpendingMonthly,
          money,
        ),
        overview: cashRangeView(
          outlook.typical,
          path,
          "30-days",
          typicalChart,
          money,
          formatCalendarDate,
        ),
        typical: cashRangeView(
          outlook.typical,
          path,
          cashRange,
          typicalChart,
          money,
          formatCalendarDate,
        ),
        lowPay:
          outlook.lowPay && lowPayChart
            ? cashRangeView(
                outlook.lowPay,
                path,
                cashRange,
                lowPayChart,
                money,
                formatCalendarDate,
              )
            : null,
        notes: cashOutlookNotes(report, path),
      },
      spendable,
      summary:
        spendable.leadsStatus && spendable.guardrail
          ? {
              warning: true,
              sentence: spendable.guardrail,
              figureLabel: "Extra payment shortfall",
              figure: spendable.guardrailOn ?? "Not dated",
              details: [
                { label: "Spendable", value: spendable.figure },
                ...summary.details.slice(0, 1),
              ],
            }
          : summary,
      readiness: planReadiness(
        report,
        outlook.typical,
        finish.filter((item) => item.isDebt).length,
        formatCalendarDate,
      ),
      finish,
      owed: owedShares(report.rollover.debts, colors),
      balance: balanceChart(path, colors),
      obligations: obligationChart(path),
      order: payoffOrder(path, colors),
      assumptions: planAssumptions(
        path.kind,
        currency,
        report.monthlyExtra,
        money,
        scenario,
        formatCalendarDate,
      ),
      milestones: debtMilestones(path, selectedKind, money, formatCalendarDate),
    };
  }, [
    report,
    path,
    outlook,
    selectedKind,
    hasPayoff,
    currency,
    cashRange,
    scenario,
  ]);

  /**
   * Pins one debt's band, or clears the pin when that debt is pressed again.
   */
  function togglePin(debtId: string) {
    setPinnedDebtId((current) => (current === debtId ? null : debtId));
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        className="print:hidden"
        title="Plan"
        description="See whether the current plan works, what needs attention, and how cash and debt change over time."
      />
      {failed ? (
        <p className="text-sm text-muted-foreground">
          This plan could not be loaded.
        </p>
      ) : (
        <>
          <PlanTabs value={activeTab} onChange={setActiveTab} />
          <div
            id="plan-panel"
            role="tabpanel"
            aria-labelledby={`plan-tab-${activeTab}`}
            className="flex flex-col gap-6 focus:outline-none"
          >
            {activeTab === "overview" && report.hasDebts ? (
              <PlanSummary
                summary={view.summary}
                nextAction={view.readiness.next}
                onReviewPayoff={() => setActiveTab("debt")}
              />
            ) : null}

            {activeTab !== "offer" ? (
              <div
                className={
                  activeTab === "decision" ? "order-2 print:hidden" : undefined
                }
              >
                <PlanScenarioControls
                  key="plan-scenario"
                  kind={selectedKind}
                  hasPayoff={hasPayoff}
                  applied={scenario}
                  storedSpending={report.livingSpendingMonthly}
                  storedFloor={report.cashFloor}
                  asOf={report.cashOutlook.asOf}
                  status={status}
                  currency={currency}
                  onKindChange={setKind}
                  onApply={applyScenario}
                />
              </div>
            ) : null}

            {activeTab === "decision" ? (
              <div className="order-1">
                <PlanDecision
                  scenario={scenario}
                  kind={selectedKind}
                  currency={currency}
                />
              </div>
            ) : null}

            {activeTab === "overview" ? (
              <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1.65fr)_minmax(18rem,0.75fr)]">
                <div className="flex flex-col gap-6">
                  <PlanCashPreview
                    view={view.cash.overview}
                    currency={currency}
                    startingReserve={report.cashOutlook.startingReserve}
                    spendable={view.spendable}
                  />
                  {report.hasDebts ? null : (
                    <div className="rounded-lg border border-border bg-card">
                      <EmptyState
                        title="A debt is required"
                        description="Add a card or loan. Plan will keep its payoff estimate conditional on dated cash."
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
                  )}
                </div>
                <PlanAttentionSummary
                  readiness={view.readiness}
                  debtItems={view.finish}
                />
              </div>
            ) : null}

            {activeTab === "cash" ? (
              <PlanCashOutlook
                sourceDescription={view.cash.sourceDescription}
                hasIncome={report.cashOutlook.hasIncome}
                typical={view.cash.typical}
                lowPay={view.cash.lowPay}
                notes={view.cash.notes}
                currency={currency}
                spendingLine={view.cash.spendingLine}
                startingCash={report.cashOutlook.startingCash}
                startingReserve={report.cashOutlook.startingReserve}
                startingAvailable={report.cashOutlook.startingAvailable}
                selectedRange={cashRange}
                onRangeChange={setCashRange}
              />
            ) : null}

            {activeTab === "offer" ? (
              report.hasDebts ? (
                <PlanOfferComparison
                  debts={report.debtFacts}
                  currency={currency}
                  form={offer.form}
                  comparison={offer.comparison}
                  error={offer.error}
                  isComparing={offer.isComparing}
                  stale={offer.stale}
                  onField={offer.setField}
                  onToggleDebt={offer.toggleDebt}
                  onCompare={() =>
                    void offer.compare(report.debtFacts, currency)
                  }
                />
              ) : (
                <div className="rounded-lg border border-border bg-card">
                  <EmptyState
                    title="A debt is required"
                    description="Add a card or loan before comparing an offer."
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
              )
            ) : null}

            {activeTab === "debt" ? (
              report.hasDebts ? (
                <PlanDebtPayoff
                  kind={selectedKind}
                  report={report}
                  bands={view.balance.bands}
                  balanceRows={view.balance.rows}
                  obligationRows={view.obligations}
                  orderRows={view.order}
                  owedShares={view.owed}
                  milestones={view.milestones}
                  assumptions={view.assumptions}
                  highlightedDebtId={hoveredDebtId ?? pinnedDebtId}
                  pinnedDebtId={pinnedDebtId}
                  onHighlight={setHoveredDebtId}
                  onTogglePin={togglePin}
                />
              ) : (
                <div className="rounded-lg border border-border bg-card">
                  <EmptyState
                    title="No debt payoff yet"
                    description="Add a debt to see its projected balance and payoff order."
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
              )
            ) : null}
          </div>
        </>
      )}
    </div>
  );
}
