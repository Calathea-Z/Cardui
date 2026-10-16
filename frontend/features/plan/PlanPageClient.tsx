"use client";

import Link from "next/link";
import { useEffect, useMemo, useRef, useState } from "react";
import { PageHeader } from "@/components/navigation/page-header";
import { buttonVariants } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import { formatCalendarDate } from "@/features/debts/debtDisplay";
import type { PayoffRolloverKind, PlanRecoveryDto } from "@/lib/api/types";
import { cn } from "@/lib/utils";
import { PlanAttentionSummary } from "./PlanAttentionSummary";
import { PlanCashOutlook } from "./PlanCashOutlook";
import { PlanDebtOutlookPanel } from "./PlanDebtOutlookPanel";
import { PlanDebtPayoff } from "./PlanDebtPayoff";
import { PlanDecision } from "./PlanDecision";
import { PlanOfferComparison } from "./PlanOfferComparison";
import { PlanSafety } from "./PlanSafety";
import { planSafetyAction } from "./planSafetyAction";
import { PlanTabs } from "./PlanTabs";
import { PlanWorkingScenario } from "./PlanWorkingScenario";
import { useRestructuringComparison } from "./useRestructuringComparison";
import { scenarioFromReport } from "./usePlanScenario";
import { usePlanWork } from "./usePlanWork";
import { useSavedPlan } from "./useSavedPlan";
import {
  canSaveWorkingScenario,
  currentFactsDraft,
  currentFactsScenario,
  draftFromApplied,
  emptyScenarioDraftErrors,
  isCurrentFacts,
  readScenarioDraft,
  type ScenarioDraft,
  type ScenarioDraftErrors,
} from "./planScenario";
import {
  balanceChart,
  balanceChartScope,
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
} from "./planCopy";
import { cashRangeView, type PlanCashRange } from "./planCashRange";
import { debtMilestones } from "./planDebtSummary";
import { planDebtOutlook } from "./planDebtOutlook";
import {
  isPlanDetail,
  type PlanDetail,
  type PlanTab,
  type PlanView,
} from "./planNavigation";
import { planReadiness } from "./planReadiness";
import { planReturnHref } from "./planReturn";
import { spendableCopy } from "./spendableCopy";
import {
  recoveryOutlook,
  recoveryPath,
  recoverySpendable,
} from "./planPayoffChoice";

type PlanPageClientProps = {
  report: PlanRecoveryDto;
  failed: boolean;
};

const backLabels: Record<PlanTab, string> = {
  overview: "Back to Overview",
  changes: "Back to Try changes",
  decision: "Back to Your plan",
};

/**
 * Plan page for the next 30 days, trying a change, and keeping a plan.
 * Cash outlook, debt payoff, and offer comparison stay in this component so the working scenario survives those views.
 */
