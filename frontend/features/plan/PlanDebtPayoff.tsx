import type { PayoffRolloverKind, PlanRecoveryDto } from "@/lib/api/types";
import { PlanBalanceChart } from "./PlanBalanceChart";
import { PlanObligationChart } from "./PlanObligationChart";
import { PlanOwedChart } from "./PlanOwedChart";
import { PlanPayoffOrder } from "./PlanPayoffOrder";
import type {
  PlanBalanceRow,
  PlanBand,
  PlanObligationRow,
  PlanOwedShare,
  PlanPayoffRow,
} from "./planChartSeries";
import type { PlanAssumption } from "./planCopy";
import type { PlanDebtMilestones } from "./planDebtSummary";

type PlanDebtPayoffProps = {
  kind: PayoffRolloverKind;
  report: PlanRecoveryDto;
  bands: PlanBand[];
  balanceRows: PlanBalanceRow[];
  obligationRows: PlanObligationRow[];
  orderRows: PlanPayoffRow[];
  owedShares: PlanOwedShare[];
  milestones: PlanDebtMilestones;
  assumptions: PlanAssumption[];
  highlightedDebtId: string | null;
  pinnedDebtId: string | null;
  onHighlight: (debtId: string | null) => void;
  onTogglePin: (debtId: string) => void;
};

/**
 * Makes declining balances the primary debt view and keeps supporting composition, obligation, and assumption details optional.
 */
export function PlanDebtPayoff({
  kind,
  report,
  bands,
  balanceRows,
  obligationRows,
  orderRows,
  owedShares,
  milestones,
  assumptions,
  highlightedDebtId,
  pinnedDebtId,
  onHighlight,
  onTogglePin,
}: PlanDebtPayoffProps) {
  return (
    <div className="flex flex-col gap-6">
      <section
        className="app-panel p-4"
        aria-labelledby="debt-milestones-title"
      >
        <h2 id="debt-milestones-title" className="app-section-title">
          Payoff milestones
        </h2>
        <dl className="mt-3 grid gap-3 sm:grid-cols-3">
          <div>
            <dt className="text-xs text-muted-foreground">Current minimums</dt>
            <dd className="mt-1 text-sm font-semibold text-foreground tabular-nums">
              {milestones.currentMinimums}
            </dd>
          </div>
          <div>
            <dt className="text-xs text-muted-foreground">First payoff</dt>
            <dd className="mt-1 text-sm font-semibold text-foreground">
              {milestones.firstPayoff}
            </dd>
            <p className="text-xs text-muted-foreground">
              Removes {milestones.firstPaymentRemoved}
            </p>
          </div>
          <div>
            <dt className="text-xs text-muted-foreground">Breathing room</dt>
            <dd className="mt-1 text-sm font-semibold text-foreground tabular-nums">
              {milestones.breathingRoom}
            </dd>
            <p className="text-xs text-muted-foreground">
              {milestones.breathingRoomNote}
            </p>
          </div>
        </dl>
      </section>

      {balanceRows.length > 0 && orderRows.length > 0 ? (
        <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1.65fr)_minmax(18rem,0.75fr)]">
          <PlanBalanceChart
            bands={bands}
            rows={balanceRows}
            currency={report.planningCurrency}
            highlightedDebtId={highlightedDebtId}
            onHighlight={onHighlight}
          />
          <PlanPayoffOrder
            rows={orderRows}
            currency={report.planningCurrency}
            kind={kind}
            pinnedDebtId={pinnedDebtId}
            onHover={onHighlight}
            onTogglePin={onTogglePin}
          />
        </div>
      ) : (
        <section className="app-panel p-4">
          <h2 className="app-section-title">Declining debt balance</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            The balance chart appears after at least one debt has a projected
            payoff. Review the attention details on Overview for missing or
            inconsistent terms.
          </p>
        </section>
      )}

      {obligationRows.length > 0 ? (
        <details className="app-panel">
          <summary className="min-h-11 cursor-pointer px-4 py-3 text-sm font-medium text-foreground focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none">
            View minimum payments and breathing room chart
          </summary>
          <div className="border-t border-border">
            <PlanObligationChart
              rows={obligationRows}
              currency={report.planningCurrency}
              embedded
            />
          </div>
        </details>
      ) : null}

      {owedShares.length > 0 ? (
        <details className="app-panel">
          <summary className="min-h-11 cursor-pointer px-4 py-3 text-sm font-medium text-foreground focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none">
            View current debt share breakdown
          </summary>
          <div className="border-t border-border">
            <PlanOwedChart
              shares={owedShares}
              currency={report.planningCurrency}
              missingBalanceNames={report.missingBalance.map(
                (debt) => debt.name,
              )}
              embedded
            />
          </div>
        </details>
      ) : null}

      <details className="app-panel">
        <summary className="min-h-11 cursor-pointer px-4 py-3 text-sm font-medium text-foreground focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none">
          How debt payoff is calculated
        </summary>
        <dl className="divide-y divide-border/70 border-t border-border">
          {assumptions.map((assumption) => (
            <div
              key={assumption.term}
              className="flex flex-col gap-0.5 px-4 py-2.5 sm:flex-row sm:gap-4"
            >
              <dt className="text-sm font-medium text-foreground sm:w-40 sm:shrink-0">
                {assumption.term}
              </dt>
              <dd className="text-sm text-muted-foreground">
                {assumption.detail}
              </dd>
            </div>
          ))}
        </dl>
      </details>
    </div>
  );
}
