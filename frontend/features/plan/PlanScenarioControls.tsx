import { ChevronRight, CircleAlert, LoaderCircle } from "lucide-react";
import { SegmentedControl } from "@/components/ui/segmented-control";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import { formatCalendarDate } from "@/features/debts/debtDisplay";
import type { PayoffRolloverKind, PlanScenarioQuery } from "@/lib/api/types";
import { PlanCashScenarioFields } from "./PlanCashScenarioFields";
import { PlanExtraField } from "./PlanExtraField";
import {
  planPathDescription,
  planPathOptions,
  planScenarioStatus,
  planScenarioSummary,
} from "./planCopy";
import type { PlanScenarioStatus } from "./usePlanScenario";

type PlanScenarioControlsProps = {
  kind: PayoffRolloverKind;
  hasPayoff: boolean;
  applied: PlanScenarioQuery;
  storedSpending: number;
  storedFloor: number;
  asOf: string;
  status: PlanScenarioStatus;
  currency: string;
  onKindChange: (kind: PayoffRolloverKind) => void;
  onApply: (scenario: PlanScenarioQuery) => void;
};

/**
 * Groups the temporary payoff behavior and cash preview in one disclosure.
 * Its summary always describes the applied forecast. A pending or failed request says the visible results are still the previous forecast.
 */
export function PlanScenarioControls({
  kind,
  hasPayoff,
  applied,
  storedSpending,
  storedFloor,
  asOf,
  status,
  currency,
  onKindChange,
  onApply,
}: PlanScenarioControlsProps) {
  const money = (amount: number) => formatCurrency(amount, currency);
  const statusMessage = status === "ready" ? null : planScenarioStatus(status);

  return (
    <section className="app-panel">
      <details className="group">
        <summary className="flex min-h-11 cursor-pointer items-center justify-between gap-3 px-4 py-3 focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none focus-visible:ring-inset">
          <span>
            <span className="block text-sm font-semibold text-foreground">
              Try a scenario
            </span>
            <span className="block text-xs text-muted-foreground">
              {planScenarioSummary(
                kind,
                hasPayoff,
                applied,
                money,
                formatCalendarDate,
              )}
            </span>
          </span>
          <ChevronRight
            aria-hidden
            className="size-4 shrink-0 transition-transform group-open:rotate-90 motion-reduce:transition-none"
          />
        </summary>
        <div className="grid gap-5 border-t border-border p-4 md:grid-cols-[minmax(0,1.3fr)_minmax(14rem,0.7fr)]">
          <div>
            <p className="text-sm font-medium text-foreground">
              When a payment ends
            </p>
            {hasPayoff ? (
              <>
                <SegmentedControl
                  label="Payment strategy"
                  options={planPathOptions}
                  value={kind}
                  onChange={onKindChange}
                  className="mt-2"
                />
                <p className="mt-2 text-sm text-muted-foreground">
                  {planPathDescription(kind)}
                </p>
              </>
            ) : (
              <p className="mt-1 text-sm text-muted-foreground">
                Payment strategy becomes available after at least one debt has a
                projected payoff.
              </p>
            )}
          </div>
          <PlanExtraField
            appliedAmount={applied.monthlyExtra}
            retryCurrentAmount={status !== "ready"}
            onApply={(monthlyExtra) => onApply({ ...applied, monthlyExtra })}
          />
          <div className="md:col-span-2">
            <PlanCashScenarioFields
              applied={applied}
              storedSpending={storedSpending}
              storedFloor={storedFloor}
              asOf={asOf}
              currency={currency}
              retry={status !== "ready"}
              onApply={onApply}
            />
          </div>
          <p className="text-xs text-muted-foreground md:col-span-2">
            Temporary preview. Nothing is saved, and it resets when you leave
            Plan. A windfall stays in cash.
          </p>
        </div>
      </details>
      {statusMessage ? (
        <p
          aria-live="polite"
          className={`flex items-start gap-2 border-t border-border px-4 py-3 text-sm ${
            status === "error" ? "text-destructive" : "text-muted-foreground"
          }`}
        >
          {status === "updating" ? (
            <LoaderCircle
              aria-hidden
              className="mt-0.5 size-4 shrink-0 animate-spin motion-reduce:animate-none"
            />
          ) : (
            <CircleAlert aria-hidden className="mt-0.5 size-4 shrink-0" />
          )}
          {statusMessage}
        </p>
      ) : null}
    </section>
  );
}