export function PlanPageClient({
  report: baseline,
  failed,
}: PlanPageClientProps) {
  const { report, status, applyScenario, kind, setKind, settled } =
    usePlanWork(baseline);
  const offer = useRestructuringComparison();
  const scenario = scenarioFromReport(report);
  const [view, setView] = useState<PlanView>("overview");
  const [detailFrom, setDetailFrom] = useState<PlanTab>("overview");
  const [cashRange, setCashRange] = useState<PlanCashRange>("30-days");
  const [draft, setDraft] = useState<ScenarioDraft | null>(null);
  const [draftErrors, setDraftErrors] = useState<ScenarioDraftErrors>(
    emptyScenarioDraftErrors,
  );
  const [showReview, setShowReview] = useState(false);
  const seeded = useRef(false);
  const [hoveredDebtId, setHoveredDebtId] = useState<string | null>(null);
  const [pinnedDebtId, setPinnedDebtId] = useState<string | null>(null);
  const hasPayoff = report.rollover.steps.length > 0;
  const selectedKind: PayoffRolloverKind = kind;
  const path = recoveryPath(report, selectedKind);
  const outlook = recoveryOutlook(report, selectedKind);
  const currency = report.planningCurrency;
  const selectedTab = isPlanDetail(view) ? detailFrom : view;
  const matchesFacts = isCurrentFacts(scenario, selectedKind);
  const calculationReady =
    draft !== null &&
    canSaveWorkingScenario({
      status,
      draft,
      applied: scenario,
      kind: selectedKind,
      asOf: baseline.cashOutlook.asOf,
      savedLiving: baseline.livingSpendingMonthly,
    });
  const saved = useSavedPlan(
    scenario,
    selectedKind,
    !failed,
    matchesFacts,
    calculationReady,
  );
  const workingLabel = matchesFacts
    ? "Current facts"
    : "Trying changes — not saved";

  useEffect(() => {
    if (!settled || seeded.current) {
      return;
    }

    seeded.current = true;
    setDraft(draftFromApplied(scenario, selectedKind));
  }, [scenario, selectedKind, settled]);

  /**
   * Sends every valid field in one preview.
   * An invalid field keeps the draft and does not replace the result on screen.
   */
  async function updatePreview() {
    if (!draft) {
      return;
    }

    const read = readScenarioDraft(
      draft,
      baseline.cashOutlook.asOf,
      baseline.livingSpendingMonthly,
    );
    if (!read.ok) {
      setDraftErrors(read.errors);
      return;
    }

    setDraftErrors(emptyScenarioDraftErrors);
    const ok = await applyScenario(read.scenario);
    if (!ok) {
      return;
    }

    setKind(read.kind);
    setShowReview(true);
  }

  /**
   * Restores current facts in the draft and on screen.
   * The entered scenario is dropped.
   */
  async function resetChanges() {
    setDraft(currentFactsDraft());
    setDraftErrors(emptyScenarioDraftErrors);
    const ok = await applyScenario(currentFactsScenario());
    if (!ok) {
      return;
    }

    setKind("Rollover");
    setShowReview(true);
  }

  const model = useMemo(() => {
    const colors = debtColors(path.debts);
    const money = (amount: number) => formatCurrency(amount, currency);
    const finish = finishPlanItems(report, path, money);
    const typicalChart = cashChart(outlook.typical);
    const lowPayChart = outlook.lowPay ? cashChart(outlook.lowPay) : null;
    const spendable = spendableCopy(
      recoverySpendable(report, selectedKind),
      report.cashOutlook,
      report.debtFacts,
      report.hasDebts,
      money,
      formatCalendarDate,
    );
    const debtAttentionCount = finish.filter((item) => item.isDebt).length;
    const cashShort =
      spendable.status === "short" ||
      spendable.guardrail !== null ||
      outlook.typical.shortfallOn !== null ||
      outlook.typical.reserveShortfallOn !== null;
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
      action: planSafetyAction({
        status: spendable.status,
        hasDebts: report.hasDebts,
        debtAttentionCount,
        noCash: report.cashOutlook.startingCashAccountCount === 0,
        noIncome:
          !report.cashOutlook.hasIncome ||
          spendable.uncertainties.some((line) =>
            line.startsWith("No steady income"),
          ),
        spendingUnset:
          report.livingSpendingMonthly <= 0 ||
          spendable.uncertainties.some((line) =>
            line.startsWith("Ordinary spending"),
          ),
      }),
      debtOutlook: planDebtOutlook(
        path,
        selectedKind,
        cashShort,
        formatCalendarDate,
      ),
      cashShort,
      readiness: planReadiness(
        report,
        outlook.typical,
        debtAttentionCount,
        formatCalendarDate,
      ),
      finish,
      scope: balanceChartScope(path, report.missingBalance.length),
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
   * Opens a supporting view and remembers which primary view to restore.
   */
  function openDetail(detail: PlanDetail, from: PlanTab) {
    setDetailFrom(from);
    setView(detail);
  }

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
        description="Check the next 30 days, try a change, and keep a plan."
        actions={
          <Link
            href="/plan/setup"
            className={cn(
              buttonVariants({ variant: "outline" }),
              "min-h-11 bg-card px-4 md:hidden",
            )}
          >
            Plan setup
          </Link>
        }
      />
      {failed ? (
        <p className="text-sm text-muted-foreground">
          This plan could not be loaded.
        </p>
      ) : (
        <>
          {draft ? (
            <PlanWorkingScenario
              draft={draft}
              errors={draftErrors}
              appliedKind={selectedKind}
              hasPayoff={hasPayoff}
              baseline={baseline}
              report={report}
              status={status}
              label={saved.viewedId ? workingLabel : saved.chrome.label}
              uncommitted={!calculationReady && status === "ready"}
              unsafe={model.cashShort}
              showReview={showReview}
              onDraft={(next) => {
                setDraft(next);
                setDraftErrors(emptyScenarioDraftErrors);
              }}
              onUpdate={() => void updatePreview()}
              onReset={() => void resetChanges()}
              onReview={() => {
                setDetailFrom("decision");
                setView("decision");
              }}
            />
          ) : null}
          <PlanTabs
            value={selectedTab}
            onChange={(tab) => {
              setDetailFrom(tab);
              setView(tab);
            }}
          />
          <div
            id="plan-panel"
            role="tabpanel"
            aria-labelledby={`plan-tab-${selectedTab}`}
            className="flex flex-col gap-6 focus:outline-none"
          >
            {isPlanDetail(view) ? (
              <button
                type="button"
                onClick={() => setView(detailFrom)}
                className="min-h-11 w-fit text-sm font-medium text-primary underline print:hidden"
              >
                {backLabels[detailFrom]}
              </button>
            ) : null}

            {view === "overview" ? (
              <>
                <PlanSafety
                  spendable={model.spendable}
                  action={model.action}
                  onOpenCash={() => openDetail("cash", "overview")}
                  onTryChange={() => setView("changes")}
                />
                <PlanDebtOutlookPanel
                  outlook={model.debtOutlook}
                  onReviewDebt={() => openDetail("debt", "overview")}
                />
                {model.action.view === "cash" ? null : (
                  <button
                    type="button"
                    onClick={() => openDetail("cash", "overview")}
                    className="min-h-11 w-fit text-sm font-medium text-primary underline"
                  >
                    Review cash outlook
                  </button>
                )}
                <PlanAttentionSummary
                  readiness={model.readiness}
                  debtItems={model.finish}
                  primaryKey={model.action.key}
                />
              </>
            ) : null}

            {view === "changes" ? (
              <>
                <div className="flex flex-col gap-2 sm:flex-row sm:flex-wrap">
                  <button
                    type="button"
                    onClick={() => openDetail("cash", "changes")}
                    className={cn(
                      buttonVariants({ variant: "outline" }),
                      "min-h-11 bg-card",
                    )}
                  >
                    Review cash outlook
                  </button>
                  <button
                    type="button"
                    onClick={() => openDetail("debt", "changes")}
                    className={cn(
                      buttonVariants({ variant: "outline" }),
                      "min-h-11 bg-card",
                    )}
                  >
                    Review debt payoff
                  </button>
                  <button
                    type="button"
                    onClick={() => openDetail("offer", "changes")}
                    className={cn(
                      buttonVariants({ variant: "outline" }),
                      "min-h-11 bg-card",
                    )}
                  >
                    Compare an offer
                  </button>
                </div>
                <p className="text-sm text-muted-foreground">
                  Offer comparisons stay temporary and are not saved with Your
                  plan.{" "}
                  <Link
                    href={planReturnHref("/plan/setup", "plan")}
                    className="font-medium text-primary underline"
                  >
                    Plan setup
                  </Link>{" "}
                  edits saved facts and returns here.
                </p>
              </>
            ) : null}

            {view === "decision" ? (
              <PlanDecision
                saved={saved}
                calculationReady={calculationReady}
                onOpenCash={() => openDetail("cash", "decision")}
                onOpenDebt={() => openDetail("debt", "decision")}
              />
            ) : null}

            {view === "cash" ? (
              <PlanCashOutlook
                sourceDescription={model.cash.sourceDescription}
                hasIncome={report.cashOutlook.hasIncome}
                typical={model.cash.typical}
                lowPay={model.cash.lowPay}
                notes={model.cash.notes}
                currency={currency}
                spendingLine={model.cash.spendingLine}
                startingCash={report.cashOutlook.startingCash}
                startingReserve={report.cashOutlook.startingReserve}
                startingAvailable={report.cashOutlook.startingAvailable}
                selectedRange={cashRange}
                onRangeChange={setCashRange}
              />
            ) : null}

            {view === "offer" ? (
              report.hasDebts ? (
                <>
                  <p className="text-sm text-foreground">
                    This comparison is temporary. It is not saved with Your
                    plan, and it does not change the preview on Try changes.
                  </p>
                  <PlanOfferComparison
                    debts={report.debtFacts}
                    currency={currency}
                    form={offer.form}
                    comparison={offer.comparison}
                    error={offer.error}
                    fieldErrors={offer.fieldErrors}
                    isComparing={offer.isComparing}
                    stale={offer.stale}
                    onField={offer.setField}
                    onToggleDebt={offer.toggleDebt}
                    onCompare={() =>
                      void offer.compare(report.debtFacts, currency)
                    }
                  />
                </>
              ) : (
                <div className="rounded-lg border border-border bg-card">
                  <EmptyState
                    title="A debt is required"
                    description="Add a card or loan before comparing an offer."
                    action={
                      <Link
                        href={planReturnHref("/debts")}
                        className={cn(buttonVariants(), "min-h-11 px-4")}
                      >
                        Go to Debts
                      </Link>
                    }
                  />
                </div>
              )
            ) : null}

            {view === "debt" ? (
              report.hasDebts ? (
                <PlanDebtPayoff
                  kind={selectedKind}
                  report={report}
                  bands={model.balance.bands}
                  balanceRows={model.balance.rows}
                  obligationRows={model.obligations}
                  orderRows={model.order}
                  owedShares={model.owed}
                  milestones={model.milestones}
                  assumptions={model.assumptions}
                  chartScopeNote={model.scope.note}
                  excludedDebtCount={model.scope.excluded}
                  cashShort={model.cashShort}
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
                        href={planReturnHref("/debts")}
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
